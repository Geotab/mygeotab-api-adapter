using Geotab.Checkmate.ObjectModel;
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that extracts <see cref="Diagnostic"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class DiagnosticProcessor : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment adapterEnvironment;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGenericDbObjectCache<DbDiagnostic, AdapterGenericDbObjectCache<DbDiagnostic>> dbDiagnosticObjectCache;
        readonly IGenericEntityPersister<DbDiagnostic> dbDiagnosticEntityPersister;
        readonly IGenericGeotabObjectCacher<Diagnostic> diagnosticGeotabObjectCacher;
        readonly IGeotabDiagnosticDbDiagnosticObjectMapper geotabDiagnosticDbDiagnosticObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IPrerequisiteServiceChecker prerequisiteServiceChecker;
        readonly IServiceTracker serviceTracker;
        readonly IStateMachine stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticProcessor"/> class.
        /// </summary>
        public DiagnosticProcessor(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment adapterEnvironment, IExceptionHelper exceptionHelper, IGenericGenericDbObjectCache<DbDiagnostic, AdapterGenericDbObjectCache<DbDiagnostic>> dbDiagnosticObjectCache, IGenericEntityPersister<DbDiagnostic> dbDiagnosticEntityPersister, IGeotabDiagnosticDbDiagnosticObjectMapper geotabDiagnosticDbDiagnosticObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IPrerequisiteServiceChecker prerequisiteServiceChecker, IServiceTracker serviceTracker, IStateMachine stateMachine, IGenericGeotabObjectCacher<Diagnostic> diagnosticGeotabObjectCacher, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.exceptionHelper = exceptionHelper;
            this.dbDiagnosticObjectCache = dbDiagnosticObjectCache;
            this.dbDiagnosticEntityPersister = dbDiagnosticEntityPersister;
            this.geotabDiagnosticDbDiagnosticObjectMapper = geotabDiagnosticDbDiagnosticObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.prerequisiteServiceChecker = prerequisiteServiceChecker;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.diagnosticGeotabObjectCacher = diagnosticGeotabObjectCacher;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            while (!stoppingToken.IsCancellationRequested)
            {
                await WaitForPrerequisiteServicesIfNeededAsync(stoppingToken);

                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        await InitializeOrUpdateCachesAsync(cancellationTokenSource);

                        var dbDiagnosticsToPersist = new List<DbDiagnostic>();
                        // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
                        if (diagnosticGeotabObjectCacher.LastUpdatedTimeUTC > diagnosticGeotabObjectCacher.LastPropagatedToDatabaseTimeUtc)
                        {
                            DateTime recordChangedTimestampUtc = DateTime.UtcNow;

                            // Find any diagnostics that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                            var dbDiagnostics = await dbDiagnosticObjectCache.GetObjectsAsync();
                            foreach (var dbDiagnostic in dbDiagnostics)
                            {
                                if (dbDiagnostic.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                                {
                                    bool diagnosticExistsInCache = diagnosticGeotabObjectCacher.GeotabObjectCache.ContainsKey(Id.Create(dbDiagnostic.GeotabId));
                                    if (!diagnosticExistsInCache)
                                    {
                                        logger.Debug($"Diagnostic '{dbDiagnostic.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                        dbDiagnostic.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                        dbDiagnostic.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        dbDiagnostic.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                        dbDiagnosticsToPersist.Add(dbDiagnostic);
                                    }
                                }
                            }

                            // Iterate through the in-memory cache of Geotab Diagnostic objects.
                            foreach (var diagnostic in diagnosticGeotabObjectCacher.GeotabObjectCache.Values)
                            {
                                // Try to find the existing database record for the cached diagnostic.
                                var dbDiagnostic = await dbDiagnosticObjectCache.GetObjectAsync(diagnostic.Id.ToString());
                                if (dbDiagnostic != null)
                                {
                                    // The diagnostic has already been added to the database.
                                    if (geotabDiagnosticDbDiagnosticObjectMapper.EntityRequiresUpdate(dbDiagnostic, diagnostic))
                                    {
                                        DbDiagnostic updatedDbDiagnostic = geotabDiagnosticDbDiagnosticObjectMapper.UpdateEntity(dbDiagnostic, diagnostic);
                                        dbDiagnosticsToPersist.Add(updatedDbDiagnostic);
                                    }
                                }
                                else
                                {
                                    // The diagnostic has not yet been added to the database. Create a DbDiagnostic, set its properties and add it to the cache.
                                    var newDbDiagnostic = geotabDiagnosticDbDiagnosticObjectMapper.CreateEntity(diagnostic);

                                    // There may be multiple records for the same entity in the batch of entities retrieved from Geotab. If there are, make sure that duplicates are set to be updated instead of inserted.
                                    if (dbDiagnosticsToPersist.Where(dbDiagnostic => dbDiagnostic.GeotabId == newDbDiagnostic.GeotabId).Any())
                                    {
                                        newDbDiagnostic.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                    }

                                    dbDiagnosticsToPersist.Add(newDbDiagnostic);
                                }
                            }

                            stoppingToken.ThrowIfCancellationRequested();
                        }
                        else
                        {
                            logger.Debug($"Diagnostic cache in database is up-to-date.");
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbDiagnostic:
                                    await dbDiagnosticEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDiagnosticsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking:
                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DiagnosticProcessor, DateTime.UtcNow);

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

                        // If there were any changes, force the DbDiagnostic cache to be updated so that the changes are immediately available to other consumers.
                        if (dbDiagnosticsToPersist.Any())
                        {
                            await dbDiagnosticObjectCache.UpdateAsync(true);
                        }
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
                    HandleException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    HandleException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                }

                // Add a delay equivalent to the configured update interval.
                var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.DiagnosticCacheUpdateIntervalMinutes);
                logger.Info($"{CurrentClassName} pausing for the configured update interval ({delayTimeSpan}).");
                await Task.Delay(delayTimeSpan, stoppingToken);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>. If the <paramref name="exception"/> is connectivity-related, the <see cref="stateMachine"/> will have its <see cref="IStateMachine.CurrentState"/> and <see cref="IStateMachine.Reason"/> set accordingly. If the value supplied for <paramref name="logLevel"/> is <see cref="NLogLogLevelName.Fatal"/>, the current process will be killed.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="logLevel">The <see cref="LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        void HandleException(Exception exception, NLogLogLevelName logLevel, string errorMessagePrefix)
        {
            exceptionHelper.LogException(exception, logLevel, errorMessagePrefix);
            if (exception is AdapterDatabaseConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.AdapterDatabaseNotAvailable);
            }
            else if (exception is MyGeotabConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.MyGeotabNotAvailable);
            }

            if (logLevel == NLogLogLevelName.Fatal)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Initializes and/or updates any caches used by this class.
        /// </summary>
        /// <returns></returns>
        async Task InitializeOrUpdateCachesAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

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
            if (dbDiagnosticObjectCache.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(dbDiagnosticObjectCache.InitializeAsync(Databases.AdapterDatabase));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Starts the current <see cref="DiagnosticProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DiagnosticProcessor);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DiagnosticProcessor, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableDiagnosticCache == true)
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
        /// Stops the current <see cref="DiagnosticProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Checks whether any prerequisite services have been run and are currently running. If any of prerequisite services have not yet been run or are not currently running, details will be logged and this service will pause operation, repeating this check intermittently until all prerequisite services are running.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        async Task WaitForPrerequisiteServicesIfNeededAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var prerequisiteServices = new List<AdapterService>
            {
            };

            await prerequisiteServiceChecker.WaitForPrerequisiteServicesIfNeededAsync(CurrentClassName, prerequisiteServices, cancellationToken);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
