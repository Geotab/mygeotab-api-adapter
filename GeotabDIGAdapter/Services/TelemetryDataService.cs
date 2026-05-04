using Dapper.Contrib.Extensions;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI;
using MyGeotabAPIAdapter.DIGAPI.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.Models;
using NLog;
using Polly;
using Polly.Retry;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that processes telemetry records from queue tables and posts them to the DIG API.
    /// </summary>
    class TelemetryDataService : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames;
        readonly IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<TelemetryDataService> awaiter;
        readonly IExceptionHelper exceptionHelper;
        readonly IDIGAPIHelper digAPIHelper;
        readonly IProvisionedDeviceCache provisionedDeviceCache;
        readonly IServiceTracker<DbGdaOServiceTracking> serviceTracker;
        readonly IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        // Entity Persisters for queue records
        readonly IGenericEntityPersister<DbGdaQGpsRecord> dbGdaQGpsRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQGpsRecordFail> dbGdaQGpsRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQAccelerationRecord> dbGdaQAccelerationRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQAccelerationRecordFail> dbGdaQAccelerationRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQBinaryRecord> dbGdaQBinaryRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQBinaryRecordFail> dbGdaQBinaryRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQBluetoothRecord> dbGdaQBluetoothRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQBluetoothRecordFail> dbGdaQBluetoothRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQDriverChangeRecord> dbGdaQDriverChangeRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQDriverChangeRecordFail> dbGdaQDriverChangeRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQGenericFaultRecord> dbGdaQGenericFaultRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQGenericFaultRecordFail> dbGdaQGenericFaultRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQGenericStatusRecord> dbGdaQGenericStatusRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQGenericStatusRecordFail> dbGdaQGenericStatusRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQJ1708FaultRecord> dbGdaQJ1708FaultRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQJ1708FaultRecordFail> dbGdaQJ1708FaultRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQJ1939FaultRecord> dbGdaQJ1939FaultRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQJ1939FaultRecordFail> dbGdaQJ1939FaultRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQObdiiFaultRecord> dbGdaQObdiiFaultRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQObdiiFaultRecordFail> dbGdaQObdiiFaultRecordFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaQVinRecord> dbGdaQVinRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaQVinRecordFail> dbGdaQVinRecordFailEntityPersister;

        // Queue to DIG record mappers
        readonly IDbGdaQGpsRecordDIGGpsRecordEntityMapper dbGdaQGpsRecordDIGGpsRecordEntityMapper;
        readonly IDbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper dbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper;
        readonly IDbGdaQBinaryRecordDIGBinaryRecordEntityMapper dbGdaQBinaryRecordDIGBinaryRecordEntityMapper;
        readonly IDbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper dbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper;
        readonly IDbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper dbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper;
        readonly IDbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper dbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper;
        readonly IDbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper dbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper;
        readonly IDbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper dbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper;
        readonly IDbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper dbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper;
        readonly IDbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper dbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper;
        readonly IDbGdaQVinRecordDIGVinRecordEntityMapper dbGdaQVinRecordDIGVinRecordEntityMapper;

        // Queue to fail record mappers
        readonly IDbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper dbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper;
        readonly IDbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper dbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper;
        readonly IDbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper dbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper;
        readonly IDbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper dbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper;
        readonly IDbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper dbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper;
        readonly IDbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper dbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper;
        readonly IDbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper dbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper;
        readonly IDbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper dbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper;
        readonly IDbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper dbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper;
        readonly IDbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper dbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper;
        readonly IDbGdaQVinRecordDbGdaQVinRecordFailEntityMapper dbGdaQVinRecordDbGdaQVinRecordFailEntityMapper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        const int MaxRecordsPerDIGAPICall = 5000;
        const int StaleThresholdMinutes = 30;
        const int ExecutionThrottleEngagingRecordCount = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryDataService"/> class.
        /// </summary>
        public TelemetryDataService(
            IDIGAdapterConfiguration adapterConfiguration,
            IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames,
            IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment,
            IBackgroundServiceAwaiter<TelemetryDataService> awaiter,
            IExceptionHelper exceptionHelper,
            IDIGAPIHelper digAPIHelper,
            IProvisionedDeviceCache provisionedDeviceCache,
            IServiceTracker<DbGdaOServiceTracking> serviceTracker,
            IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext,
            // Entity persisters
            IGenericEntityPersister<DbGdaQGpsRecord> dbGdaQGpsRecordEntityPersister,
            IGenericEntityPersister<DbGdaQGpsRecordFail> dbGdaQGpsRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQAccelerationRecord> dbGdaQAccelerationRecordEntityPersister,
            IGenericEntityPersister<DbGdaQAccelerationRecordFail> dbGdaQAccelerationRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQBinaryRecord> dbGdaQBinaryRecordEntityPersister,
            IGenericEntityPersister<DbGdaQBinaryRecordFail> dbGdaQBinaryRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQBluetoothRecord> dbGdaQBluetoothRecordEntityPersister,
            IGenericEntityPersister<DbGdaQBluetoothRecordFail> dbGdaQBluetoothRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQDriverChangeRecord> dbGdaQDriverChangeRecordEntityPersister,
            IGenericEntityPersister<DbGdaQDriverChangeRecordFail> dbGdaQDriverChangeRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQGenericFaultRecord> dbGdaQGenericFaultRecordEntityPersister,
            IGenericEntityPersister<DbGdaQGenericFaultRecordFail> dbGdaQGenericFaultRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQGenericStatusRecord> dbGdaQGenericStatusRecordEntityPersister,
            IGenericEntityPersister<DbGdaQGenericStatusRecordFail> dbGdaQGenericStatusRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQJ1708FaultRecord> dbGdaQJ1708FaultRecordEntityPersister,
            IGenericEntityPersister<DbGdaQJ1708FaultRecordFail> dbGdaQJ1708FaultRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQJ1939FaultRecord> dbGdaQJ1939FaultRecordEntityPersister,
            IGenericEntityPersister<DbGdaQJ1939FaultRecordFail> dbGdaQJ1939FaultRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQObdiiFaultRecord> dbGdaQObdiiFaultRecordEntityPersister,
            IGenericEntityPersister<DbGdaQObdiiFaultRecordFail> dbGdaQObdiiFaultRecordFailEntityPersister,
            IGenericEntityPersister<DbGdaQVinRecord> dbGdaQVinRecordEntityPersister,
            IGenericEntityPersister<DbGdaQVinRecordFail> dbGdaQVinRecordFailEntityPersister,
            // Queue to DIG mappers
            IDbGdaQGpsRecordDIGGpsRecordEntityMapper dbGdaQGpsRecordDIGGpsRecordEntityMapper,
            IDbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper dbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper,
            IDbGdaQBinaryRecordDIGBinaryRecordEntityMapper dbGdaQBinaryRecordDIGBinaryRecordEntityMapper,
            IDbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper dbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper,
            IDbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper dbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper,
            IDbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper dbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper,
            IDbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper dbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper,
            IDbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper dbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper,
            IDbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper dbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper,
            IDbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper dbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper,
            IDbGdaQVinRecordDIGVinRecordEntityMapper dbGdaQVinRecordDIGVinRecordEntityMapper,
            // Queue to fail mappers
            IDbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper dbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper,
            IDbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper dbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper,
            IDbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper dbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper,
            IDbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper dbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper,
            IDbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper dbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper,
            IDbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper dbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper,
            IDbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper dbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper,
            IDbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper dbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper,
            IDbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper dbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper,
            IDbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper dbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper,
            IDbGdaQVinRecordDbGdaQVinRecordFailEntityMapper dbGdaQVinRecordDbGdaQVinRecordFailEntityMapper)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.digAdapterDatabaseObjectNames = digAdapterDatabaseObjectNames;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.digAPIHelper = digAPIHelper;
            this.provisionedDeviceCache = provisionedDeviceCache;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.adapterContext = adapterContext;

            // Entity persisters
            this.dbGdaQGpsRecordEntityPersister = dbGdaQGpsRecordEntityPersister;
            this.dbGdaQGpsRecordFailEntityPersister = dbGdaQGpsRecordFailEntityPersister;
            this.dbGdaQAccelerationRecordEntityPersister = dbGdaQAccelerationRecordEntityPersister;
            this.dbGdaQAccelerationRecordFailEntityPersister = dbGdaQAccelerationRecordFailEntityPersister;
            this.dbGdaQBinaryRecordEntityPersister = dbGdaQBinaryRecordEntityPersister;
            this.dbGdaQBinaryRecordFailEntityPersister = dbGdaQBinaryRecordFailEntityPersister;
            this.dbGdaQBluetoothRecordEntityPersister = dbGdaQBluetoothRecordEntityPersister;
            this.dbGdaQBluetoothRecordFailEntityPersister = dbGdaQBluetoothRecordFailEntityPersister;
            this.dbGdaQDriverChangeRecordEntityPersister = dbGdaQDriverChangeRecordEntityPersister;
            this.dbGdaQDriverChangeRecordFailEntityPersister = dbGdaQDriverChangeRecordFailEntityPersister;
            this.dbGdaQGenericFaultRecordEntityPersister = dbGdaQGenericFaultRecordEntityPersister;
            this.dbGdaQGenericFaultRecordFailEntityPersister = dbGdaQGenericFaultRecordFailEntityPersister;
            this.dbGdaQGenericStatusRecordEntityPersister = dbGdaQGenericStatusRecordEntityPersister;
            this.dbGdaQGenericStatusRecordFailEntityPersister = dbGdaQGenericStatusRecordFailEntityPersister;
            this.dbGdaQJ1708FaultRecordEntityPersister = dbGdaQJ1708FaultRecordEntityPersister;
            this.dbGdaQJ1708FaultRecordFailEntityPersister = dbGdaQJ1708FaultRecordFailEntityPersister;
            this.dbGdaQJ1939FaultRecordEntityPersister = dbGdaQJ1939FaultRecordEntityPersister;
            this.dbGdaQJ1939FaultRecordFailEntityPersister = dbGdaQJ1939FaultRecordFailEntityPersister;
            this.dbGdaQObdiiFaultRecordEntityPersister = dbGdaQObdiiFaultRecordEntityPersister;
            this.dbGdaQObdiiFaultRecordFailEntityPersister = dbGdaQObdiiFaultRecordFailEntityPersister;
            this.dbGdaQVinRecordEntityPersister = dbGdaQVinRecordEntityPersister;
            this.dbGdaQVinRecordFailEntityPersister = dbGdaQVinRecordFailEntityPersister;

            // Queue to DIG mappers
            this.dbGdaQGpsRecordDIGGpsRecordEntityMapper = dbGdaQGpsRecordDIGGpsRecordEntityMapper;
            this.dbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper = dbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper;
            this.dbGdaQBinaryRecordDIGBinaryRecordEntityMapper = dbGdaQBinaryRecordDIGBinaryRecordEntityMapper;
            this.dbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper = dbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper;
            this.dbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper = dbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper;
            this.dbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper = dbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper;
            this.dbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper = dbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper;
            this.dbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper = dbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper;
            this.dbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper = dbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper;
            this.dbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper = dbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper;
            this.dbGdaQVinRecordDIGVinRecordEntityMapper = dbGdaQVinRecordDIGVinRecordEntityMapper;

            // Queue to fail mappers
            this.dbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper = dbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper;
            this.dbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper = dbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper;
            this.dbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper = dbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper;
            this.dbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper = dbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper;
            this.dbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper = dbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper;
            this.dbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper = dbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper;
            this.dbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper = dbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper;
            this.dbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper = dbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper;
            this.dbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper = dbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper;
            this.dbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper = dbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper;
            this.dbGdaQVinRecordDbGdaQVinRecordFailEntityMapper = dbGdaQVinRecordDbGdaQVinRecordFailEntityMapper;

            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Builds the SQL statement for executing a claim batch stored procedure based on the database provider type.
        /// </summary>
        string BuildSqlToExecuteSpClaimBatch(string storedProcedureName)
        {
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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.TelemetryDataServiceIntervalSeconds);

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
                catch (DIGConnectionException digConnectionException)
                {
                    HandleDIGConnectionException(digConnectionException);
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
        async Task<BatchProcessingResult> ExecuteIterationBusinessLogicAsync(CancellationToken stoppingToken)
        {
            MethodBase? methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Started iteration of {methodBase?.ReflectedType?.Name}.{methodBase?.Name}");

            var iterationStopwatch = Stopwatch.StartNew();
            int totalCount = 0;
            int successCount = 0;
            int failureCount = 0;
            long totalApiLatencyMs = 0;
            int apiCallCount = 0;

            // Per-record-type counters
            var recordTypeCounts = new Dictionary<RecordType, int>();
            foreach (RecordType rt in Enum.GetValues(typeof(RecordType)))
            {
                recordTypeCounts[rt] = 0;
            }

            // Build ThirdPartyId to SerialNo lookup from ProvisionedDevices
            var thirdPartyIdToSerialNoMap = await provisionedDeviceCache.GetThirdPartyIdToSerialNoMapAsync(stoppingToken);
            if (thirdPartyIdToSerialNoMap.Count == 0)
            {
                logger.Debug("No provisioned devices with IsOkayToSendDataToGeotab=true found. Skipping telemetry processing.");

                // No records were processed, but the OServiceTracking record for this service still
                // needs to be updated to show that the service is operating.
                await UpdateServiceTrackingRecordAsync(0, 0, 0);

                return new BatchProcessingResult { TotalCount = 0, SuccessCount = 0, FailureCount = 0 };
            }

            // Collect records from all enabled record types
            var allDigRecords = new List<object>();
            var allQueueRecordsToProcess = new List<QueueRecordProcessingContext>();

            // Process each enabled record type
            if (adapterConfiguration.EnableGpsRecords)
            {
                await CollectGpsRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableAccelerationRecords)
            {
                await CollectAccelerationRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableBinaryRecords)
            {
                await CollectBinaryRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableBluetoothRecords)
            {
                await CollectBluetoothRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableDriverChangeRecords)
            {
                await CollectDriverChangeRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableGenericFaultRecords)
            {
                await CollectGenericFaultRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableGenericStatusRecords)
            {
                await CollectGenericStatusRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableJ1708FaultRecords)
            {
                await CollectJ1708FaultRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableJ1939FaultRecords)
            {
                await CollectJ1939FaultRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableObdiiFaultRecords)
            {
                await CollectObdiiFaultRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }
            if (adapterConfiguration.EnableVinRecords)
            {
                await CollectVinRecordsAsync(thirdPartyIdToSerialNoMap, allDigRecords, allQueueRecordsToProcess, stoppingToken);
            }

            // Separate ImmediateFailure records (no matching ThirdPartyId) from valid records
            var immediateFailureContexts = allQueueRecordsToProcess.Where(c => c.ImmediateFailure).ToList();
            var validContexts = allQueueRecordsToProcess.Where(c => !c.ImmediateFailure).ToList();

            // Persist ImmediateFailure records to fail tables immediately
            if (immediateFailureContexts.Count > 0)
            {
                logger.Warn($"{immediateFailureContexts.Count} record(s) have no matching provisioned device and will be moved to fail tables.");
                await PersistFailedRecordsAsync(immediateFailureContexts, string.Empty, stoppingToken);
                totalCount += immediateFailureContexts.Count;
                failureCount += immediateFailureContexts.Count;
            }

            if (allDigRecords.Count == 0)
            {
                // No valid records to post, but update tracking to show the service is operating
                // (totalCount and failureCount may be non-zero from ImmediateFailure records)
                await UpdateServiceTrackingRecordAsync(totalCount, 0, failureCount);

                iterationStopwatch.Stop();
                logger.Debug($"No telemetry records to process. Iteration completed in {iterationStopwatch.ElapsedMilliseconds}ms.");
                return new BatchProcessingResult { TotalCount = totalCount, SuccessCount = 0, FailureCount = failureCount };
            }

            // Count records per type from validContexts
            foreach (var context in validContexts)
            {
                recordTypeCounts[context.RecordType]++;
            }

            // Log per-record-type breakdown
            var typeBreakdown = BuildRecordTypeBreakdownString(recordTypeCounts);
            logger.Info($"Collected {allDigRecords.Count} telemetry record(s) for posting to DIG API. {typeBreakdown}");

            // Post records to DIG API in batches of max 5000
            // Note: allDigRecords and validContexts are synchronized (same count, same order)
            for (int i = 0; i < allDigRecords.Count; i += MaxRecordsPerDIGAPICall)
            {
                var batchRecords = allDigRecords.Skip(i).Take(MaxRecordsPerDIGAPICall).ToList();
                var batchContexts = validContexts.Skip(i).Take(MaxRecordsPerDIGAPICall).ToList();

                totalCount += batchRecords.Count;

                var postResult = await digAPIHelper.PostRecordsAsync(batchRecords, adapterConfiguration.TimeoutSecondsForDIGTasks);

                // Track API latency
                totalApiLatencyMs += postResult.ElapsedMilliseconds;
                apiCallCount++;

                if (postResult.IsSuccess)
                {
                    logger.Debug($"Successfully posted {batchRecords.Count} record(s) to DIG API. TrackingId: {postResult.TrackingId}, Latency: {postResult.ElapsedMilliseconds}ms");
                    successCount += batchRecords.Count;

                    // Delete successfully processed records from queue tables
                    await PersistSuccessfulRecordsAsync(batchContexts, stoppingToken);
                }
                else
                {
                    logger.Warn($"Failed to post {batchRecords.Count} record(s) to DIG API: {postResult.ErrorMessage} (Latency: {postResult.ElapsedMilliseconds}ms)");
                    failureCount += batchRecords.Count;

                    // Move failed records to fail tables
                    var failureReason = $"[Source: {postResult.ErrorSource}] {postResult.ErrorMessage}";
                    await PersistFailedRecordsAsync(batchContexts, failureReason, stoppingToken);
                }
            }

            // Update DbGdaOServiceTracking with batch metrics
            await UpdateServiceTrackingRecordAsync(totalCount, successCount, failureCount);

            iterationStopwatch.Stop();
            var avgLatencyMs = apiCallCount > 0 ? totalApiLatencyMs / apiCallCount : 0;

            if (failureCount > 0)
            {
                logger.Warn($"Telemetry processing complete. Total: {totalCount}, Success: {successCount}, Failed: {failureCount}. " +
                    $"API calls: {apiCallCount}, Avg latency: {avgLatencyMs}ms, Iteration: {iterationStopwatch.ElapsedMilliseconds}ms. {typeBreakdown}");
            }
            else if (totalCount > 0)
            {
                logger.Info($"Telemetry processing complete. Total: {totalCount}, Success: {successCount}, Failed: {failureCount}. " +
                    $"API calls: {apiCallCount}, Avg latency: {avgLatencyMs}ms, Iteration: {iterationStopwatch.ElapsedMilliseconds}ms. {typeBreakdown}");
            }

            logger.Trace($"Completed iteration of {methodBase?.ReflectedType?.Name}.{methodBase?.Name}");

            return new BatchProcessingResult
            {
                TotalCount = totalCount,
                SuccessCount = successCount,
                FailureCount = failureCount
            };
        }

        #region Record Collection Methods

        async Task CollectGpsRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQGpsRecord>(
                digAdapterDatabaseObjectNames.SpClaimQGpsRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQGpsRecordDIGGpsRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.Gps, QueueRecord = record });
                }
                else
                {
                    // No provisioned device found - move to fail table
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.Gps,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectAccelerationRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQAccelerationRecord>(
                digAdapterDatabaseObjectNames.SpClaimQAccelerationRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.Acceleration, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.Acceleration,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectBinaryRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQBinaryRecord>(
                digAdapterDatabaseObjectNames.SpClaimQBinaryRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQBinaryRecordDIGBinaryRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.Binary, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.Binary,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectBluetoothRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQBluetoothRecord>(
                digAdapterDatabaseObjectNames.SpClaimQBluetoothRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.Bluetooth, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.Bluetooth,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectDriverChangeRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQDriverChangeRecord>(
                digAdapterDatabaseObjectNames.SpClaimQDriverChangeRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.DriverChange, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.DriverChange,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectGenericFaultRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQGenericFaultRecord>(
                digAdapterDatabaseObjectNames.SpClaimQGenericFaultRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.GenericFault, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.GenericFault,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectGenericStatusRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQGenericStatusRecord>(
                digAdapterDatabaseObjectNames.SpClaimQGenericStatusRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.GenericStatus, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.GenericStatus,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectJ1708FaultRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQJ1708FaultRecord>(
                digAdapterDatabaseObjectNames.SpClaimQJ1708FaultRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.J1708Fault, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.J1708Fault,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectJ1939FaultRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQJ1939FaultRecord>(
                digAdapterDatabaseObjectNames.SpClaimQJ1939FaultRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.J1939Fault, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.J1939Fault,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectObdiiFaultRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQObdiiFaultRecord>(
                digAdapterDatabaseObjectNames.SpClaimQObdiiFaultRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.ObdiiFault, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.ObdiiFault,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        async Task CollectVinRecordsAsync(Dictionary<string, string> thirdPartyIdMap, List<object> digRecords, List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            var queueRecords = await GetBatchFromQueueAsync<DbGdaQVinRecord>(
                digAdapterDatabaseObjectNames.SpClaimQVinRecordsBatchName, cancellationToken);

            foreach (var record in queueRecords)
            {
                if (thirdPartyIdMap.TryGetValue(record.ThirdPartyId, out var serialNo))
                {
                    var digRecord = dbGdaQVinRecordDIGVinRecordEntityMapper.CreateEntity(record, serialNo);
                    digRecords.Add(digRecord);
                    contexts.Add(new QueueRecordProcessingContext { RecordType = RecordType.Vin, QueueRecord = record });
                }
                else
                {
                    contexts.Add(new QueueRecordProcessingContext
                    {
                        RecordType = RecordType.Vin,
                        QueueRecord = record,
                        ImmediateFailure = true,
                        FailureReason = $"No provisioned device found for ThirdPartyId: {record.ThirdPartyId}"
                    });
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets a batch of records from a queue table using the specified stored procedure.
        /// </summary>
        async Task<IEnumerable<T>> GetBatchFromQueueAsync<T>(string storedProcedureName, CancellationToken cancellationToken) where T : class
        {
            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<T>(adapterContext);

                var sql = BuildSqlToExecuteSpClaimBatch(storedProcedureName);
                var parameters = new
                {
                    BatchSize = adapterConfiguration.TelemetryDataServiceBatchSizePerType,
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

        void HandleDIGConnectionException(DIGConnectionException ex)
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
        /// Persists successfully processed records by deleting them from queue tables.
        /// </summary>
        async Task PersistSuccessfulRecordsAsync(List<QueueRecordProcessingContext> contexts, CancellationToken cancellationToken)
        {
            // Group by record type and persist
            var gpsRecords = contexts.Where(c => c.RecordType == RecordType.Gps).Select(c => (DbGdaQGpsRecord)c.QueueRecord).ToList();
            var accelerationRecords = contexts.Where(c => c.RecordType == RecordType.Acceleration).Select(c => (DbGdaQAccelerationRecord)c.QueueRecord).ToList();
            var binaryRecords = contexts.Where(c => c.RecordType == RecordType.Binary).Select(c => (DbGdaQBinaryRecord)c.QueueRecord).ToList();
            var bluetoothRecords = contexts.Where(c => c.RecordType == RecordType.Bluetooth).Select(c => (DbGdaQBluetoothRecord)c.QueueRecord).ToList();
            var driverChangeRecords = contexts.Where(c => c.RecordType == RecordType.DriverChange).Select(c => (DbGdaQDriverChangeRecord)c.QueueRecord).ToList();
            var genericFaultRecords = contexts.Where(c => c.RecordType == RecordType.GenericFault).Select(c => (DbGdaQGenericFaultRecord)c.QueueRecord).ToList();
            var genericStatusRecords = contexts.Where(c => c.RecordType == RecordType.GenericStatus).Select(c => (DbGdaQGenericStatusRecord)c.QueueRecord).ToList();
            var j1708FaultRecords = contexts.Where(c => c.RecordType == RecordType.J1708Fault).Select(c => (DbGdaQJ1708FaultRecord)c.QueueRecord).ToList();
            var j1939FaultRecords = contexts.Where(c => c.RecordType == RecordType.J1939Fault).Select(c => (DbGdaQJ1939FaultRecord)c.QueueRecord).ToList();
            var obdiiFaultRecords = contexts.Where(c => c.RecordType == RecordType.ObdiiFault).Select(c => (DbGdaQObdiiFaultRecord)c.QueueRecord).ToList();
            var vinRecords = contexts.Where(c => c.RecordType == RecordType.Vin).Select(c => (DbGdaQVinRecord)c.QueueRecord).ToList();

            // Mark all for deletion
            foreach (var r in gpsRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in accelerationRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in binaryRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in bluetoothRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in driverChangeRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in genericFaultRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in genericStatusRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in j1708FaultRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in j1939FaultRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in obdiiFaultRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
            foreach (var r in vinRecords) r.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;

            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    if (gpsRecords.Count > 0) await dbGdaQGpsRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, gpsRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (accelerationRecords.Count > 0) await dbGdaQAccelerationRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, accelerationRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (binaryRecords.Count > 0) await dbGdaQBinaryRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, binaryRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (bluetoothRecords.Count > 0) await dbGdaQBluetoothRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, bluetoothRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (driverChangeRecords.Count > 0) await dbGdaQDriverChangeRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, driverChangeRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (genericFaultRecords.Count > 0) await dbGdaQGenericFaultRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, genericFaultRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (genericStatusRecords.Count > 0) await dbGdaQGenericStatusRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, genericStatusRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (j1708FaultRecords.Count > 0) await dbGdaQJ1708FaultRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, j1708FaultRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (j1939FaultRecords.Count > 0) await dbGdaQJ1939FaultRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, j1939FaultRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (obdiiFaultRecords.Count > 0) await dbGdaQObdiiFaultRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, obdiiFaultRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (vinRecords.Count > 0) await dbGdaQVinRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, vinRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);

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

        /// <summary>
        /// Persists failed records by moving them to fail tables and deleting from queue tables.
        /// </summary>
        async Task PersistFailedRecordsAsync(List<QueueRecordProcessingContext> contexts, string failureReason, CancellationToken cancellationToken)
        {
            // Create fail records and mark queue records for deletion
            var gpsFailRecords = new List<DbGdaQGpsRecordFail>();
            var accelerationFailRecords = new List<DbGdaQAccelerationRecordFail>();
            var binaryFailRecords = new List<DbGdaQBinaryRecordFail>();
            var bluetoothFailRecords = new List<DbGdaQBluetoothRecordFail>();
            var driverChangeFailRecords = new List<DbGdaQDriverChangeRecordFail>();
            var genericFaultFailRecords = new List<DbGdaQGenericFaultRecordFail>();
            var genericStatusFailRecords = new List<DbGdaQGenericStatusRecordFail>();
            var j1708FaultFailRecords = new List<DbGdaQJ1708FaultRecordFail>();
            var j1939FaultFailRecords = new List<DbGdaQJ1939FaultRecordFail>();
            var obdiiFaultFailRecords = new List<DbGdaQObdiiFaultRecordFail>();
            var vinFailRecords = new List<DbGdaQVinRecordFail>();

            var gpsRecordsToDelete = new List<DbGdaQGpsRecord>();
            var accelerationRecordsToDelete = new List<DbGdaQAccelerationRecord>();
            var binaryRecordsToDelete = new List<DbGdaQBinaryRecord>();
            var bluetoothRecordsToDelete = new List<DbGdaQBluetoothRecord>();
            var driverChangeRecordsToDelete = new List<DbGdaQDriverChangeRecord>();
            var genericFaultRecordsToDelete = new List<DbGdaQGenericFaultRecord>();
            var genericStatusRecordsToDelete = new List<DbGdaQGenericStatusRecord>();
            var j1708FaultRecordsToDelete = new List<DbGdaQJ1708FaultRecord>();
            var j1939FaultRecordsToDelete = new List<DbGdaQJ1939FaultRecord>();
            var obdiiFaultRecordsToDelete = new List<DbGdaQObdiiFaultRecord>();
            var vinRecordsToDelete = new List<DbGdaQVinRecord>();

            foreach (var context in contexts)
            {
                var reason = context.ImmediateFailure ? context.FailureReason : failureReason;

                switch (context.RecordType)
                {
                    case RecordType.Gps:
                        var gpsRecord = (DbGdaQGpsRecord)context.QueueRecord;
                        gpsFailRecords.Add(dbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper.CreateEntity(gpsRecord, reason));
                        gpsRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        gpsRecordsToDelete.Add(gpsRecord);
                        break;
                    case RecordType.Acceleration:
                        var accelerationRecord = (DbGdaQAccelerationRecord)context.QueueRecord;
                        accelerationFailRecords.Add(dbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper.CreateEntity(accelerationRecord, reason));
                        accelerationRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        accelerationRecordsToDelete.Add(accelerationRecord);
                        break;
                    case RecordType.Binary:
                        var binaryRecord = (DbGdaQBinaryRecord)context.QueueRecord;
                        binaryFailRecords.Add(dbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper.CreateEntity(binaryRecord, reason));
                        binaryRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        binaryRecordsToDelete.Add(binaryRecord);
                        break;
                    case RecordType.Bluetooth:
                        var bluetoothRecord = (DbGdaQBluetoothRecord)context.QueueRecord;
                        bluetoothFailRecords.Add(dbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper.CreateEntity(bluetoothRecord, reason));
                        bluetoothRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        bluetoothRecordsToDelete.Add(bluetoothRecord);
                        break;
                    case RecordType.DriverChange:
                        var driverChangeRecord = (DbGdaQDriverChangeRecord)context.QueueRecord;
                        driverChangeFailRecords.Add(dbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper.CreateEntity(driverChangeRecord, reason));
                        driverChangeRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        driverChangeRecordsToDelete.Add(driverChangeRecord);
                        break;
                    case RecordType.GenericFault:
                        var genericFaultRecord = (DbGdaQGenericFaultRecord)context.QueueRecord;
                        genericFaultFailRecords.Add(dbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper.CreateEntity(genericFaultRecord, reason));
                        genericFaultRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        genericFaultRecordsToDelete.Add(genericFaultRecord);
                        break;
                    case RecordType.GenericStatus:
                        var genericStatusRecord = (DbGdaQGenericStatusRecord)context.QueueRecord;
                        genericStatusFailRecords.Add(dbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper.CreateEntity(genericStatusRecord, reason));
                        genericStatusRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        genericStatusRecordsToDelete.Add(genericStatusRecord);
                        break;
                    case RecordType.J1708Fault:
                        var j1708FaultRecord = (DbGdaQJ1708FaultRecord)context.QueueRecord;
                        j1708FaultFailRecords.Add(dbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper.CreateEntity(j1708FaultRecord, reason));
                        j1708FaultRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        j1708FaultRecordsToDelete.Add(j1708FaultRecord);
                        break;
                    case RecordType.J1939Fault:
                        var j1939FaultRecord = (DbGdaQJ1939FaultRecord)context.QueueRecord;
                        j1939FaultFailRecords.Add(dbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper.CreateEntity(j1939FaultRecord, reason));
                        j1939FaultRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        j1939FaultRecordsToDelete.Add(j1939FaultRecord);
                        break;
                    case RecordType.ObdiiFault:
                        var obdiiFaultRecord = (DbGdaQObdiiFaultRecord)context.QueueRecord;
                        obdiiFaultFailRecords.Add(dbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper.CreateEntity(obdiiFaultRecord, reason));
                        obdiiFaultRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        obdiiFaultRecordsToDelete.Add(obdiiFaultRecord);
                        break;
                    case RecordType.Vin:
                        var vinRecord = (DbGdaQVinRecord)context.QueueRecord;
                        vinFailRecords.Add(dbGdaQVinRecordDbGdaQVinRecordFailEntityMapper.CreateEntity(vinRecord, reason));
                        vinRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                        vinRecordsToDelete.Add(vinRecord);
                        break;
                }
            }

            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    // Insert fail records
                    if (gpsFailRecords.Count > 0) await dbGdaQGpsRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, gpsFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (accelerationFailRecords.Count > 0) await dbGdaQAccelerationRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, accelerationFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (binaryFailRecords.Count > 0) await dbGdaQBinaryRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, binaryFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (bluetoothFailRecords.Count > 0) await dbGdaQBluetoothRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, bluetoothFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (driverChangeFailRecords.Count > 0) await dbGdaQDriverChangeRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, driverChangeFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (genericFaultFailRecords.Count > 0) await dbGdaQGenericFaultRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, genericFaultFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (genericStatusFailRecords.Count > 0) await dbGdaQGenericStatusRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, genericStatusFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (j1708FaultFailRecords.Count > 0) await dbGdaQJ1708FaultRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, j1708FaultFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (j1939FaultFailRecords.Count > 0) await dbGdaQJ1939FaultRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, j1939FaultFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (obdiiFaultFailRecords.Count > 0) await dbGdaQObdiiFaultRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, obdiiFaultFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (vinFailRecords.Count > 0) await dbGdaQVinRecordFailEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, vinFailRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);

                    // Delete from queue tables
                    if (gpsRecordsToDelete.Count > 0) await dbGdaQGpsRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, gpsRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (accelerationRecordsToDelete.Count > 0) await dbGdaQAccelerationRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, accelerationRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (binaryRecordsToDelete.Count > 0) await dbGdaQBinaryRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, binaryRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (bluetoothRecordsToDelete.Count > 0) await dbGdaQBluetoothRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, bluetoothRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (driverChangeRecordsToDelete.Count > 0) await dbGdaQDriverChangeRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, driverChangeRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (genericFaultRecordsToDelete.Count > 0) await dbGdaQGenericFaultRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, genericFaultRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (genericStatusRecordsToDelete.Count > 0) await dbGdaQGenericStatusRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, genericStatusRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (j1708FaultRecordsToDelete.Count > 0) await dbGdaQJ1708FaultRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, j1708FaultRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (j1939FaultRecordsToDelete.Count > 0) await dbGdaQJ1939FaultRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, j1939FaultRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (obdiiFaultRecordsToDelete.Count > 0) await dbGdaQObdiiFaultRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, obdiiFaultRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    if (vinRecordsToDelete.Count > 0) await dbGdaQVinRecordEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, vinRecordsToDelete, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);

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

        /// <summary>
        /// Handles waiting for prerequisite services and/or database maintenance if required prior to continuing.
        /// </summary>
        async Task PrepareForExecutionAsync(CancellationToken stoppingToken)
        {
            var prerequisiteServices = new List<DIGAdapterService>
            {
                DIGAdapterService.DeviceProvisioningService
            };
            await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
            await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
        }

        /// <summary>
        /// Starts the current <see cref="TelemetryDataService"/> instance.
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbGdaOServiceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbGdaOServiceTrackings, DIGAdapterService.TelemetryDataService, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, DIGAdapterService.TelemetryDataService, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            stateMachine.RegisterService(nameof(TelemetryDataService), adapterConfiguration.EnableTelemetryDataService);

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableTelemetryDataService)
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
        /// Stops the current <see cref="TelemetryDataService"/> instance.
        /// </summary>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            stateMachine.RegisterService(nameof(TelemetryDataService), false);
            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the <see cref="DbGdaOServiceTracking"/> record associated with the <see cref="TelemetryDataService"/> service.
        /// </summary>
        /// <param name="totalCount">The total number of records processed in the batch.</param>
        /// <param name="successCount">The number of successfully processed records.</param>
        /// <param name="failureCount">The number of failed records.</param>
        async Task UpdateServiceTrackingRecordAsync(int totalCount, int successCount, int failureCount)
        {
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                try
                {
                    await serviceTracker.UpdateDbOServiceTrackingRecordWithMetricsAsync(
                        adapterContext, DIGAdapterService.TelemetryDataService, DateTime.UtcNow,
                        totalCount, successCount, failureCount);
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

        /// <summary>
        /// Enum representing the different telemetry record types.
        /// </summary>
        enum RecordType
        {
            Gps,
            Acceleration,
            Binary,
            Bluetooth,
            DriverChange,
            GenericFault,
            GenericStatus,
            J1708Fault,
            J1939Fault,
            ObdiiFault,
            Vin
        }

        /// <summary>
        /// Context for tracking a queue record during processing.
        /// </summary>
        class QueueRecordProcessingContext
        {
            public RecordType RecordType { get; set; }
            public object QueueRecord { get; set; } = null!;
            public bool ImmediateFailure { get; set; }
            public string FailureReason { get; set; } = string.Empty;
        }

        /// <summary>
        /// Builds a concise string representation of record counts per type for logging.
        /// Only includes types with non-zero counts.
        /// </summary>
        static string BuildRecordTypeBreakdownString(Dictionary<RecordType, int> recordTypeCounts)
        {
            var nonZeroCounts = recordTypeCounts
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => $"{kvp.Key}:{kvp.Value}")
                .ToList();

            if (nonZeroCounts.Count == 0)
            {
                return string.Empty;
            }

            return $"[{string.Join(", ", nonZeroCounts)}]";
        }
    }
}
