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
using NLog;
using Polly;
using Polly.Retry;
using System.Reflection;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that applies device subscription instructions from the
    /// <see cref="nameof(DbGdaQDeviceSubscription)"/> queue to the DeviceSubscriptions table.
    ///
    /// The service never writes DeviceSubscriptions directly. It stages the instructions it claims and calls
    /// spMerge_stg_DeviceSubscriptions, which owns deduplication, validation, ordering, the termination guard and the
    /// audit trail. Any other intake stages into the same table and calls the same procedure, so no two intakes can
    /// disagree about how identical data is applied.
    ///
    /// Merging and clearing the queue are one transaction owned by this service, so a queue record is deleted only
    /// once the merge that consumed it has committed. A crash before that leaves the records unconsumed and the
    /// subscriptions untouched, and the work is simply redone. Committing the merge separately from the queue delete
    /// would instead let a redone batch replay instructions against subscriptions that had already moved on. Staging
    /// commits ahead of both, which it must; see ApplySubscriptionInstructionsAsync for why, and for the sweep that
    /// reclaims staging rows orphaned by a crash in that window.
    /// </summary>
    class DeviceSubscriptionService : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames;
        readonly IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DeviceSubscriptionService> awaiter;
        readonly IDeviceLookup deviceLookup;
        readonly IExceptionHelper exceptionHelper;
        readonly IServiceTracker<DbGdaOServiceTracking> serviceTracker;
        readonly IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        // Entity Persisters
        readonly IGenericEntityPersister<DbGdaQDeviceSubscription> dbGdaQDeviceSubscriptionEntityPersister;
        readonly IGenericEntityPersister<DbGdaQDeviceSubscriptionFail> dbGdaQDeviceSubscriptionFailEntityPersister;
        readonly IGenericEntityPersister<DbGdaStgDeviceSubscription> dbGdaStgDeviceSubscriptionEntityPersister;

        // Mappers
        readonly IDbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper dbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper;
        readonly IDbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper dbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The number of minutes after which a queue record left "in progress" by a crashed instance is eligible to be claimed again. Matches the equivalent value in the device provisioning workflow.
        /// </summary>
        const int ClaimStaleThresholdMinutes = 30;

        const int ExecutionThrottleEngagingRecordCount = 1;

        /// <summary>
        /// The maximum length of the termination guard's recorded reason, matching the width of the column it is written to.
        /// </summary>
        const int MaxTrippedReasonLength = 500;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceSubscriptionService"/> class.
        /// </summary>
        public DeviceSubscriptionService(
            IDIGAdapterConfiguration adapterConfiguration,
            IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames,
            IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment,
            IBackgroundServiceAwaiter<DeviceSubscriptionService> awaiter,
            IDeviceLookup deviceLookup,
            IExceptionHelper exceptionHelper,
            IServiceTracker<DbGdaOServiceTracking> serviceTracker,
            IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext,
            IGenericEntityPersister<DbGdaQDeviceSubscription> dbGdaQDeviceSubscriptionEntityPersister,
            IGenericEntityPersister<DbGdaQDeviceSubscriptionFail> dbGdaQDeviceSubscriptionFailEntityPersister,
            IGenericEntityPersister<DbGdaStgDeviceSubscription> dbGdaStgDeviceSubscriptionEntityPersister,
            IDbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper dbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper,
            IDbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper dbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.digAdapterDatabaseObjectNames = digAdapterDatabaseObjectNames;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.deviceLookup = deviceLookup;
            this.exceptionHelper = exceptionHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.adapterContext = adapterContext;
            this.dbGdaQDeviceSubscriptionEntityPersister = dbGdaQDeviceSubscriptionEntityPersister;
            this.dbGdaQDeviceSubscriptionFailEntityPersister = dbGdaQDeviceSubscriptionFailEntityPersister;
            this.dbGdaStgDeviceSubscriptionEntityPersister = dbGdaStgDeviceSubscriptionEntityPersister;
            this.dbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper = dbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper;
            this.dbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper = dbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper;

            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Builds the SQL statement for executing the spClaimQDeviceSubscriptionsBatch stored procedure/function based on the database provider type.
        /// </summary>
        /// <returns>The SQL statement formatted for the current database provider.</returns>
        string BuildSqlToExecuteSpClaimQDeviceSubscriptionsBatch()
        {
            var storedProcedureName = digAdapterDatabaseObjectNames.SpClaimQDeviceSubscriptionsBatchName;

            return adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT * FROM {SqlMapperExtensions.FormatTableNameForSql(storedProcedureName, "npgsqlconnection")}(@BatchSize::integer, @StaleThresholdMinutes::integer, @MaxClaimAttempts::integer);",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $"EXEC {SqlMapperExtensions.FormatTableNameForSql(storedProcedureName, "sqlconnection")} @BatchSize = @BatchSize, @StaleThresholdMinutes = @StaleThresholdMinutes, @MaxClaimAttempts = @MaxClaimAttempts;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };
        }

        /// <summary>
        /// Builds the SQL statement for executing the spMerge_stg_DeviceSubscriptions stored procedure/function based on the database provider type.
        /// </summary>
        /// <returns>The SQL statement formatted for the current database provider.</returns>
        string BuildSqlToExecuteSpMergeStgDeviceSubscriptions()
        {
            var storedProcedureName = digAdapterDatabaseObjectNames.SpMergeStgDeviceSubscriptionsName;

            return adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT * FROM {SqlMapperExtensions.FormatTableNameForSql(storedProcedureName, "npgsqlconnection")}(@BatchId::uuid, @Mode::smallint, @SnapshotOrdinal::bigint, @ExpectedIdentifierTypes::text, @MaxTerminationsPerWindow::integer, @TerminationWindowMinutes::integer);",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $"EXEC {SqlMapperExtensions.FormatTableNameForSql(storedProcedureName, "sqlconnection")} @BatchId = @BatchId, @Mode = @Mode, @SnapshotOrdinal = @SnapshotOrdinal, @ExpectedIdentifierTypes = @ExpectedIdentifierTypes, @MaxTerminationsPerWindow = @MaxTerminationsPerWindow, @TerminationWindowMinutes = @TerminationWindowMinutes;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };
        }

        /// <summary>
        /// Builds the SQL statement that deletes staging rows left behind by a crash between staging and merging, returning the batches they belonged to. The age threshold is evaluated by the database, because the staged timestamps are written by the database.
        /// </summary>
        /// <returns>The SQL statement formatted for the current database provider.</returns>
        string BuildSqlToSweepStaleStagingBatches()
        {
            return adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    @"DELETE FROM gda.""stg_DeviceSubscriptions""
                      WHERE ""StagedDateTimeUtc"" < ((now() AT TIME ZONE 'UTC') - (@StaleStagingBatchMinutes * interval '1 minute'))
                      RETURNING ""BatchId"";",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    @"DELETE FROM gda.stg_DeviceSubscriptions
                      OUTPUT deleted.BatchId
                      WHERE StagedDateTimeUtc < DATEADD(MINUTE, -@StaleStagingBatchMinutes, SYSUTCDATETIME());",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };
        }

        /// <summary>
        /// Builds the SQL statement that takes the merge procedure's lock - the transaction-owned application lock on SQL Server, the transaction-scoped advisory lock on PostgreSQL - ahead of the procedure. It is the first statement of the merge's transaction. The procedure takes the same lock again on entry, which a transaction already holding it is granted at once.
        /// <para>
        /// Taken before this batch's staged rows are locked, so that an instance waiting for another instance's merge holds nothing that merge needs. With the rows locked first, SQL Server could form a cycle: the merging instance's delete of its own staged rows, if its plan scans the table, waits on the waiting instance's rows while that instance waits for the lock. The deadlock monitor then fails the waiter's lock request, and that error ends the process. The shipped plan seeks the <c>BatchId</c> index and does not scan, but a plan is chosen at run time; this order does not depend on it. PostgreSQL's delete does not lock rows it does not delete, but both providers keep the same order. On SQL Server the wait is bounded and fails exactly as the procedure's own does.
        /// </para>
        /// </summary>
        /// <returns>The SQL statement formatted for the current database provider.</returns>
        string BuildSqlToAcquireMergeLock()
        {
            return adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    @"SELECT pg_advisory_xact_lock(hashtext('gda.spMerge_stg_DeviceSubscriptions'));",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    @"DECLARE @LockResult INT;
                      EXEC @LockResult = sp_getapplock @Resource = 'gda.spMerge_stg_DeviceSubscriptions', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000;
                      IF @LockResult < 0 RAISERROR('Could not acquire the device-subscription merge lock.', 16, 1);",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };
        }

        /// <summary>
        /// Builds the SQL statement that locks the staged rows belonging to a single batch until the end of the transaction, returning one row per staged row. It follows the merge lock (see <see cref="BuildSqlToAcquireMergeLock"/>) in the merge's transaction: holding these rows is what stops another instance's stale-staging sweep from deleting them while this batch is merged, and the number returned is how a batch whose rows are already gone is recognised.
        /// <para>
        /// On SQL Server the select is deliberately not covered by the <c>BatchId</c> index, and seeks it, so that the update locks land on the clustered rows as well as on their index keys and on nothing else. Locking the index keys alone would let a sweep take the clustered row first and then wait on the key, while the merge waited on that clustered row - a deadlock - and a scan would lock other instances' batches too.
        /// </para>
        /// </summary>
        /// <returns>The SQL statement formatted for the current database provider.</returns>
        string BuildSqlToLockStagedBatch()
        {
            return adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    @"SELECT ""BatchId"" FROM gda.""stg_DeviceSubscriptions"" WHERE ""BatchId"" = @BatchId FOR UPDATE;",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    @"SELECT * FROM gda.stg_DeviceSubscriptions WITH (UPDLOCK, ROWLOCK, FORCESEEK) WHERE BatchId = @BatchId;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };
        }

        /// <summary>
        /// Builds the SQL statement that deletes the staged rows belonging to a single batch. Needed because staging and merging are separate transactions: when the merge refuses a batch, or finds it incomplete, nothing rolls the staged rows back, so they are removed explicitly in the merge's transaction rather than left for the stale-batch sweep.
        /// </summary>
        /// <returns>The SQL statement formatted for the current database provider.</returns>
        string BuildSqlToDeleteStagedBatch()
        {
            return adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    @"DELETE FROM gda.""stg_DeviceSubscriptions"" WHERE ""BatchId"" = @BatchId;",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    @"DELETE FROM gda.stg_DeviceSubscriptions WHERE BatchId = @BatchId;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };
        }

        /// <summary>
        /// Builds the SQL statement that records a tripped termination guard.
        /// <para>
        /// The statement returns the id of the row it updated, so the caller can tell a persisted trip from one
        /// that matched no row. A repository Execute cannot: it reports success for any statement that did not
        /// throw, discarding the affected-row count.
        /// </para>
        /// <para>
        /// On PostgreSQL the times are <c>statement_timestamp()</c>, not <c>now()</c>. The trip is recorded inside the
        /// merge's transaction, and <c>now()</c> is when that transaction began - before any wait for the merge lock -
        /// not when the guard tripped; README section 3.1 uses <c>TrippedDateTimeUtc</c> as a cut-off. SQL Server's
        /// <c>SYSUTCDATETIME()</c> is already the statement's time.
        /// </para>
        /// </summary>
        /// <returns>The SQL statement formatted for the current database provider.</returns>
        string BuildSqlToPersistGuardTrip()
        {
            return adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    @"UPDATE gda.""DeviceSubscriptionGuardState""
                      SET ""IsTripped"" = true,
                          ""TrippedDateTimeUtc"" = (statement_timestamp() AT TIME ZONE 'UTC'),
                          ""TrippedReason"" = @TrippedReason,
                          ""TrippedTerminationCount"" = @TrippedTerminationCount,
                          ""TrippedWindowCount"" = @TrippedWindowCount,
                          ""RecordLastChangedUtc"" = (statement_timestamp() AT TIME ZONE 'UTC')
                      WHERE ""id"" = 1
                      RETURNING ""id"";",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    @"UPDATE gda.DeviceSubscriptionGuardState
                      SET IsTripped = 1,
                          TrippedDateTimeUtc = SYSUTCDATETIME(),
                          TrippedReason = @TrippedReason,
                          TrippedTerminationCount = @TrippedTerminationCount,
                          TrippedWindowCount = @TrippedWindowCount,
                          RecordLastChangedUtc = SYSUTCDATETIME()
                      OUTPUT inserted.id
                      WHERE id = 1;",
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
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.DeviceSubscriptionServiceIntervalSeconds);

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

            await SweepStaleStagingBatchesAsync(stoppingToken);

            var dbGdaQDeviceSubscriptions = (await GetBatchOfSubscriptionInstructionsAsync(stoppingToken)).ToList();

            if (dbGdaQDeviceSubscriptions.Count == 0)
            {
                logger.Debug($"No records retrieved from {digAdapterDatabaseObjectNames.DbGdaQDeviceSubscriptionTableName} table.");

                // No records were returned, but the OServiceTracking record for this service still needs to be
                // updated to show that the service is operating. The overload WITHOUT metrics is used deliberately:
                // the metrics overload assigns the batch size rather than accumulating it, so calling it here would
                // zero the last real batch's size one iteration later and destroy the only per-batch signal there is.
                await UpdateServiceTrackingRecordAsync();

                return new BatchProcessingResult
                {
                    TotalCount = 0,
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            logger.Info($"Retrieved {dbGdaQDeviceSubscriptions.Count} records from {digAdapterDatabaseObjectNames.DbGdaQDeviceSubscriptionTableName} table. Processing...");

            var result = await ApplySubscriptionInstructionsAsync(dbGdaQDeviceSubscriptions, stoppingToken);

            await UpdateServiceTrackingRecordAsync(result.TotalCount, result.SuccessCount, result.FailureCount);

            if (result.FailureCount > 0)
            {
                logger.Warn($"Of the {result.TotalCount} records from the {digAdapterDatabaseObjectNames.DbGdaQDeviceSubscriptionTableName} table, {result.SuccessCount} were successfully processed and {result.FailureCount} failed. Copies of any failed records have been inserted into the {digAdapterDatabaseObjectNames.DbGdaQDeviceSubscriptionFailTableName} table for reference.");
            }
            else
            {
                logger.Info($"Of the {result.TotalCount} records from the {digAdapterDatabaseObjectNames.DbGdaQDeviceSubscriptionTableName} table, {result.SuccessCount} were successfully processed and {result.FailureCount} failed.");
            }

            logger.Trace($"Completed iteration of {methodBase?.ReflectedType?.Name}.{methodBase?.Name}");

            return result;
        }

        /// <summary>
        /// Atomically claims and retrieves a batch of <see cref="DbGdaQDeviceSubscription"/> records from the adapter database, in the order the instructions must be applied in.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<IEnumerable<DbGdaQDeviceSubscription>> GetBatchOfSubscriptionInstructionsAsync(CancellationToken cancellationToken)
        {
            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<DbGdaQDeviceSubscription>(adapterContext);

                var sql = BuildSqlToExecuteSpClaimQDeviceSubscriptionsBatch();
                var parameters = new
                {
                    BatchSize = adapterConfiguration.DeviceSubscriptionServiceBatchSize,
                    StaleThresholdMinutes = ClaimStaleThresholdMinutes,
                    // The claim procedure moves records that have exhausted their attempts to the fail table before
                    // claiming anything - up to BatchSize of them per call - so an exhausted record normally never
                    // reaches this service. With more than BatchSize exhausted records waiting, some are claimed once
                    // more before a later call moves them.
                    MaxClaimAttempts = adapterConfiguration.DeviceSubscriptionServiceMaxClaimAttempts
                };

                return await repo.QueryAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
            }, new Context());
        }

        /// <summary>
        /// Deletes staging rows old enough to have been abandoned by a crash between staging and merging. The merge
        /// clears only its own batch's rows rather than truncating the table, because a second writer may hold rows
        /// under another batch - which removes the pattern's only self-healing step, so this restores it.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task SweepStaleStagingBatchesAsync(CancellationToken cancellationToken)
        {
            var sweptBatches = await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<StagingBatchIdentifier>(adapterContext);

                var sql = BuildSqlToSweepStaleStagingBatches();
                var parameters = new { adapterConfiguration.StaleStagingBatchMinutes };

                return await repo.QueryAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
            }, new Context());

            var sweptBatchList = sweptBatches.ToList();
            if (sweptBatchList.Count == 0)
            {
                return;
            }

            var distinctBatchIds = sweptBatchList.Select(sweptBatch => sweptBatch.BatchId).Distinct().ToList();
            logger.Warn($"Swept {sweptBatchList.Count} orphaned row(s) from {digAdapterDatabaseObjectNames.DbGdaStgDeviceSubscriptionTableName} across {distinctBatchIds.Count} batch(es) older than {adapterConfiguration.StaleStagingBatchMinutes} minute(s): {string.Join(", ", distinctBatchIds)}. Orphaned rows indicate that a run stopped between staging and merging - or, with more than one instance running this service, that the sweep reached a batch another instance was still waiting to merge. The instructions themselves were not lost: their queue records are deleted only once a merge has committed, and a batch whose staged rows have gone is not merged at all - its queue records stay claimed and are claimed again once that claim is {ClaimStaleThresholdMinutes} minutes old.");
        }

        /// <summary>
        /// Stages the claimed instructions, merges them, records any the merge rejected, and clears the claimed
        /// records from the queue.
        /// <para>
        /// Staging is committed in its own transaction before the merge runs, and the merge and the queue clear
        /// share a second one. The two cannot be combined: the bulk persister writes on the unit of work's
        /// provider-specific connection - <c>GetNpgsqlConnection</c> / <c>GetSqlConnection</c> open a second
        /// connection carrying its own transaction - while the merge is issued by a repository on the unit of
        /// work's main connection. Staged rows are therefore invisible to the merge until they are committed,
        /// which on PostgreSQL silently merges nothing and on SQL Server blocks until the command times out.
        /// This is the same split every merge processor in the solution uses; see <c>DeviceProcessor2</c>.
        /// </para>
        /// <para>
        /// A queue record is still only deleted once the merge that consumed it has committed, which is the
        /// property that matters. The split trades atomicity between staging and merging for that, and a crash
        /// in between leaves orphaned staging rows, which the stale-batch sweep already exists to reclaim. The
        /// second transaction begins by locking and counting the batch's staged rows, so a batch whose rows another
        /// instance's sweep removed in between is not merged, and its queue records are not deleted.
        /// </para>
        /// </summary>
        /// <param name="dbGdaQDeviceSubscriptions">The claimed instructions, in application order.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<BatchProcessingResult> ApplySubscriptionInstructionsAsync(IList<DbGdaQDeviceSubscription> dbGdaQDeviceSubscriptions, CancellationToken cancellationToken)
        {
            var batchId = Guid.NewGuid();
            var totalCount = dbGdaQDeviceSubscriptions.Count;

            // The only validation this service performs. Everything that can be judged from the instruction itself is
            // judged by the merge, so that this intake and any other reach identical conclusions about identical data.
            // An operation type the queue cannot express is not an instruction at all, so it never reaches staging.
            var malformedInstructions = new List<(DbGdaQDeviceSubscription QueueRecord, string FailureReason)>();
            var stageableInstructions = new List<DbGdaQDeviceSubscription>();

            foreach (var dbGdaQDeviceSubscription in dbGdaQDeviceSubscriptions)
            {
                if (dbGdaQDeviceSubscription.OperationType != DeviceSubscriptionOperationType.Subscribe
                    && dbGdaQDeviceSubscription.OperationType != DeviceSubscriptionOperationType.Terminate)
                {
                    malformedInstructions.Add((dbGdaQDeviceSubscription,
                        $"OperationType {dbGdaQDeviceSubscription.OperationType} is not a valid instruction. Use {DeviceSubscriptionOperationType.Subscribe} to subscribe a device or {DeviceSubscriptionOperationType.Terminate} to terminate a subscription."));
                    continue;
                }

                stageableInstructions.Add(dbGdaQDeviceSubscription);
            }

            // Every instruction is staged under its own operation type and the merge collapses the batch. Collapsing
            // here as well would be worse, not merely redundant: an instruction that names only a serial number is
            // resolved to its identifier by the merge, against a table this service does not read, so a collapse
            // performed here could not group such an instruction with the others that name the same device.
            var stagingRecords = stageableInstructions
                .Select(instruction => dbGdaQDeviceSubscriptionDbGdaStgDeviceSubscriptionEntityMapper.CreateEntity(instruction, batchId, instruction.OperationType))
                .ToList();

            var mergeResults = new List<DeviceSubscriptionMergeResult>();
            var guardTrip = (DeviceSubscriptionMergeResult?)null;
            var guardTripPersisted = false;
            var stagedRowsFound = (int?)null;

            // Transaction 1 of 2 - stage, and commit. The merge below runs on a different connection and cannot
            // see these rows until they are committed; see the remarks on this method.
            if (stagingRecords.Count != 0)
            {
                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                {
                    using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                    using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    try
                    {
                        await dbGdaStgDeviceSubscriptionEntityPersister.PersistEntitiesToDatabaseAsync(
                            adapterContext, stagingRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);

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

            // Transaction 2 of 2 - merge, record rejections, and clear the claimed records from the queue.
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                mergeResults.Clear();
                guardTrip = null;
                guardTripPersisted = false;
                stagedRowsFound = null;

                try
                {
                    if (stagingRecords.Count != 0)
                    {
                        // Lock this batch's staged rows before merging, and count them. The merge treats a batch with
                        // no staged rows as nothing to do, and this transaction then deletes the claimed queue records,
                        // so a batch another instance's stale-staging sweep had removed in the meantime was lost
                        // silently. Held until this transaction ends, the locks stop that sweep; the count catches a
                        // batch it reached first. Such a batch is not merged: what is left of it is cleared and its
                        // queue records stay claimed, to be claimed again once the claim is stale.
                        //
                        // The merge lock is taken before the rows, so that while this instance waits for another
                        // instance's merge it holds nothing that merge needs - see BuildSqlToAcquireMergeLock.
                        var stagedRowRepo = new BaseRepository<StagingBatchIdentifier>(adapterContext);
                        await stagedRowRepo.ExecuteAsync(BuildSqlToAcquireMergeLock(), null, cancellationTokenSource, false, adapterContext);
                        var lockedStagedRows = await stagedRowRepo.QueryAsync(BuildSqlToLockStagedBatch(), new { BatchId = batchId }, cancellationTokenSource, false, adapterContext);
                        var lockedStagedRowCount = lockedStagedRows.Count();
                        if (lockedStagedRowCount != stagingRecords.Count)
                        {
                            stagedRowsFound = lockedStagedRowCount;
                            await stagedRowRepo.ExecuteAsync(BuildSqlToDeleteStagedBatch(), new object[] { new { BatchId = batchId } }, cancellationTokenSource, false, adapterContext);
                            await adapterUOW.CommitAsync();
                            return;
                        }

                        var repo = new BaseRepository<DeviceSubscriptionMergeResult>(adapterContext);
                        var sql = BuildSqlToExecuteSpMergeStgDeviceSubscriptions();
                        var parameters = new
                        {
                            BatchId = batchId,
                            Mode = (short)DeviceSubscriptionMergeMode.Delta,
                            SnapshotOrdinal = (long?)null,
                            adapterConfiguration.ExpectedIdentifierTypes,
                            // A configured maximum of zero means the operator has switched the guard off, which the
                            // procedure expresses as no maximum at all.
                            MaxTerminationsPerWindow = adapterConfiguration.MaxTerminationsPerWindow > 0
                                ? adapterConfiguration.MaxTerminationsPerWindow
                                : (int?)null,
                            adapterConfiguration.TerminationWindowMinutes
                        };

                        mergeResults.AddRange(await repo.QueryAsync(sql, parameters, cancellationTokenSource, false, adapterContext));
                    }

                    guardTrip = mergeResults.FirstOrDefault(mergeResult =>
                        mergeResult.Code == DeviceSubscriptionMergeResultCode.BrakeWouldTrip
                        || mergeResult.Code == DeviceSubscriptionMergeResultCode.GuardAlreadyTripped
                        || mergeResult.Code == DeviceSubscriptionMergeResultCode.EmptySnapshotRefused);

                    if (guardTrip != null)
                    {
                        // The merge applied nothing, and the claimed records must stay in the queue: they are left
                        // claimed rather than deleted, so they are picked up again once an operator has cleared the
                        // guard. What this transaction does instead is record the trip and clear the batch's staged
                        // rows, and it COMMITS rather than rolling back, so the refusal is recorded all at once and
                        // while the merge lock is still held. Recorded afterwards, in transactions of its own, a
                        // crash in between lost the trip - the guard then read as untripped and the refused batch was
                        // applied unreviewed when it was next claimed - and another instance's merge could run in
                        // the gap. Committing is safe because the merge evaluates the guard before it changes
                        // anything, so nothing else is in this transaction. The staged rows were committed by the
                        // staging transaction, so they are deleted here rather than left for the stale-batch sweep,
                        // which would log them as evidence of a crash that never happened.
                        if (guardTrip.Code == DeviceSubscriptionMergeResultCode.BrakeWouldTrip)
                        {
                            var guardStateRepo = new BaseRepository<DbGdaDeviceSubscriptionGuardState>(adapterContext);
                            // Queried, not executed, so that a statement which matched NO row is distinguishable from
                            // one that updated the guard. The single id = 1 row is created by the schema scripts and a
                            // CHECK makes a second impossible, but both ClearDatabaseScripts delete it and re-insert it
                            // as separate steps, so a partially un-commented clear leaves the table empty. That matters
                            // more than it looks: the merge reads the row with COALESCE(IsTripped, false), so a missing
                            // row reads as UNTRIPPED and the guard silently stops being sticky.
                            var persistedRows = await guardStateRepo.QueryAsync(BuildSqlToPersistGuardTrip(), BuildGuardTripParameters(guardTrip), cancellationTokenSource, false, adapterContext);
                            guardTripPersisted = persistedRows.Any();
                        }

                        var refusedBatchRepo = new BaseRepository<DbGdaStgDeviceSubscription>(adapterContext);
                        await refusedBatchRepo.ExecuteAsync(BuildSqlToDeleteStagedBatch(), new object[] { new { BatchId = batchId } }, cancellationTokenSource, false, adapterContext);
                        await adapterUOW.CommitAsync();
                        return;
                    }

                    var failedRecords = new List<DbGdaQDeviceSubscriptionFail>();

                    foreach (var (queueRecord, failureReason) in malformedInstructions)
                    {
                        failedRecords.Add(dbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper.CreateEntity(queueRecord, failureReason));
                    }

                    var queueRecordsById = dbGdaQDeviceSubscriptions.ToDictionary(queueRecord => queueRecord.id);
                    foreach (var rejection in mergeResults.Where(mergeResult => mergeResult.Code == DeviceSubscriptionMergeResultCode.RowRejected))
                    {
                        if (rejection.QueueId == null || !queueRecordsById.TryGetValue(rejection.QueueId.Value, out var queueRecord))
                        {
                            // Unreachable for this intake: every row it stages carries the id of the queue record it
                            // came from. Logged rather than dropped so that a rejection can never vanish silently.
                            logger.Error($"The merge rejected a staged row that could not be matched to a claimed queue record. Identifier: '{rejection.Identifier}'. Reason: {rejection.ResultMessage}");
                            continue;
                        }

                        failedRecords.Add(dbGdaQDeviceSubscriptionDbGdaQDeviceSubscriptionFailEntityMapper.CreateEntity(queueRecord, rejection.ResultMessage ?? "Rejected by the merge procedure with no reason supplied."));
                    }

                    if (failedRecords.Count != 0)
                    {
                        await dbGdaQDeviceSubscriptionFailEntityPersister.PersistEntitiesToDatabaseAsync(
                            adapterContext, failedRecords, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Info);
                    }

                    foreach (var queueRecord in dbGdaQDeviceSubscriptions)
                    {
                        queueRecord.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;
                    }
                    await dbGdaQDeviceSubscriptionEntityPersister.PersistEntitiesToDatabaseAsync(
                        adapterContext, dbGdaQDeviceSubscriptions, cancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Info);

                    await adapterUOW.CommitAsync();

                    logger.Debug($"Persisted batch {batchId}: {stagingRecords.Count} staged, {failedRecords.Count} failed, {dbGdaQDeviceSubscriptions.Count} deleted from queue.");
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    await adapterUOW.RollBackAsync();
                    throw;
                }
            }, new Context());

            if (stagedRowsFound != null)
            {
                // Error, never Fatal - see LogRefusedMerge. Nothing is lost, but it should not happen.
                logger.Error($"Batch {batchId} was not merged by this attempt: {stagingRecords.Count} instruction(s) were staged for it, but only {stagedRowsFound} staged row(s) remained when its merge was about to run. Either another adapter instance's stale-staging sweep removed them ('StaleStagingBatchMinutes' is shorter than this batch waited) - then nothing from this batch was applied, and its {totalCount} queue record(s) are still claimed: they are claimed again once that claim is {ClaimStaleThresholdMinutes} minutes old, and anything already applied is then superseded rather than applied twice. Or an earlier attempt of this transaction committed before its acknowledgement was lost - then the batch was applied and its queue records deleted by that attempt. {digAdapterDatabaseObjectNames.DbGdaQDeviceSubscriptionTableName} tells the two apart: the claimed records remain only in the first case.");

                // The earlier attempt may have committed the merge, so the composed lookup is rebuilt as it is after
                // any applied batch. If nothing was applied, the rebuild costs one refresh.
                deviceLookup.Invalidate();

                return new BatchProcessingResult
                {
                    TotalCount = totalCount,
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            if (guardTrip != null)
            {
                LogRefusedMerge(guardTrip, guardTripPersisted);

                return new BatchProcessingResult
                {
                    TotalCount = totalCount,
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            var rejectedCount = mergeResults.Count(mergeResult => mergeResult.Code == DeviceSubscriptionMergeResultCode.RowRejected);
            var failureCount = rejectedCount + malformedInstructions.Count;
            var successCount = totalCount - failureCount;

            foreach (var summary in mergeResults.Where(mergeResult => mergeResult.Code == DeviceSubscriptionMergeResultCode.Completed))
            {
                logger.Info($"{summary.ResultMessage}");
            }
            foreach (var rejection in mergeResults.Where(mergeResult => mergeResult.Code == DeviceSubscriptionMergeResultCode.RowRejected))
            {
                logger.Warn($"Subscription instruction for identifier '{rejection.Identifier}' (serial number '{rejection.SerialNumber}') was rejected: {rejection.ResultMessage}");
            }
            foreach (var (queueRecord, failureReason) in malformedInstructions)
            {
                logger.Warn($"Subscription instruction {queueRecord.id} was rejected before staging: {failureReason}");
            }

            if (successCount > 0)
            {
                // Which devices may be transmitted for has changed, so the composed lookup is rebuilt on the next
                // transmit iteration rather than whenever the refresh interval happens to elapse.
                deviceLookup.Invalidate();
            }

            return new BatchProcessingResult
            {
                TotalCount = totalCount,
                SuccessCount = successCount,
                FailureCount = failureCount
            };
        }

        /// <summary>
        /// Builds the parameters that record a tripped termination guard from the merge's refusal row.
        /// </summary>
        /// <param name="refusal">The row the merge returned describing its refusal.</param>
        /// <returns>The parameters for the statement built by <see cref="BuildSqlToPersistGuardTrip"/>.</returns>
        object BuildGuardTripParameters(DeviceSubscriptionMergeResult refusal)
        {
            var trippedReason = refusal.ResultMessage ?? "The termination guard tripped.";
            if (trippedReason.Length > MaxTrippedReasonLength)
            {
                trippedReason = trippedReason[..MaxTrippedReasonLength];
            }

            // The refusal row has no queue record to refer to, so the merge overloads two of its columns to carry
            // the two numbers that describe the trip. See the XML documentation on DeviceSubscriptionMergeResult.
            return new
            {
                TrippedReason = trippedReason,
                TrippedTerminationCount = (int?)refusal.QueueOrdinal,
                TrippedWindowCount = (int?)refusal.QueueId
            };
        }

        /// <summary>
        /// Logs a merge that refused to apply anything. Called only once the refusal - and, for a trip, the
        /// termination guard's tripped state - has been committed, so the log never reports a refusal that was
        /// not recorded.
        /// </summary>
        /// <param name="refusal">The row the merge returned describing its refusal.</param>
        /// <param name="guardTripPersisted">Whether the statement recording a trip updated the guard row.</param>
        void LogRefusedMerge(DeviceSubscriptionMergeResult refusal, bool guardTripPersisted)
        {
            // Error, never Fatal. Fatal terminates the process in this application, and an operator cannot clear a
            // guard on an adapter that is no longer running. Re-logged every iteration so that the condition stays
            // visible for as long as it persists.
            logger.Error($"No subscription instructions were applied. {refusal.ResultMessage}");

            if (refusal.Code != DeviceSubscriptionMergeResultCode.BrakeWouldTrip)
            {
                return;
            }

            if (guardTripPersisted == false)
            {
                logger.Error($"The termination guard tripped, but it could NOT be recorded: {digAdapterDatabaseObjectNames.DbGdaDeviceSubscriptionGuardStateTableName} has no row with id = 1, so the update matched nothing. The guard is therefore NOT sticky - the next batch is evaluated as though it had never tripped, and there is nothing for an operator to clear. Restore the row before relying on the guard; the schema creation scripts insert it, and both ClearDatabaseScripts re-insert it as their last step.");
                return;
            }

            logger.Error($"The termination guard has been recorded as tripped in {digAdapterDatabaseObjectNames.DbGdaDeviceSubscriptionGuardStateTableName}. No subscription instruction will be applied until an operator has investigated and cleared it.");
        }

        void HandleAdapterDatabaseConnectionException(AdapterDatabaseConnectionException ex)
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
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        async Task PrepareForExecutionAsync(CancellationToken stoppingToken)
        {
            var prerequisiteServices = new List<DIGAdapterService>
            {
                // This service has no prerequisites. It is the entry point of a control plane that does not involve
                // any other service, which is what allows a deployment to run it without the provisioning workflow.
            };
            await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
            await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
        }

        /// <summary>
        /// Starts the current <see cref="DeviceSubscriptionService"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbGdaOServiceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbGdaOServiceTrackings, DIGAdapterService.DeviceSubscriptionService, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, DIGAdapterService.DeviceSubscriptionService, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            stateMachine.RegisterService(nameof(DeviceSubscriptionService), adapterConfiguration.EnableDeviceSubscriptionService);

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableDeviceSubscriptionService)
            {
                logger.Info($"******** STARTING SERVICE: {CurrentClassName}");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {CurrentClassName} service has not been enabled and will NOT be started. Nothing else reads gda.Q_DeviceSubscriptions, so any subscription instruction written to that queue will stay there unapplied and no device will become - or stop being - transmittable.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="DeviceSubscriptionService"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            stateMachine.RegisterService(nameof(DeviceSubscriptionService), false);
            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the <see cref="DbGdaOServiceTracking"/> record associated with the <see cref="DeviceSubscriptionService"/> service to show that the service is running.
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
                        adapterContext, DIGAdapterService.DeviceSubscriptionService, DateTime.UtcNow);
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
        /// Updates the <see cref="DbGdaOServiceTracking"/> record associated with the <see cref="DeviceSubscriptionService"/> service, including the metrics for the batch just processed.
        /// </summary>
        /// <param name="totalCount">The total number of records processed in the batch.</param>
        /// <param name="successCount">The number of successfully processed records.</param>
        /// <param name="failureCount">The number of failed records.</param>
        /// <returns></returns>
        async Task UpdateServiceTrackingRecordAsync(int totalCount, int successCount, int failureCount)
        {
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                try
                {
                    await serviceTracker.UpdateDbOServiceTrackingRecordWithMetricsAsync(
                        adapterContext, DIGAdapterService.DeviceSubscriptionService, DateTime.UtcNow,
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
        /// The shape of a row returned by the stale staging batch sweep.
        /// </summary>
        class StagingBatchIdentifier
        {
            public Guid BatchId { get; set; }
        }
    }
}
