using MyGeotabAPIAdapter;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI;
using MyGeotabAPIAdapter.DIGAPI.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.Services;
using NLog;
using Polly;
using Polly.Retry;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that retrieves invalid records from the DIG API /invalidrecords endpoint
    /// and persists them to the database for later analysis.
    /// </summary>
    class InvalidRecordRetrievalService : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<InvalidRecordRetrievalService> awaiter;
        readonly IExceptionHelper exceptionHelper;
        readonly IDIGAPIHelper digAPIHelper;
        readonly IServiceTracker<DbGdaOServiceTracking> serviceTracker;
        readonly IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        // Entity Persisters
        readonly IGenericEntityPersister<DbGdaDIGInvalidRecord> dbGdaDIGInvalidRecordEntityPersister;
        readonly IGenericEntityPersister<DbGdaDIGInvalidRecordsCursor> dbGdaDIGInvalidRecordsCursorEntityPersister;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidRecordRetrievalService"/> class.
        /// </summary>
        public InvalidRecordRetrievalService(
            IDIGAdapterConfiguration adapterConfiguration,
            IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment,
            IBackgroundServiceAwaiter<InvalidRecordRetrievalService> awaiter,
            IExceptionHelper exceptionHelper,
            IDIGAPIHelper digAPIHelper,
            IServiceTracker<DbGdaOServiceTracking> serviceTracker,
            IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext,
            IGenericEntityPersister<DbGdaDIGInvalidRecord> dbGdaDIGInvalidRecordEntityPersister,
            IGenericEntityPersister<DbGdaDIGInvalidRecordsCursor> dbGdaDIGInvalidRecordsCursorEntityPersister)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.digAPIHelper = digAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.adapterContext = adapterContext;
            this.dbGdaDIGInvalidRecordEntityPersister = dbGdaDIGInvalidRecordEntityPersister;
            this.dbGdaDIGInvalidRecordsCursorEntityPersister = dbGdaDIGInvalidRecordsCursorEntityPersister;

            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.InvalidRecordRetrievalServiceIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                await PrepareForExecutionAsync(stoppingToken);

                try
                {
                    await ExecuteIterationBusinessLogicAsync(stoppingToken);

                    // Always wait for the configured interval between iterations
                    await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Wait, stoppingToken);
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
        /// Executes the business logic for a single iteration.
        /// </summary>
        async Task ExecuteIterationBusinessLogicAsync(CancellationToken stoppingToken)
        {
            MethodBase? methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Started iteration of {methodBase?.ReflectedType?.Name}.{methodBase?.Name}");

            var iterationStopwatch = Stopwatch.StartNew();

            // Load the cursor from the database
            var cursor = await LoadCursorAsync(stoppingToken);
            int? nextResultKey = cursor.NextResultKey == 0 ? null : cursor.NextResultKey;
            int totalRetrieved = 0;
            int totalPersisted = 0;
            int totalSkippedDuplicates = 0;
            long totalApiLatencyMs = 0;
            int apiCallCount = 0;

            logger.Debug($"Starting invalid record retrieval. Cursor: {nextResultKey?.ToString() ?? "start"}, BatchSize: {adapterConfiguration.InvalidRecordRetrievalServiceBatchSize}");

            // Continue retrieving records until no more are available
            bool hasMoreRecords = true;
            while (hasMoreRecords && !stoppingToken.IsCancellationRequested)
            {
                var result = await digAPIHelper.GetInvalidRecordsAsync(
                    nextResultKey,
                    adapterConfiguration.InvalidRecordRetrievalServiceBatchSize,
                    adapterConfiguration.TimeoutSecondsForDIGTasks);

                // Track API latency
                totalApiLatencyMs += result.ElapsedMilliseconds;
                apiCallCount++;

                if (!result.IsSuccess)
                {
                    logger.Warn($"Failed to retrieve invalid records from DIG API: {result.ErrorMessage} (Latency: {result.ElapsedMilliseconds}ms)");
                    break;
                }

                logger.Debug($"Retrieved {result.CurrentNumberOfInvalidRecords} invalid record(s). " +
                    $"Total available: {result.TotalNumberOfInvalidRecords}, Latency: {result.ElapsedMilliseconds}ms, NextCursor: {result.NextResultKey?.ToString() ?? "null"}");

                totalRetrieved += result.CurrentNumberOfInvalidRecords;

                if (result.CurrentNumberOfInvalidRecords > 0)
                {
                    // Map and persist records
                    var (persisted, skipped) = await PersistInvalidRecordsAsync(result.InvalidRecords, stoppingToken);
                    totalPersisted += persisted;
                    totalSkippedDuplicates += skipped;
                }

                // Update cursor. Per DIG spec, always persist the NextResultKey from the
                // response — never reset to 0. The cursor must be preserved between polling
                // cycles so that subsequent polls only retrieve newly added invalid records.
                if (result.NextResultKey.HasValue)
                {
                    nextResultKey = result.NextResultKey;
                    cursor.NextResultKey = result.NextResultKey.Value;
                }
                cursor.LastUpdatedUtc = DateTime.UtcNow;
                await UpdateCursorAsync(cursor, stoppingToken);

                // Stop the inner loop when no more records are available
                if (result.CurrentNumberOfInvalidRecords == 0)
                {
                    hasMoreRecords = false;
                }
            }

            // Update service tracking with batch metrics
            await UpdateServiceTrackingRecordAsync(totalRetrieved, totalPersisted, totalSkippedDuplicates);

            iterationStopwatch.Stop();
            var avgLatencyMs = apiCallCount > 0 ? totalApiLatencyMs / apiCallCount : 0;

            if (totalRetrieved > 0)
            {
                logger.Info($"Invalid record retrieval complete. Retrieved: {totalRetrieved}, Persisted: {totalPersisted}, Duplicates skipped: {totalSkippedDuplicates}. " +
                    $"API calls: {apiCallCount}, Avg latency: {avgLatencyMs}ms, Iteration: {iterationStopwatch.ElapsedMilliseconds}ms");
            }
            else
            {
                logger.Debug($"No invalid records available from DIG API. API calls: {apiCallCount}, Iteration: {iterationStopwatch.ElapsedMilliseconds}ms");
            }

            logger.Trace($"Completed iteration of {methodBase?.ReflectedType?.Name}.{methodBase?.Name}");
        }

        /// <summary>
        /// Loads the cursor from the database.
        /// </summary>
        async Task<DbGdaDIGInvalidRecordsCursor> LoadCursorAsync(CancellationToken cancellationToken)
        {
            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<DbGdaDIGInvalidRecordsCursor>(adapterContext);
                var cursors = await repo.GetAllAsync(cancellationTokenSource);
                return cursors.FirstOrDefault() ?? new DbGdaDIGInvalidRecordsCursor
                {
                    id = 1,
                    NextResultKey = 0,
                    LastUpdatedUtc = DateTime.UtcNow
                };
            }, new Context());
        }

        /// <summary>
        /// Updates the cursor in the database.
        /// </summary>
        async Task UpdateCursorAsync(DbGdaDIGInvalidRecordsCursor cursor, CancellationToken cancellationToken)
        {
            cursor.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    await dbGdaDIGInvalidRecordsCursorEntityPersister.PersistEntitiesToDatabaseAsync(
                        adapterContext,
                        new List<DbGdaDIGInvalidRecordsCursor> { cursor },
                        cancellationTokenSource,
                        MyGeotabAPIAdapter.Logging.LogLevel.Debug);
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
        /// Persists invalid records to the database, skipping duplicates based on GeotabGUID.
        /// </summary>
        /// <returns>A tuple containing (persisted count, skipped duplicates count).</returns>
        async Task<(int persisted, int skipped)> PersistInvalidRecordsAsync(List<DIGInvalidRecord> invalidRecords, CancellationToken cancellationToken)
        {
            var recordsToPersist = new List<DbGdaDIGInvalidRecord>();
            var retrievedAtUtc = DateTime.UtcNow;

            foreach (var invalidRecord in invalidRecords)
            {
                var dbRecord = MapToDbEntity(invalidRecord, retrievedAtUtc);
                if (dbRecord != null)
                {
                    recordsToPersist.Add(dbRecord);
                }
            }

            if (recordsToPersist.Count == 0)
            {
                return (0, invalidRecords.Count);
            }

            // Check for existing records by GeotabGUID to avoid duplicates
            var guids = recordsToPersist.Select(r => r.GeotabGUID).ToList();
            var existingGuids = await GetExistingGuidsAsync(guids, cancellationToken);

            var newRecords = recordsToPersist.Where(r => !existingGuids.Contains(r.GeotabGUID)).ToList();
            int skipped = recordsToPersist.Count - newRecords.Count;

            if (newRecords.Count > 0)
            {
                foreach (var record in newRecords)
                {
                    record.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;
                }

                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                {
                    using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                    using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    try
                    {
                        await dbGdaDIGInvalidRecordEntityPersister.PersistEntitiesToDatabaseAsync(
                            adapterContext,
                            newRecords,
                            cancellationTokenSource,
                            MyGeotabAPIAdapter.Logging.LogLevel.Debug);
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

            return (newRecords.Count, skipped);
        }

        /// <summary>
        /// Gets existing GeotabGUIDs from the database.
        /// </summary>
        async Task<HashSet<string>> GetExistingGuidsAsync(List<string> guids, CancellationToken cancellationToken)
        {
            if (guids.Count == 0)
            {
                return new HashSet<string>();
            }

            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<DbGdaDIGInvalidRecord>(adapterContext);

                // Query for existing records with matching GUIDs
                var guidList = string.Join(",", guids.Select(g => $"'{g.Replace("'", "''")}'"));
                var sql = adapterContext.ProviderType switch
                {
                    ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                        $"SELECT \"GeotabGUID\" FROM gda.\"DIGInvalidRecords\" WHERE \"GeotabGUID\" IN ({guidList})",
                    ConnectionInfo.DataAccessProviderType.SQLServer =>
                        $"SELECT GeotabGUID FROM gda.DIGInvalidRecords WHERE GeotabGUID IN ({guidList})",
                    _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
                };

                var existingRecords = await repo.QueryAsync(sql, null, cancellationTokenSource, false, adapterContext);
                return existingRecords.Select(r => r.GeotabGUID).ToHashSet();
            }, new Context());
        }

        /// <summary>
        /// Maps a DIGInvalidRecord to a DbGdaDIGInvalidRecord entity.
        /// </summary>
        DbGdaDIGInvalidRecord? MapToDbEntity(DIGInvalidRecord invalidRecord, DateTime retrievedAtUtc)
        {
            try
            {
                var baseRecordJson = invalidRecord.BaseRecord.GetRawText();

                // Extract common fields from the BaseRecord
                string geotabGuid = string.Empty;
                string recordType = string.Empty;
                string serialNo = string.Empty;
                DateTime recordDateTime = DateTime.MinValue;

                if (invalidRecord.BaseRecord.ValueKind == JsonValueKind.Object)
                {
                    // Try to extract Type
                    if (invalidRecord.BaseRecord.TryGetProperty("Type", out var typeElement))
                    {
                        recordType = typeElement.GetString() ?? string.Empty;
                    }

                    // Try to extract SerialNo
                    if (invalidRecord.BaseRecord.TryGetProperty("SerialNo", out var serialNoElement))
                    {
                        serialNo = serialNoElement.GetString() ?? string.Empty;
                    }

                    // Try to extract DateTime
                    if (invalidRecord.BaseRecord.TryGetProperty("DateTime", out var dateTimeElement))
                    {
                        if (dateTimeElement.TryGetDateTime(out var dt))
                        {
                            recordDateTime = dt;
                        }
                    }

                    // Try to extract GeotabGUID if present
                    if (invalidRecord.BaseRecord.TryGetProperty("GeotabGUID", out var guidElement))
                    {
                        geotabGuid = guidElement.GetString() ?? string.Empty;
                    }
                }

                // Generate a GUID if not present in the record
                if (string.IsNullOrEmpty(geotabGuid))
                {
                    // Create a deterministic GUID based on record content to prevent duplicates
                    geotabGuid = GenerateDeterministicGuid(serialNo, recordType, recordDateTime, invalidRecord.TimeStamp);
                }

                return new DbGdaDIGInvalidRecord
                {
                    GeotabGUID = geotabGuid,
                    RecordType = recordType,
                    SerialNo = serialNo,
                    RecordDateTime = recordDateTime,
                    BaseRecordJson = baseRecordJson,
                    Cause = invalidRecord.Cause,
                    TimeStamp = invalidRecord.TimeStamp,
                    UserId = invalidRecord.UserId,
                    RetrievedAtUtc = retrievedAtUtc,
                    RecordCreationTimeUtc = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to map invalid record to database entity: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generates a deterministic GUID based on record properties to prevent duplicate inserts.
        /// </summary>
        static string GenerateDeterministicGuid(string serialNo, string recordType, DateTime recordDateTime, DateTime timeStamp)
        {
            var input = $"{serialNo}|{recordType}|{recordDateTime:O}|{timeStamp:O}";
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return new Guid(hash).ToString();
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
        /// Starts the current <see cref="InvalidRecordRetrievalService"/> instance.
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbGdaOServiceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbGdaOServiceTrackings, DIGAdapterService.InvalidRecordRetrievalService, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, DIGAdapterService.InvalidRecordRetrievalService, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            stateMachine.RegisterService(nameof(InvalidRecordRetrievalService), adapterConfiguration.EnableInvalidRecordRetrievalService);

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableInvalidRecordRetrievalService)
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
        /// Stops the current <see cref="InvalidRecordRetrievalService"/> instance.
        /// </summary>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            stateMachine.RegisterService(nameof(InvalidRecordRetrievalService), false);
            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the <see cref="DbGdaOServiceTracking"/> record associated with the <see cref="InvalidRecordRetrievalService"/> service.
        /// </summary>
        /// <param name="totalRetrieved">The total number of invalid records retrieved from the DIG API.</param>
        /// <param name="totalPersisted">The number of records successfully persisted to the database.</param>
        /// <param name="totalSkippedDuplicates">The number of duplicate records skipped.</param>
        async Task UpdateServiceTrackingRecordAsync(int totalRetrieved, int totalPersisted, int totalSkippedDuplicates)
        {
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                try
                {
                    await serviceTracker.UpdateDbOServiceTrackingRecordWithMetricsAsync(
                        adapterContext, DIGAdapterService.InvalidRecordRetrievalService, DateTime.UtcNow,
                        totalRetrieved, totalPersisted, totalSkippedDuplicates);
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
