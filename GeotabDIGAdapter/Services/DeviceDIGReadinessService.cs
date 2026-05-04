using Dapper.Contrib.Extensions;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.Models;
using MyGeotabAPIAdapter.MyAdminAPI;
using NLog;
using Polly;
using Polly.Retry;
using System.Reflection;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that checks provisioned devices for DIG readiness by querying the MyAdmin API to determine if devices have been assigned to a database and are ready to send data to Geotab.
    /// </summary>
    class DeviceDIGReadinessService : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames;
        readonly IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DeviceDIGReadinessService> awaiter;
        readonly IExceptionHelper exceptionHelper;
        readonly IMyAdminAPIHelper myAdminAPIHelper;
        readonly IServiceTracker<DbGdaOServiceTracking> serviceTracker;
        readonly IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        // Entity Persisters
        readonly IGenericEntityPersister<DbGdaProvisionedDevice> dbGdaProvisionedDeviceEntityPersister;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        const int DeviceDIGReadinessServiceBatchSize = 5000;
        const int ExecutionThrottleEngagingRecordCount = 1;
        const int DefaultDeviceDIGReadinessServiceIntervalSeconds = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceDIGReadinessService"/> class.
        /// </summary>
        public DeviceDIGReadinessService(
            IDIGAdapterConfiguration adapterConfiguration,
            IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames,
            IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment,
            IBackgroundServiceAwaiter<DeviceDIGReadinessService> awaiter,
            IExceptionHelper exceptionHelper,
            IMyAdminAPIHelper myAdminAPIHelper,
            IServiceTracker<DbGdaOServiceTracking> serviceTracker,
            IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext,
            IGenericEntityPersister<DbGdaProvisionedDevice> dbGdaProvisionedDeviceEntityPersister)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.digAdapterDatabaseObjectNames = digAdapterDatabaseObjectNames;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.myAdminAPIHelper = myAdminAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.adapterContext = adapterContext;
            this.dbGdaProvisionedDeviceEntityPersister = dbGdaProvisionedDeviceEntityPersister;

            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Builds the SQL statement to retrieve devices that need DIG readiness checking. Orders by RecordLastChangedUtc to ensure all devices are checked before rechecking any.
        /// </summary>
        /// <returns>The SQL statement formatted for the current database provider.</returns>
        string BuildSqlToGetDevicesToBeChecked()
        {
            var tableName = digAdapterDatabaseObjectNames.DbGdaProvisionedDevicesTableName;

            return adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT * FROM {SqlMapperExtensions.FormatTableNameForSql(tableName, "npgsqlconnection")} 
                       WHERE ""IsOkayToSendDataToGeotab"" = false 
                       ORDER BY ""RecordLastChangedUtc"" 
                       LIMIT @BatchSize;",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $@"SELECT TOP (@BatchSize) * FROM {SqlMapperExtensions.FormatTableNameForSql(tableName, "sqlconnection")} 
                       WHERE IsOkayToSendDataToGeotab = 0 
                       ORDER BY RecordLastChangedUtc;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delayTimeSpan = TimeSpan.FromSeconds(DefaultDeviceDIGReadinessServiceIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                await PrepareForExecutionAsync(stoppingToken);

                try
                {
                    var result = await ExecuteIterationBusinessLogicAsync(stoppingToken);

                    if (result.ShouldThrottle(ExecutionThrottleEngagingRecordCount))
                    {
                        await awaiter.WaitForConfiguredIntervalAsync(
                            delayTimeSpan, DelayIntervalType.Wait, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.Warn($"{CurrentClassName} process cancelled.");
                    throw;
                }
                catch (AdapterDatabaseConnectionException databaseConnectionException)
                {
                    HandleAdapterDatabaseConnectionException(databaseConnectionException);
                }
                catch (MyAdminConnectionException myAdminConnectionException)
                {
                    HandleMyAdminConnectionException(myAdminConnectionException);
                }
                catch (Exception ex)
                {
                    HandleGenericException(ex);
                }
            }
        }

        /// <summary>
        /// Executes the business logic for a single iteration of <see cref="ExecuteAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        async Task<BatchProcessingResult> ExecuteIterationBusinessLogicAsync(CancellationToken stoppingToken)
        {
            MethodBase? methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Started iteration of {methodBase?.ReflectedType?.Name}.{methodBase?.Name}");

            // Retrieve a batch of DbGdaProvisionedDevice entities that need to be checked for DIG readiness.
            var dbGdaProvisionedDevices = await GetBatchOfDevicesToBeCheckedAsync(stoppingToken);

            if (dbGdaProvisionedDevices.Count == 0)
            {
                logger.Debug($"No records retrieved from {digAdapterDatabaseObjectNames.DbGdaProvisionedDevicesTableName} table.");

                // No records were returned, but the OServiceTracking record for this service still
                // needs to be updated to show that the service is operating.
                await UpdateServiceTrackingRecordAsync();

                return new BatchProcessingResult
                {
                    TotalCount = 0,
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            logger.Info($"Retrieved {dbGdaProvisionedDevices.Count} records from {digAdapterDatabaseObjectNames.DbGdaProvisionedDevicesTableName} table. Checking DIG readiness...");

            // Extract serial numbers for the API call.
            var serialNumbers = dbGdaProvisionedDevices
                .Select(d => d.GeotabSerialNumber)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            // Call MyAdmin API to get device database names.
            var apiResult = await myAdminAPIHelper.GetDeviceDatabaseNamesAsync(serialNumbers, adapterConfiguration.TimeoutSecondsForDIGTasks);

            // Process the results.
            var result = await ProcessDIGReadinessResultsAsync(dbGdaProvisionedDevices, apiResult, stoppingToken);

            // Update DbGdaOServiceTracking.
            await UpdateServiceTrackingRecordAsync();

            logger.Info($"Of the {result.TotalCount} records checked, {result.SuccessCount} are now DIG-ready and {result.FailureCount} are not yet ready (will be rechecked later).");

            logger.Trace($"Completed iteration of {methodBase?.ReflectedType?.Name}.{methodBase?.Name}");

            return result;
        }

        /// <summary>
        /// Retrieves a batch of <see cref="DbGdaProvisionedDevice"/> records from the adapter database 
        /// where DIG readiness has not yet been confirmed (IsOkayToSendDataToGeotab = false), 
        /// ordered by RecordLastChangedUtc to ensure fair rotation through all pending devices.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<List<DbGdaProvisionedDevice>> GetBatchOfDevicesToBeCheckedAsync(CancellationToken cancellationToken)
        {
            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<DbGdaProvisionedDevice>(adapterContext);

                var sql = BuildSqlToGetDevicesToBeChecked();
                var parameters = new { BatchSize = DeviceDIGReadinessServiceBatchSize };

                var results = await repo.QueryAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
                return results.ToList();
            }, new Context());
        }

        void HandleAdapterDatabaseConnectionException(AdapterDatabaseConnectionException ex)
        {
            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
            stateMachine.HandleException(ex, NLogLogLevelName.Error);
        }

        void HandleMyAdminConnectionException(MyAdminConnectionException ex)
        {
            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
            stateMachine.HandleException(ex, NLogLogLevelName.Error);
        }

        void HandleGenericException(Exception ex)
        {
            exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
            stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
        }

        /// <summary>
        /// Persists the DIG readiness check results to the database. All records in the batch are updated 
        /// (RecordLastChangedUtc is set) to ensure fair rotation. Devices that are DIG-ready also have 
        /// IsOkayToSendDataToGeotab set to true.
        /// </summary>
        /// <param name="devicesToUpdate">List of devices to update.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PersistDIGReadinessResultsAsync(List<DbGdaProvisionedDevice> devicesToUpdate, CancellationToken cancellationToken)
        {
            if (devicesToUpdate.Count == 0)
            {
                return;
            }

            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    await dbGdaProvisionedDeviceEntityPersister.PersistEntitiesToDatabaseAsync(
                        adapterContext, devicesToUpdate, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Info);

                    await adapterUOW.CommitAsync();

                    var readyCount = devicesToUpdate.Count(d => d.IsOkayToSendDataToGeotab);
                    var notReadyCount = devicesToUpdate.Count - readyCount;
                    logger.Debug($"Persisted DIG readiness results: {readyCount} devices marked as ready, {notReadyCount} devices updated for later recheck.");
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    await adapterUOW.RollBackAsync();
                    throw;
                }
            }, new Context());
        }

        /// <summary>
        /// Handles waiting for prerequisite services and/or database maintenance if required prior to continuing.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        async Task PrepareForExecutionAsync(CancellationToken stoppingToken)
        {
            var prerequisiteServices = new List<DIGAdapterService>
            {
                // Add any prerequisite services here if needed.
            };
            await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
            await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
        }

        /// <summary>
        /// Processes the DIG readiness API results. For devices with an OwnerDatabaseName, sets IsOkayToSendDataToGeotab to true.
        /// All devices in the batch have their RecordLastChangedUtc updated to ensure fair rotation through pending devices.
        /// </summary>
        /// <param name="dbGdaProvisionedDevices">The batch of devices that were checked.</param>
        /// <param name="apiResult">The result from the GetDeviceDatabaseNamesAsync API call.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<BatchProcessingResult> ProcessDIGReadinessResultsAsync(
            List<DbGdaProvisionedDevice> dbGdaProvisionedDevices,
            GetDeviceDatabaseNamesResult apiResult,
            CancellationToken cancellationToken)
        {
            int totalCount = dbGdaProvisionedDevices.Count;
            int successCount = 0;
            int notReadyCount = 0;

            // Build a lookup of serial numbers that have an OwnerDatabaseName (i.e., are DIG-ready).
            var digReadySerialNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (apiResult.ApiDeviceDatabaseOwnerShareds != null)
            {
                foreach (var deviceInfo in apiResult.ApiDeviceDatabaseOwnerShareds)
                {
                    if (!string.IsNullOrEmpty(deviceInfo.SerialNo) && !string.IsNullOrEmpty(deviceInfo.OwnerDatabaseName))
                    {
                        digReadySerialNumbers.Add(deviceInfo.SerialNo);
                    }
                }
            }

            // Prepare all devices for update.
            var devicesToUpdate = new List<DbGdaProvisionedDevice>();
            var now = DateTime.UtcNow;

            foreach (var device in dbGdaProvisionedDevices)
            {
                // Check if this device is DIG-ready.
                if (digReadySerialNumbers.Contains(device.GeotabSerialNumber))
                {
                    device.IsOkayToSendDataToGeotab = true;
                    successCount++;
                    logger.Debug($"Device '{device.GeotabSerialNumber}' (ThirdPartyId: '{device.ThirdPartyId}') is now DIG-ready.");
                }
                else
                {
                    // Device not ready yet - will be rechecked later.
                    notReadyCount++;
                }

                // Update RecordLastChangedUtc for all devices to ensure fair rotation.
                device.RecordLastChangedUtc = now;
                device.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                devicesToUpdate.Add(device);
            }

            // Persist all updates in a single transaction.
            await PersistDIGReadinessResultsAsync(devicesToUpdate, cancellationToken);

            var result = new BatchProcessingResult
            {
                TotalCount = totalCount,
                SuccessCount = successCount,
                FailureCount = notReadyCount // Not really "failures" - just not ready yet
            };

            // Set custom throttling logic. We don't want to immediately re-query and analyze the same records (ones that are not "DIG-ready").
            result.CustomShouldThrottleLogic = (total, success, threshold) =>
            {
                if (total > 0 && success == 0)
                {
                    return true;
                }
                return total < threshold;
            };

            return result;
        }

        /// <summary>
        /// Starts the current <see cref="DeviceDIGReadinessService"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbGdaOServiceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbGdaOServiceTrackings, DIGAdapterService.DeviceDIGReadinessService, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, DIGAdapterService.DeviceDIGReadinessService, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Register this service with the StateMachine.
            stateMachine.RegisterService(nameof(DeviceDIGReadinessService), adapterConfiguration.EnableDeviceDIGReadinessService);

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableDeviceDIGReadinessService)
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
        /// Stops the current <see cref="DeviceDIGReadinessService"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            stateMachine.RegisterService(nameof(DeviceDIGReadinessService), false);
            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the <see cref="DbGdaOServiceTracking"/> record associated with the <see cref="DeviceDIGReadinessService"/> service to show that the service is running.
        /// </summary>
        /// <returns></returns>
        async Task UpdateServiceTrackingRecordAsync()
        {
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                try
                {
                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(
                        adapterContext, DIGAdapterService.DeviceDIGReadinessService, DateTime.UtcNow);
                    await adapterUOW.CommitAsync();
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    await adapterUOW.RollBackAsync();
                    throw;
                }
            }, new Context());
        }
    }
}