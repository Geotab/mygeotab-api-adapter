using Dapper.Contrib.Extensions;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityMappers;
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
    /// A <see cref="BackgroundService"/> that provisions devices by processing records from the <see cref="nameof(DbGdaQProvisionDevice)"/> table and interacting with the MyAdmin API.
    /// </summary>
    class DeviceProvisioningService : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames;
        readonly IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DeviceProvisioningService> awaiter;
        readonly IExceptionHelper exceptionHelper;
        readonly IMyAdminAPIHelper myAdminAPIHelper;
        readonly IServiceTracker<DbGdaOServiceTracking> serviceTracker;
        readonly IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        // Entity Persisters
        readonly IGenericEntityPersister<DbGdaQProvisionDevice> dbGdaQProvisionDeviceEntityPersister;
        readonly IGenericEntityPersister<DbGdaQProvisionDeviceFail> dbGdaQProvisionDeviceFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaProvisionedDevice> dbGdaProvisionedDeviceEntityPersister;

        // Mappers
        readonly IDbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper dbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper;
        readonly IDbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper dbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        const int DeviceProvisioningBatchSize = 5000;
        const int StaleThresholdMinutes = 30;
        const int ExecutionThrottleEngagingRecordCount = 1;
        const int DefaultDeviceProvisioningServiceIntervalSeconds = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProvisioningService"/> class.
        /// </summary>
        public DeviceProvisioningService(
            IDIGAdapterConfiguration adapterConfiguration,
            IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames,
            IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment,
            IBackgroundServiceAwaiter<DeviceProvisioningService> awaiter,
            IExceptionHelper exceptionHelper,
            IMyAdminAPIHelper myAdminAPIHelper,
            IServiceTracker<DbGdaOServiceTracking> serviceTracker,
            IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext,
            IGenericEntityPersister<DbGdaQProvisionDevice> dbGdaQProvisionDeviceEntityPersister,
            IGenericEntityPersister<DbGdaQProvisionDeviceFail> dbGdaQProvisionDeviceFailEntityPersister,
            IGenericEntityPersister<DbGdaProvisionedDevice> dbGdaProvisionedDeviceEntityPersister,
            IDbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper dbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper,
            IDbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper dbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper)
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
            this.dbGdaQProvisionDeviceEntityPersister = dbGdaQProvisionDeviceEntityPersister;
            this.dbGdaQProvisionDeviceFailEntityPersister = dbGdaQProvisionDeviceFailEntityPersister;
            this.dbGdaProvisionedDeviceEntityPersister = dbGdaProvisionedDeviceEntityPersister;
            this.dbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper = dbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper;
            this.dbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper = dbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper;

            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Builds the SQL statement for executing the spClaimQProvisionDevicesBatch stored procedure/function based on the database provider type.
        /// </summary>
        /// <returns>The SQL statement formatted for the current database provider.</returns>
        string BuildSqlToExecuteSpClaimQProvisionDevicesBatch()
        {
            var storedProcedureName = digAdapterDatabaseObjectNames.SpClaimQProvisionDevicesBatchName;

            return adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT * FROM {SqlMapperExtensions.FormatTableNameForSql(storedProcedureName, "npgsqlconnection")}(@BatchSize::integer, @StaleThresholdMinutes::integer);",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $"EXEC {SqlMapperExtensions.FormatTableNameForSql(storedProcedureName, "sqlconnection")} @BatchSize = @BatchSize, @StaleThresholdMinutes = @StaleThresholdMinutes;",
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
            var delayTimeSpan = TimeSpan.FromSeconds(DefaultDeviceProvisioningServiceIntervalSeconds);

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

            // Retrieve a batch of DbGdaQProvisionDevice entities from the database (FIFO order by RecordCreationTimeUtc).
            var dbGdaQProvisionDevices = await GetBatchOfDevicesToBeProvisionedAsync(stoppingToken);

            if (!dbGdaQProvisionDevices.Any())
            {
                logger.Debug($"No records retrieved from {digAdapterDatabaseObjectNames.DbGdaQProvisionDeviceTableName} table.");

                // No records were returned, but the OServiceTracking record for this service still
                // needs to be updated to show that the service is operating. This is required for
                // dependent services that check ServiceIsRunningAsync/ServiceHasBeenRunAsync.
                await UpdateServiceTrackingRecordAsync();

                return new BatchProcessingResult
                {
                    TotalCount = 0,
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            logger.Info($"Retrieved {dbGdaQProvisionDevices.Count()} records from {digAdapterDatabaseObjectNames.DbGdaQProvisionDeviceTableName} table. Processing...");

            // Process the batch of DbGdaQProvisionDevice entities.
            var result = await ProcessBatchOfDevicesToBeProvisionedAsync(dbGdaQProvisionDevices, stoppingToken);

            // Update DbGdaOServiceTracking.
            await UpdateServiceTrackingRecordAsync();

            if (result.FailureCount > 0)
            {
                logger.Warn($"Of the {result.TotalCount} records from the {digAdapterDatabaseObjectNames.DbGdaQProvisionDeviceTableName} table, {result.SuccessCount} were successfully processed and {result.FailureCount} failed. Copies of any failed records have been inserted into the {digAdapterDatabaseObjectNames.DbGdaQProvisionDeviceFailTableName} table for reference.");
            }
            else
            {
                logger.Info($"Of the {result.TotalCount} records from the {digAdapterDatabaseObjectNames.DbGdaQProvisionDeviceTableName} table, {result.SuccessCount} were successfully processed and {result.FailureCount} failed.");
            }

            logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

            return result;
        }

        /// <summary>
        /// Atomically claims and retrieves a batch of <see cref="DbGdaQProvisionDevice"/> records from the adapter database. Uses a stored procedure/function that implements atomic claim semantics with row locking to prevent duplicate processing in multi-instance scenarios and to ensure crash recovery.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<IEnumerable<DbGdaQProvisionDevice>> GetBatchOfDevicesToBeProvisionedAsync(CancellationToken cancellationToken)
        {
            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<DbGdaQProvisionDevice>(adapterContext);

                var sql = BuildSqlToExecuteSpClaimQProvisionDevicesBatch();
                var parameters = new
                {
                    BatchSize = DeviceProvisioningBatchSize,
                    StaleThresholdMinutes
                };

                return await repo.QueryAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
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
        /// Persists the results of device provisioning operations to the database in batches. Successful provisions are moved to ProvisionedDevices, failures to Q_ProvisionDevicesFail, and processed records are deleted from Q_ProvisionDevices.
        /// </summary>
        /// <param name="successfulProvisions">List of successfully provisioned devices with their serial numbers.</param>
        /// <param name="failedProvisions">List of failed provisions with their failure reasons.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PersistDeviceProvisioningResultsAsync(
            List<(DbGdaQProvisionDevice QueueRecord, string GeotabSerialNumber)> successfulProvisions,
            List<(DbGdaQProvisionDevice QueueRecord, string FailureReason)> failedProvisions,
            CancellationToken cancellationToken)
        {
            if (!successfulProvisions.Any() && !failedProvisions.Any())
            {
                return;
            }

            // Prepare entities for successful provisions.
            var provisionedDevices = new List<DbGdaProvisionedDevice>();
            var queueRecordsToDelete = new List<DbGdaQProvisionDevice>();

            foreach (var (queueRecord, serialNumber) in successfulProvisions)
            {
                var provisionedDevice = dbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper.CreateEntity(queueRecord, serialNumber);
                provisionedDevices.Add(provisionedDevice);

                queueRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                queueRecordsToDelete.Add(queueRecord);
            }

            // Prepare entities for failed provisions.
            var failedDevices = new List<DbGdaQProvisionDeviceFail>();

            foreach (var (queueRecord, failureReason) in failedProvisions)
            {
                var failedDevice = dbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper.CreateEntity(queueRecord, failureReason);
                failedDevices.Add(failedDevice);

                queueRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                queueRecordsToDelete.Add(queueRecord);
            }

            // Persist to database within a single transaction.
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    // Insert successful provisions into ProvisionedDevices.
                    if (provisionedDevices.Count != 0)
                    {
                        await dbGdaProvisionedDeviceEntityPersister.PersistEntitiesToDatabaseAsync(
                            adapterContext, provisionedDevices, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Info);
                    }

                    // Insert failed provisions into Q_ProvisionDevicesFail.
                    if (failedDevices.Count != 0)
                    {
                        await dbGdaQProvisionDeviceFailEntityPersister.PersistEntitiesToDatabaseAsync(
                            adapterContext, failedDevices, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Info);
                    }

                    // Delete processed records from Q_ProvisionDevices.
                    if (queueRecordsToDelete.Count != 0)
                    {
                        await dbGdaQProvisionDeviceEntityPersister.PersistEntitiesToDatabaseAsync(
                            adapterContext, queueRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Info);
                    }

                    await adapterUOW.CommitAsync();

                    logger.Debug($"Persisted batch: {provisionedDevices.Count} successful, {failedDevices.Count} failed, {queueRecordsToDelete.Count} deleted from queue.");
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
        /// Processes a batch of <see cref="DbGdaQProvisionDevice"/> entities with per-device persistence. After each device is provisioned via MyAdmin, its result is immediately persisted to the database to prevent orphaned provisions if the adapter crashes mid-batch.
        /// </summary>
        /// <param name="dbGdaQProvisionDevices">The batch of <see cref="DbGdaQProvisionDevice"/> entities to be processed.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<BatchProcessingResult> ProcessBatchOfDevicesToBeProvisionedAsync(IEnumerable<DbGdaQProvisionDevice> dbGdaQProvisionDevices, CancellationToken cancellationToken)
        {
            int totalCount = dbGdaQProvisionDevices.Count();
            int successCount = 0;
            int failureCount = 0;

            foreach (var dbGdaQProvisionDevice in dbGdaQProvisionDevices)
            {
                try
                {
                    var result = await myAdminAPIHelper.ProvisionDeviceToAccountAsync(
                        dbGdaQProvisionDevice,
                        adapterConfiguration.TimeoutSecondsForDIGTasks);

                    var successfulProvisions = new List<(DbGdaQProvisionDevice QueueRecord, string GeotabSerialNumber)>();
                    var failedProvisions = new List<(DbGdaQProvisionDevice QueueRecord, string FailureReason)>();

                    if (result.IsSuccess)
                    {
                        successCount++;
                        successfulProvisions.Add((dbGdaQProvisionDevice, result.GeotabSerialNumber));
                    }
                    else
                    {
                        failureCount++;
                        var failureReason = $"[Source: {result.ErrorSource}] {result.ErrorMessage}";
                        failedProvisions.Add((dbGdaQProvisionDevice, failureReason));
                    }

                    // Persist this device's result immediately.
                    await PersistDeviceProvisioningResultsAsync(successfulProvisions, failedProvisions, cancellationToken);
                }
                catch (MyAdminConnectionException)
                {
                    // Re-throw connectivity exceptions so the main loop's typed catch handler can set the appropriate state reason.
                    throw;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"{DefaultErrorMessagePrefix} while provisioning device '{dbGdaQProvisionDevice.ThirdPartyId}'");

                    var failedProvisions = new List<(DbGdaQProvisionDevice QueueRecord, string FailureReason)>
                    {
                        (dbGdaQProvisionDevice, $"[Source: {ErrorSource.Middleware}] {ex.Message}")
                    };
                    await PersistDeviceProvisioningResultsAsync(new List<(DbGdaQProvisionDevice, string)>(), failedProvisions, cancellationToken);
                }
            }

            return new BatchProcessingResult
            {
                TotalCount = totalCount,
                SuccessCount = successCount,
                FailureCount = failureCount
            };
        }

        /// <summary>
        /// Starts the current <see cref="DeviceProvisioningService"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbGdaOServiceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbGdaOServiceTrackings, DIGAdapterService.DeviceProvisioningService, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, DIGAdapterService.DeviceProvisioningService, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            stateMachine.RegisterService(nameof(DeviceProvisioningService), adapterConfiguration.EnableDeviceProvisioningService);

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableDeviceProvisioningService)
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
        /// Stops the current <see cref="DeviceProvisioningService"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            stateMachine.RegisterService(nameof(DeviceProvisioningService), false);
            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the <see cref="DbGdaOServiceTracking"/> record associated with the <see cref="DeviceProvisioningService"/> service to show that the service is running.
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
                        adapterContext, DIGAdapterService.DeviceProvisioningService, DateTime.UtcNow);
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