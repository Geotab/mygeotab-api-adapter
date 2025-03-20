using Geotab.Checkmate.ObjectModel.Engine;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that extracts <see cref="Diagnostic"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class DiagnosticProcessor2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DiagnosticProcessor2> awaiter;
        readonly IBaseRepository<DbStgDiagnostic2> dbStgDiagnostic2Repo;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext> dbDiagnosticId2ObjectCache;
        readonly IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnostic2, AdapterDatabaseUnitOfWorkContext> dbDiagnostic2ObjectCache;
        readonly IGenericEntityPersister<DbStgDiagnostic2> dbStgDiagnostic2EntityPersister;
        readonly IGenericGeotabObjectCacher<Diagnostic> diagnosticGeotabObjectCacher;
        readonly IGeotabDiagnosticDbStgDiagnostic2ObjectMapper geotabDiagnosticDbStgDiagnostic2ObjectMapper;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticProcessor2"/> class.
        /// </summary>
        public DiagnosticProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<DiagnosticProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext> dbDiagnosticId2ObjectCache, IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnostic2, AdapterDatabaseUnitOfWorkContext> dbDiagnostic2ObjectCache, IGenericEntityPersister<DbStgDiagnostic2> dbStgDiagnostic2EntityPersister, IGeotabDiagnosticDbStgDiagnostic2ObjectMapper geotabDiagnosticDbStgDiagnostic2ObjectMapper, IGeotabIdConverter geotabIdConverter, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericGeotabObjectCacher<Diagnostic> diagnosticGeotabObjectCacher, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbDiagnosticId2ObjectCache = dbDiagnosticId2ObjectCache;
            this.dbDiagnostic2ObjectCache = dbDiagnostic2ObjectCache;
            this.dbStgDiagnostic2EntityPersister = dbStgDiagnostic2EntityPersister;
            this.geotabDiagnosticDbStgDiagnostic2ObjectMapper = geotabDiagnosticDbStgDiagnostic2ObjectMapper;
            this.geotabIdConverter = geotabIdConverter;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.diagnosticGeotabObjectCacher = diagnosticGeotabObjectCacher;

            dbStgDiagnostic2Repo = new BaseRepository<DbStgDiagnostic2>(adapterContext);

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const string MergeFunctionSQL_Postgres = @"SELECT public.""spMerge_stg_Diagnostics2""(@SetEntityStatusDeletedForMissingDiagnostics::boolean);";
            const string MergeProcedureSQL_SQLServer = @"EXEC [dbo].[spMerge_stg_Diagnostics2] @SetEntityStatusDeletedForMissingDiagnostics = @SetEntityStatusDeletedForMissingDiagnostics;";
            const string TruncateStagingTableSQL_Postgres = @"TRUNCATE TABLE public.""stg_Diagnostics2"";";
            const string TruncateStagingTableSQL_SQLServer = @"TRUNCATE TABLE [dbo].[stg_Diagnostics2];";

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.DiagnosticCacheUpdateIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService> { };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        var dbStgDiagnostic2sToPersist = new List<DbStgDiagnostic2>();

                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (diagnosticGeotabObjectCacher.LastUpdatedTimeUTC > diagnosticGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            // Iterate through the in-memory cache of Geotab Diagnostic objects that were added or updated in the last cache update, mapping them to corresponding DbStgDiagnostic2 entities and adding them to the list of DbStgDiagnostic2 objects to persist. Note that deduplication logic is contained in the database.
                            foreach (var diagnostic in diagnosticGeotabObjectCacher.GeotabObjectsChangedInLastUpdate)
                            {
                                var newdbStgDiagnostic2 = geotabDiagnosticDbStgDiagnostic2ObjectMapper.CreateEntity(diagnostic);
                                dbStgDiagnostic2sToPersist.Add(newdbStgDiagnostic2);
                            }

                            stoppingToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            logger.Debug($"Diagnostic cache in database is up-to-date.");
                        }

                        // Persist changes to database. Step 1: Persist the DbStgDiagnostic2 entities.
                        if (dbStgDiagnostic2sToPersist.Count != 0)
                        {
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                {
                                    try
                                    {
                                        // Truncate staging table in case it contains any data:
                                        var sql = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => TruncateStagingTableSQL_Postgres,
                                            ConnectionInfo.DataAccessProviderType.SQLServer => TruncateStagingTableSQL_SQLServer,
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };
                                        await dbStgDiagnostic2Repo.ExecuteAsync(sql, null, cancellationTokenSource, true, adapterContext);

                                        // DbStgDiagnostic2:
                                        await dbStgDiagnostic2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgDiagnostic2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        // Commit transactions:
                                        await adapterUOW.CommitAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                        exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                        await adapterUOW.RollBackAsync();
                                        throw;
                                    }
                                }
                            }, new Context());
                        }

                        // Persist changes to database. Step 2: Merge the DbStgDiagnostic2 entities into the DbDiagnostic2 table and update the DbOServiceTracking table.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    if (dbStgDiagnostic2sToPersist.Count != 0)
                                    {
                                        // Define parameters for the merge procedure.
                                        var setEntityStatusDeletedForMissingDiagnostics = false;
                                        if (diagnosticGeotabObjectCacher.LastCacheOperationType == CacheOperationType.Refresh)
                                        {
                                            setEntityStatusDeletedForMissingDiagnostics = true;
                                        }
                                        var parameters = new[]
                                        {
                                        new { SetEntityStatusDeletedForMissingDiagnostics = setEntityStatusDeletedForMissingDiagnostics }
                                    };

                                        // Build the SQL statement to execute the merge procedure.
                                        var sql = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => MergeFunctionSQL_Postgres,
                                            ConnectionInfo.DataAccessProviderType.SQLServer => MergeProcedureSQL_SQLServer,
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };

                                        // Execute the merge procedure.
                                        await dbStgDiagnostic2Repo.ExecuteAsync(sql, parameters, cancellationTokenSource);
                                    }

                                    // DbOServiceTracking:
                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DiagnosticProcessor2, DateTime.UtcNow);

                                    // Commit transactions:
                                    await adapterUOW.CommitAsync();
                                }
                                catch (Exception ex)
                                {
                                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                    await adapterUOW.RollBackAsync();
                                    throw;
                                }
                            }
                        }, new Context());

                        // If there were any changes, force the DbDiagnostic2 and DbDiagnosticId2 caches to be updated so that the changes are immediately available to other consumers. Run tasks in parallel.
                        var dbDiagnosticCacheUpdateTasks = new List<Task>();
                        if (dbStgDiagnostic2sToPersist.Count != 0)
                        {
                            dbDiagnosticCacheUpdateTasks.Add(dbDiagnostic2ObjectCache.UpdateAsync(true));
                        }
                        if (dbStgDiagnostic2sToPersist.Count != 0)
                        {
                            dbDiagnosticCacheUpdateTasks.Add(dbDiagnosticId2ObjectCache.UpdateAsync(true));
                        }
                        await Task.WhenAll(dbDiagnosticCacheUpdateTasks);
                    }

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"{CurrentClassName} process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (AdapterDatabaseConnectionException databaseConnectionException)
                {
                    exceptionHelper.LogException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(databaseConnectionException, NLogLogLevelName.Error);
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    exceptionHelper.LogException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(myGeotabConnectionException, NLogLogLevelName.Error);

                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                }

                // Add a delay equivalent to the configured interval.
                await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Update, stoppingToken);
            }
        }

        /// <summary>
        /// Initializes and/or updates any caches used by this class.
        /// </summary>
        /// <returns></returns>
        async Task InitializeOrUpdateCachesAsync(CancellationTokenSource cancellationTokenSource)
        {
            var dbObjectCacheInitializationAndUpdateTasks = new List<Task>();

            // Update the in-memory cache of Geotab objects obtained via API from the MyGeotab database.
            if (diagnosticGeotabObjectCacher.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(diagnosticGeotabObjectCacher.InitializeAsync(cancellationTokenSource, adapterConfiguration.DiagnosticCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.DiagnosticCacheUpdateIntervalMinutes, adapterConfiguration.DiagnosticCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitDefault, true));
            }
            else
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(diagnosticGeotabObjectCacher.UpdateGeotabObjectCacheAsync(cancellationTokenSource));
            }

            // Update the in-memory cache of database objects that correspond with Geotab objects.
            if (dbDiagnostic2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDiagnostic2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }
            if (dbDiagnosticId2ObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDiagnosticId2ObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="DiagnosticProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DiagnosticProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DiagnosticProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
                        await adapterUOW.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                        await adapterUOW.RollBackAsync();
                        throw;
                    }
                }
            }, new Context());

            // Register this service with the StateMachine. Set mustPauseForDatabaseMaintenance to true if the service is enabled or false otherwise.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(DiagnosticProcessor2), adapterConfiguration.EnableDiagnosticCache);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableDiagnosticCache == true)
            {
                logger.Info($"******** STARTING SERVICE: {CurrentClassName}");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {CurrentClassName} service has not been enabled and will NOT be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="DiagnosticProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(DiagnosticProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
