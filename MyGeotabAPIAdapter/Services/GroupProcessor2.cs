using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="Group"/> objects from a MyGeotab database for in-memory caching. 
    /// </summary>
    class GroupProcessor2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<GroupProcessor2> awaiter;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericGeotabObjectCacher<Group> groupGeotabObjectCacher;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupProcessor2"/> class.
        /// </summary>
        public GroupProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<GroupProcessor2> awaiter, IExceptionHelper exceptionHelper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericGeotabObjectCacher<Group> groupGeotabObjectCacher, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.groupGeotabObjectCacher = groupGeotabObjectCacher;

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
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromMinutes(adapterConfiguration.GroupCacheUpdateIntervalMinutes);

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
            if (groupGeotabObjectCacher.IsInitialized == false)
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(groupGeotabObjectCacher.InitializeAsync(cancellationTokenSource, adapterConfiguration.GroupCacheIntervalDailyReferenceStartTimeUTC, adapterConfiguration.GroupCacheUpdateIntervalMinutes, adapterConfiguration.GroupCacheRefreshIntervalMinutes, myGeotabAPIHelper.GetFeedResultLimitDefault, false));
            }
            else
            {
                dbObjectCacheInitializationAndUpdateTasks.Add(groupGeotabObjectCacher.UpdateGeotabObjectCacheAsync(cancellationTokenSource));
            }

            await Task.WhenAll(dbObjectCacheInitializationAndUpdateTasks);
        }

        /// <summary>
        /// Starts the current <see cref="GroupProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.GroupProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.GroupProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Register this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false as this service does not need to participate in pauses for database maintenance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(GroupProcessor2), false);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableGroupCache == true)
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
        /// Stops the current <see cref="GroupProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(GroupProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
