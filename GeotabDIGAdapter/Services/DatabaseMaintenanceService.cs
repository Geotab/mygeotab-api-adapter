using Dapper.Contrib.Extensions;
using MyGeotabAPIAdapter;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.Services;
using NLog;
using Polly;
using Polly.Retry;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that keeps the partitioned tables in the adapter database supplied
    /// with the period partitions they need, and - unless retention has been switched off - reclaims the space
    /// held by failure records older than the configured retention window. It also performs Level 1 database
    /// maintenance (statistics, and on PostgreSQL vacuuming) and Level 2 database maintenance (index
    /// reorganizing, rebuilding and reindexing) on the gda schema, on the model of the MyGeotab API Adapter's
    /// DatabaseMaintenanceService2.
    /// <para>
    /// Partitioning and retention are done by database procedures, and the two levels by statements chosen from
    /// two views. This service exists to decide when each is due, with what arguments, and to hold the other
    /// services still while the work that needs it runs.
    /// </para>
    /// </summary>
    class DatabaseMaintenanceService : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        /// <summary>
        /// How long to wait for the other services to pause before giving up on an iteration.
        /// </summary>
        const int WaitTimeoutMinutesForPausingOtherServices = 5;

        /// <summary>
        /// How often to re-check whether the other services have paused.
        /// </summary>
        const int PauseCheckIntervalSeconds = 5;

        /// <summary>
        /// How long to wait before retrying an iteration that was abandoned because the other services did not
        /// pause. The configured interval is a day by default, and waiting a day to retry work that never ran
        /// would leave a database that needs partitions without them for that whole day.
        /// </summary>
        const int RetryDelayMinutesAfterAbandonedIteration = 5;

        /// <summary>
        /// The schema whose partitioned tables this service manages. It matches the scope the two procedures
        /// enforce for themselves; nothing outside it is ever read or written here.
        /// </summary>
        const string ManagedSchemaName = "gda";

        /// <summary>
        /// The SQL Server partition scheme the managed tables sit on. Declared in the schema creation scripts.
        /// </summary>
        const string MSSQLPartitionSchemeName = "DateTimePartitionScheme_GeotabDigAdapter";

        /// <summary>
        /// The prefix shared by every queue and fail table in the managed schema.
        /// </summary>
        const string QueueTableNamePrefix = "Q_";

        /// <summary>
        /// The suffix that distinguishes a fail table from the live queue it belongs to.
        /// </summary>
        const string FailTableNameSuffix = "Fail";

        /// <summary>
        /// The live queue the DeviceProvisioningService drains. Named here because it is one of only two
        /// live queues that the TelemetryDataService does not read.
        /// </summary>
        const string ProvisionDevicesQueueTableName = "Q_ProvisionDevices";

        /// <summary>
        /// The live queue the DeviceSubscriptionService drains.
        /// </summary>
        const string DeviceSubscriptionsQueueTableName = "Q_DeviceSubscriptions";

        /// <summary>
        /// How many invalid records to remove per DELETE statement.
        /// <para>
        /// Deliberately well below the roughly 5,000 locks at which SQL Server escalates to a table lock. A
        /// batch that escalates holds the whole table for its duration, which is the very thing batching is
        /// here to avoid.
        /// </para>
        /// </summary>
        const int InvalidRecordDeletionBatchSize = 2000;

        /// <summary>
        /// How long the invalid record sweep may keep deleting in one iteration before leaving the rest for the
        /// next one.
        /// <para>
        /// The first run after retention is switched on can face a backlog of any size, and the sweep is not
        /// worth an unbounded amount of database time. What it does not reach is simply removed next iteration.
        /// </para>
        /// </summary>
        const int MaxInvalidRecordDeletionMinutes = 10;

        /// <summary>
        /// How many times a maintenance statement is retried after a transient failure - a deadlock, or a failure to open
        /// a connection; a connection lost part-way through a statement matches no retry rule - for partition management,
        /// partition retirement, a batch of the invalid record sweep, a Level 1 or Level 2 statement, or a read of either
        /// maintenance view. A TIMEOUT is not retried at all, and on PostgreSQL that includes a statement stopped by a
        /// lock_timeout or statement_timeout set for the adapter's login or database.
        /// <para>
        /// The transaction retry policy used everywhere else here retries a timeout almost without limit, and each
        /// repository call beneath it ten more times with a longer command timeout on each attempt. That is right for a
        /// short statement that a transient condition will release. An index rebuild that times out is not that: it needs
        /// more time than it was given, every retry starts its work again from the beginning, and Level 2's offline rebuilds
        /// run while the other services are paused - so the retries would hold telemetry still for hours. So each maintenance statement
        /// is instead given ONE attempt at the longest command timeout any attempt could have,
        /// <see cref="DatabaseResilienceHelper.MaxTimeoutSeconds"/> - the single-attempt <see cref="BaseRepository{T}"/>
        /// each of those sites constructs - and a timeout ends its run.
        /// </para>
        /// <para>
        /// Partition management and retirement run with the other services paused too, so the same reasoning applies to
        /// them. Level 2's online operations and the invalid record sweep run outside the pause, but an unbounded retry of
        /// one of their statements would still hold this service - and with it the next iteration's partition creation -
        /// for as long as the retry went on, and a retried sweep batch would never reach the sweep's own time limit, which
        /// is checked between batches. A run that exhausts its retries is
        /// recorded as failed and attempted again after the service's interval.
        /// </para>
        /// </summary>
        const int MaxMaintenanceStatementRetries = 3;

        /// <summary>
        /// The PostgreSQL role that the schema scripts grant EXECUTE on pgstattuple to, and so the one named in the
        /// remedy when Level 2 cannot run.
        /// </summary>
        const string PostgreSQLClientRoleName = "geotabdigadapter_client";

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;
        readonly AsyncRetryPolicy asyncRetryPolicyForMaintenanceStatements;

        readonly IDIGAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DatabaseMaintenanceService> awaiter;
        readonly IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbGdaDigMaintenanceLog> dbGdaDigMaintenanceLogPersister;
        readonly IServiceTracker<DbGdaOServiceTracking> serviceTracker;
        readonly IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine;
        readonly IStringHelper stringHelper;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        // The previous depth report, kept so that each report can say how far each table has moved since the
        // last one. A count on its own says little; a count that has doubled every day for a week says a great
        // deal. A negative total means no report has been produced yet.
        long previousLiveQueueDepthTotal = -1;
        long previousFailTableDepthTotal = -1;
        long previousInvalidRecordDepth = -1;
        Dictionary<string, long> previousDepthByTableName = new(StringComparer.Ordinal);

        // When partition management and retention, and the table-depth report, last started. They keep the
        // service's own interval, which is what they have always run on; Level 1 and Level 2 read theirs from the
        // maintenance log instead. Held in memory rather than read back from the log so that every startup
        // runs them at once, as it always has - that is what creates any partition a stopped adapter missed.
        DateTime? intervalTasksLastStartedUtc;
        DateTime? depthReportLastProducedUtc;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMaintenanceService"/> class.
        /// </summary>
        public DatabaseMaintenanceService(
            IDIGAdapterConfiguration adapterConfiguration,
            IAdapterEnvironment<DbGdaOServiceTracking> adapterEnvironment,
            IBackgroundServiceAwaiter<DatabaseMaintenanceService> awaiter,
            IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames,
            IExceptionHelper exceptionHelper,
            IGenericEntityPersister<DbGdaDigMaintenanceLog> dbGdaDigMaintenanceLogPersister,
            IServiceTracker<DbGdaOServiceTracking> serviceTracker,
            IStateMachine<DbGdaMiddlewareVersionInfo> stateMachine,
            IStringHelper stringHelper,
            IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.digAdapterDatabaseObjectNames = digAdapterDatabaseObjectNames;
            this.exceptionHelper = exceptionHelper;
            this.dbGdaDigMaintenanceLogPersister = dbGdaDigMaintenanceLogPersister;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.stringHelper = stringHelper;
            this.adapterContext = adapterContext;

            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy, and a bounded one that does not retry a timeout for the maintenance statements themselves.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
            asyncRetryPolicyForMaintenanceStatements = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger, MaxMaintenanceStatementRetries, retryTimeouts: false);
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // The service wakes at the shortest interval it has to honour, and each task then runs only once its
            // own interval has elapsed - see DigMaintenancePolicy.GetServiceTickMinutes.
            var configuredDelayTimeSpan = TimeSpan.FromMinutes(DigMaintenancePolicy.GetServiceTickMinutes(adapterConfiguration));
            var retryDelayTimeSpan = TimeSpan.FromMinutes(RetryDelayMinutesAfterAbandonedIteration);
            logger.Info($"{nameof(DatabaseMaintenanceService)} will check for due maintenance every {configuredDelayTimeSpan.TotalMinutes} minute(s). Partition management, retention and the table-depth report run every {adapterConfiguration.DatabaseMaintenanceServiceIntervalMinutes} minute(s); Level 1 database maintenance {DescribeLevelSchedule(adapterConfiguration.EnableLevel1DigMaintenance, adapterConfiguration.Level1DigMaintenanceIntervalMinutes)}; Level 2 database maintenance {DescribeLevelSchedule(adapterConfiguration.EnableLevel2DigMaintenance, adapterConfiguration.Level2DigMaintenanceIntervalMinutes)}.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await PrepareForExecutionAsync(stoppingToken);

                var delayTimeSpan = configuredDelayTimeSpan;

                try
                {
                    var iterationCompleted = await ExecuteIterationBusinessLogicAsync(stoppingToken);

                    // Retry sooner than the configured interval when nothing was done, so that a database
                    // waiting on partitions is not left waiting for a full interval.
                    if (iterationCompleted == false && retryDelayTimeSpan < configuredDelayTimeSpan)
                    {
                        delayTimeSpan = retryDelayTimeSpan;
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

                await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Wait, stoppingToken);
            }
        }

        /// <summary>
        /// Executes the business logic for a single iteration, running each task that is due, in this order:
        /// <list type="number">
        /// <item>the table-depth report;</item>
        /// <item>Level 1 maintenance, which is online work and runs outside the maintenance pause;</item>
        /// <item>with the other services paused: partition creation, then - if automatic retention is enabled -
        /// partition retirement;</item>
        /// <item>with the pause released: the invalid record sweep, if automatic retention is enabled;</item>
        /// <item>Level 2 maintenance, last: it finds the indexes to maintain with the other services running, pauses them
        /// again only for its offline operations, and runs its online operations after that pause is released.</item>
        /// </list>
        /// Partition management, retention and the depth report run on the service's own interval; Level 1 and
        /// Level 2 on theirs. The pause is requested only when something that needs it is due.
        /// </summary>
        /// <returns><c>true</c> if every task that was due ran; <c>false</c> if the iteration was abandoned
        /// because the other services did not pause in time.</returns>
        async Task<bool> ExecuteIterationBusinessLogicAsync(CancellationToken stoppingToken)
        {
            MethodBase? methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Started iteration of {methodBase?.ReflectedType?.Name}.{methodBase?.Name}");

            var iterationStopwatch = Stopwatch.StartNew();

            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

            var iterationStartUtc = DateTime.UtcNow;
            var intervalTasksDue = IsServiceIntervalElapsed(intervalTasksLastStartedUtc, iterationStartUtc);

            // Reported first, so that it happens even on an iteration that is later abandoned because the other
            // services did not pause - which is exactly the kind of iteration during which a backlog builds. It
            // keeps the service's own interval, so that it is not produced every time the service wakes.
            if (IsServiceIntervalElapsed(depthReportLastProducedUtc, iterationStartUtc) == true)
            {
                depthReportLastProducedUtc = iterationStartUtc;
                await ReportTableDepthsAsync(cancellationTokenSource);
            }

            // Level 1 is online work - statistics, and on PostgreSQL vacuuming - so it runs outside the pause. The
            // MyGeotab API Adapter does not pause for it either, and holding telemetry still for it would be a
            // self-inflicted outage.
            await ExecuteLevel1DatabaseMaintenanceIfDueAsync(cancellationTokenSource);

            // Partition management and retention, when due. The minimum date is worked out BEFORE the pause is requested: on
            // the first run it reads every partitioned table to find the oldest record, which needs nothing held still.
            if (intervalTasksDue == true)
            {
                var minDateTimeUtc = await GetMinDateTimeUtcAsync(cancellationTokenSource);
                if (minDateTimeUtc == null)
                {
                    // A configuration error that an operator has to correct. Recorded as having started, so that it
                    // is reported once per interval rather than on every wake: retrying cannot help.
                    intervalTasksLastStartedUtc = iterationStartUtc;
                }
                else
                {
                    var partitionStartDateTimeUtc = minDateTimeUtc.Value;
                    var runsUnderPause = new List<MaintenanceRunOutcome>();
                    try
                    {
                        var otherServicesPaused = await PauseOtherServicesForDatabaseMaintenanceAsync($"Partition management and retention will be attempted again in {RetryDelayMinutesAfterAbandonedIteration} minutes, and Level 2, if it is due, after them.", cancellationTokenSource);
                        if (otherServicesPaused == false)
                        {
                            return false;
                        }

                        // Recorded as soon as the work starts rather than once it succeeds, so that a failure is
                        // attempted again after the configured interval - as it always has been - rather than on the
                        // next wake.
                        intervalTasksLastStartedUtc = iterationStartUtc;

                        // Partition creation runs FIRST. Retention removes whole partitions, and a database with no
                        // partition covering the current period would have nowhere to put the records written while
                        // this iteration runs. A failure here stops retention from running after it.
                        var partitionRun = await RunMaintenanceTaskAsync(DigMaintenanceType.Partition, () => ManagePartitionsAsync(partitionStartDateTimeUtc, cancellationTokenSource));
                        runsUnderPause.Add(partitionRun);

                        if (partitionRun.Failure == null)
                        {
                            if (adapterConfiguration.EnableAutomaticRetention == true)
                            {
                                runsUnderPause.Add(await RunMaintenanceTaskAsync(DigMaintenanceType.Retention, () => RetirePartitionsAsync(cancellationTokenSource)));
                            }
                            else
                            {
                                logger.Debug($"Automatic retention is disabled. No failure records will be removed.");
                            }
                        }
                    }
                    finally
                    {
                        // Let the other services resume, whether or not the maintenance succeeded.
                        stateMachine.SetStateReason(StateReason.AdapterDatabaseMaintenance, false);
                    }

                    // Recorded only now, once the other services have resumed, as the MyGeotab API Adapter records its
                    // own maintenance. Writing a row goes through the transaction retry policy, which waits out almost
                    // anything, so a row written inside the pause held telemetry still for as long as whatever blocked
                    // the insert - an operator's open transaction on the log table, say - rather than for the hour at
                    // most that bounds each maintenance statement. A failed run is rethrown only once it has been
                    // recorded, so a failed partition run still ends this iteration, exactly as it did before.
                    await RecordMaintenanceRunsAsync(runsUnderPause, cancellationTokenSource);
                    runsUnderPause.FirstOrDefault(run => run.Failure != null)?.Failure?.Throw();

                    // Deliberately AFTER the pause is released. The invalid records are removed by a batched DELETE
                    // rather than by dropping a partition, so it takes nothing but short-lived row locks on rows that
                    // are, by definition, older than anything being written. A first run against a long backlog can
                    // keep deleting for minutes, and there is no reason to hold telemetry still for it.
                    if (adapterConfiguration.EnableAutomaticRetention == true)
                    {
                        await RunAndLogAsync(DigMaintenanceType.Retention, () => DeleteExpiredInvalidRecordsAsync(cancellationTokenSource), cancellationTokenSource);
                    }
                }
            }

            // Level 2 comes LAST, deliberately after retention: rebuilding an index on a partition that is about to be retired
            // is wasted work. It decides whether it is due, and which indexes to maintain, only now - so that its plan cannot
            // name a partition retention has just removed - and it runs in every iteration it is due in, so that a service
            // interval shorter than its own cannot keep putting it off.
            if (await ExecuteLevel2DatabaseMaintenanceIfDueAsync(cancellationTokenSource) == false)
            {
                return false;
            }

            // The OServiceTracking record for this service needs to be updated to show that the service is
            // operating. The overload WITHOUT metrics is used deliberately: the metrics overload assigns the
            // batch size rather than accumulating it, and this service processes no batches.
            await UpdateServiceTrackingRecordAsync();

            iterationStopwatch.Stop();
            logger.Debug($"Database maintenance iteration completed in {iterationStopwatch.ElapsedMilliseconds}ms.");

            logger.Trace($"Completed iteration of {methodBase?.ReflectedType?.Name}.{methodBase?.Name}");
            return true;
        }

        /// <summary>
        /// Whether the service's own interval has elapsed since a task that runs on it last started. A task that
        /// has not run since startup is always due.
        /// </summary>
        bool IsServiceIntervalElapsed(DateTime? lastStartedUtc, DateTime nowUtc)
        {
            return lastStartedUtc == null || (nowUtc - lastStartedUtc.Value).TotalMinutes >= adapterConfiguration.DatabaseMaintenanceServiceIntervalMinutes;
        }

        /// <summary>
        /// Renders whether a maintenance level is enabled and, if it is, how often it runs.
        /// </summary>
        static string DescribeLevelSchedule(bool isEnabled, int intervalMinutes)
        {
            return isEnabled == true ? $"every {intervalMinutes} minute(s)" : "is disabled";
        }

        /// <summary>
        /// The three kinds of table the depth report covers. They are counted together but mean different
        /// things, so a depth worth remarking on in one is unremarkable in another.
        /// </summary>
        enum DigTableFamily
        {
            /// <summary>
            /// A live queue an upstream integration writes to and the TelemetryDataService drains. Its normal
            /// depth is near zero, so a large one means records are arriving faster than they leave.
            /// </summary>
            LiveQueue,

            /// <summary>
            /// A fail table holding records that could not be transmitted. It only ever accumulates, so its
            /// depth is bounded by retention and by nothing else.
            /// </summary>
            FailTable,

            /// <summary>
            /// DIGInvalidRecords, holding records the Data Intake Gateway accepted and then rejected.
            /// </summary>
            InvalidRecords
        }

        /// <summary>
        /// Counts the records held by every queue, fail and invalid record table, and reports how each has moved
        /// since the previous report.
        /// <para>
        /// Nothing else in the adapter says how deep these tables are. A queue that is not being drained and a
        /// fail table that is not being reclaimed both end the same way - a full disk - and both are obvious for
        /// weeks beforehand to anyone who thinks to look. This is what makes looking unnecessary.
        /// </para>
        /// <para>
        /// A failure here is logged and swallowed. Counting is the one thing this service does that the database
        /// does not need done, and an observability query that could stop partitions being created would be
        /// worse than no observability at all.
        /// </para>
        /// </summary>
        async Task ReportTableDepthsAsync(CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                var reportStopwatch = Stopwatch.StartNew();
                var depths = (await GetTableDepthsAsync(cancellationTokenSource)).ToList();
                reportStopwatch.Stop();

                if (depths.Count == 0)
                {
                    logger.Warn($"No queue, fail or invalid record table was found in the '{ManagedSchemaName}' schema, so there are no table depths to report. This is expected only if the adapter is pointed at a database that does not hold the DIG Adapter schema.");
                    return;
                }

                var liveQueueDepthTotal = depths.Where(depth => ClassifyTable(depth.TableName) == DigTableFamily.LiveQueue).Sum(depth => depth.Depth);
                var failTableDepthTotal = depths.Where(depth => ClassifyTable(depth.TableName) == DigTableFamily.FailTable).Sum(depth => depth.Depth);
                var invalidRecordDepth = depths.Where(depth => ClassifyTable(depth.TableName) == DigTableFamily.InvalidRecords).Sum(depth => depth.Depth);

                // Only the non-empty tables are named. In a healthy deployment that is a short list, and a
                // report that named all 27 every time would be one an operator learned to skip.
                var nonEmptyDepths = depths
                    .Where(depth => depth.Depth > 0)
                    .OrderByDescending(depth => depth.Depth)
                    .ThenBy(depth => depth.TableName, StringComparer.Ordinal)
                    .ToList();
                var nonEmptyBreakdown = nonEmptyDepths.Count == 0
                    ? "none"
                    : string.Join(", ", nonEmptyDepths.Select(depth => $"{depth.TableName}:{depth.Depth:N0}"));

                logger.Info($"Table depth report. Live queues: {liveQueueDepthTotal:N0} record(s) (delta: {FormatDelta(liveQueueDepthTotal, previousLiveQueueDepthTotal)}); fail tables: {failTableDepthTotal:N0} (delta: {FormatDelta(failTableDepthTotal, previousFailTableDepthTotal)}); invalid records: {invalidRecordDepth:N0} (delta: {FormatDelta(invalidRecordDepth, previousInvalidRecordDepth)}). Non-empty: [{nonEmptyBreakdown}]. {depths.Count} table(s) counted in {reportStopwatch.ElapsedMilliseconds}ms.");

                WarnAboutTablesOverTheDepthThreshold(depths);
                WarnAboutQueuesWithNoEnabledReader(depths);

                previousLiveQueueDepthTotal = liveQueueDepthTotal;
                previousFailTableDepthTotal = failTableDepthTotal;
                previousInvalidRecordDepth = invalidRecordDepth;
                previousDepthByTableName = depths.ToDictionary(depth => depth.TableName, depth => depth.Depth, StringComparer.Ordinal);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Warn, $"{CurrentClassName} could not report table depths. Database maintenance will continue; only the depth report is lost");
            }
        }

        /// <summary>
        /// Logs a warning for each table holding more records than the configured threshold, saying what that
        /// means for the kind of table it is.
        /// </summary>
        void WarnAboutTablesOverTheDepthThreshold(IReadOnlyList<DigTableDepth> depths)
        {
            var threshold = adapterConfiguration.QueueDepthWarningThreshold;
            if (threshold <= 0)
            {
                // Zero is the documented way to keep the report and switch the warnings off - for a deployment
                // whose normal depths are simply larger than any threshold worth setting.
                return;
            }

            foreach (var depth in depths.Where(depth => depth.Depth > threshold).OrderByDescending(depth => depth.Depth))
            {
                var family = ClassifyTable(depth.TableName);
                var consequence = family switch
                {
                    // Named per queue rather than in general: the two control-plane queues are not drained
                    // by the TelemetryDataService, and sending an operator to look at the throughput of a
                    // service that never touches the table is worse than not naming a service at all.
                    DigTableFamily.LiveQueue =>
                        $"Records are arriving faster than the {DescribeReaderOf(depth.TableName).ServiceName} is draining them, or - for a telemetry queue - they belong to devices that are not authorized to send and are being held. Check that service's throughput and, for a telemetry queue, the composed device lookup.",
                    DigTableFamily.FailTable when adapterConfiguration.EnableAutomaticRetention == false =>
                        $"Automatic retention is DISABLED, so nothing will ever remove these records and the table will keep growing until the disk is full. Enable it, or reclaim the space yourself.",
                    DigTableFamily.FailTable =>
                        $"Records older than {adapterConfiguration.FailTableRetentionDays} day(s) are reclaimed automatically, so a table this size means records are still being failed at a rate worth investigating. The reason is recorded against each record.",
                    DigTableFamily.InvalidRecords when adapterConfiguration.EnableAutomaticRetention == false =>
                        "Automatic retention is DISABLED, so nothing will ever remove these records. Enable it, or reclaim the space yourself.",
                    _ =>
                        $"Records older than {adapterConfiguration.FailTableRetentionDays} day(s) are reclaimed automatically, so a table this size means the Data Intake Gateway is still rejecting records at a rate worth investigating."
                };

                var previousDepthDescription = previousDepthByTableName.TryGetValue(depth.TableName, out var previousDepth)
                    ? $" (delta: {FormatDelta(depth.Depth, previousDepth)} since the previous report)"
                    : string.Empty;

                logger.Warn($"{ManagedSchemaName}.{depth.TableName} holds {depth.Depth:N0} record(s){previousDepthDescription}, which is above the configured warning threshold of {threshold:N0}. {consequence}");
            }
        }

        /// <summary>
        /// Logs a warning for each live queue that holds records while the service that drains it is disabled.
        /// <para>
        /// Deliberately independent of the depth threshold. A threshold answers "is this queue deeper than
        /// usual"; this answers "is anything ever going to read it", and one record nothing will read is
        /// already the whole defect. The only signal before this was the startup line saying a service was
        /// disabled - which fires identically whether the queue it drains is empty or holds a month of records
        /// nobody is coming for, so it says nothing about whether anything is actually stuck.
        /// </para>
        /// </summary>
        void WarnAboutQueuesWithNoEnabledReader(IReadOnlyList<DigTableDepth> depths)
        {
            var unreadQueues = depths
                .Where(depth => depth.Depth > 0 && ClassifyTable(depth.TableName) == DigTableFamily.LiveQueue)
                .Select(depth => new { Depth = depth, Reader = DescribeReaderOf(depth.TableName) })
                .Where(candidate => candidate.Reader.IsEnabled == false)
                .OrderByDescending(candidate => candidate.Depth.Depth);

            foreach (var unreadQueue in unreadQueues)
            {
                logger.Warn($"{ManagedSchemaName}.{unreadQueue.Depth.TableName} holds {unreadQueue.Depth.Depth:N0} record(s) and NOTHING WILL READ THEM. The {unreadQueue.Reader.ServiceName} is what drains this queue and it is disabled ('{unreadQueue.Reader.SettingName}' is false), so those records will stay where they are indefinitely and anything further written to the queue will accumulate behind them. Enable it, or stop writing to this queue and remove the records yourself.");
            }
        }

        /// <summary>
        /// The service that drains a live queue, and whether this configuration leaves it able to.
        /// </summary>
        /// <param name="ServiceName">The service that claims records from the queue.</param>
        /// <param name="SettingName">The configuration key that is holding the queue shut, when one is.</param>
        /// <param name="IsEnabled">Whether anything will read the queue.</param>
        readonly record struct QueueReader(string ServiceName, string SettingName, bool IsEnabled);

        /// <summary>
        /// Works out which service drains a given live queue, and whether the configuration lets it.
        /// <para>
        /// The two control-plane queues have one service each. Every other live queue is a telemetry queue,
        /// and a telemetry queue is read only while BOTH the TelemetryDataService and that record type's own
        /// enable key are set. The two are reported separately because the fix is a different key: the
        /// service-level key is named when it is the one that is off, and the record type's key otherwise.
        /// </para>
        /// </summary>
        QueueReader DescribeReaderOf(string queueTableName)
        {
            if (queueTableName == ProvisionDevicesQueueTableName)
            {
                return new QueueReader(nameof(DeviceProvisioningService), nameof(IDIGAdapterConfiguration.EnableDeviceProvisioningService), adapterConfiguration.EnableDeviceProvisioningService);
            }

            if (queueTableName == DeviceSubscriptionsQueueTableName)
            {
                return new QueueReader(nameof(DeviceSubscriptionService), nameof(IDIGAdapterConfiguration.EnableDeviceSubscriptionService), adapterConfiguration.EnableDeviceSubscriptionService);
            }

            if (adapterConfiguration.EnableTelemetryDataService == false)
            {
                return new QueueReader(nameof(TelemetryDataService), nameof(IDIGAdapterConfiguration.EnableTelemetryDataService), false);
            }

            var recordType = DescribeRecordTypeOf(queueTableName);
            return recordType == null
                ? new QueueReader(nameof(TelemetryDataService), nameof(IDIGAdapterConfiguration.EnableTelemetryDataService), true)
                : new QueueReader(nameof(TelemetryDataService), recordType.Value.SettingName, recordType.Value.IsEnabled);
        }

        /// <summary>
        /// Says whether the record type a telemetry queue holds is enabled, and names the key that decides it.
        /// <para>
        /// An arm per record type rather than a reflective lookup on the key name. The property name and the
        /// appsettings key are the same word by convention, but a convention is not a contract, and a
        /// reflective lookup that silently found nothing would reintroduce the exact silence this check exists
        /// to remove. A queue with no arm here returns null and is judged on the service-level key alone, so a
        /// telemetry queue added later without touching this method costs a missed warning, never a wrong one.
        /// </para>
        /// </summary>
        (string SettingName, bool IsEnabled)? DescribeRecordTypeOf(string queueTableName) => queueTableName switch
        {
            "Q_AccelerationRecords" => (nameof(IDIGAdapterConfiguration.EnableAccelerationRecords), adapterConfiguration.EnableAccelerationRecords),
            "Q_BinaryRecords" => (nameof(IDIGAdapterConfiguration.EnableBinaryRecords), adapterConfiguration.EnableBinaryRecords),
            "Q_BluetoothRecords" => (nameof(IDIGAdapterConfiguration.EnableBluetoothRecords), adapterConfiguration.EnableBluetoothRecords),
            "Q_DriverChangeRecords" => (nameof(IDIGAdapterConfiguration.EnableDriverChangeRecords), adapterConfiguration.EnableDriverChangeRecords),
            "Q_GenericFaultRecords" => (nameof(IDIGAdapterConfiguration.EnableGenericFaultRecords), adapterConfiguration.EnableGenericFaultRecords),
            "Q_GenericStatusRecords" => (nameof(IDIGAdapterConfiguration.EnableGenericStatusRecords), adapterConfiguration.EnableGenericStatusRecords),
            "Q_GpsRecords" => (nameof(IDIGAdapterConfiguration.EnableGpsRecords), adapterConfiguration.EnableGpsRecords),
            "Q_J1708FaultRecords" => (nameof(IDIGAdapterConfiguration.EnableJ1708FaultRecords), adapterConfiguration.EnableJ1708FaultRecords),
            "Q_J1939FaultRecords" => (nameof(IDIGAdapterConfiguration.EnableJ1939FaultRecords), adapterConfiguration.EnableJ1939FaultRecords),
            "Q_ObdiiFaultRecords" => (nameof(IDIGAdapterConfiguration.EnableObdiiFaultRecords), adapterConfiguration.EnableObdiiFaultRecords),
            "Q_VinRecords" => (nameof(IDIGAdapterConfiguration.EnableVinRecords), adapterConfiguration.EnableVinRecords),
            _ => null
        };

        /// <summary>
        /// Decides which kind of table a name belongs to. The naming convention it reads is the schema's own and
        /// is enforced by the schema creation scripts: every queue is Q_&lt;RecordType&gt;, and its fail table is
        /// the same name with Fail appended.
        /// </summary>
        static DigTableFamily ClassifyTable(string tableName)
        {
            if (tableName.StartsWith(QueueTableNamePrefix, StringComparison.Ordinal) == false)
            {
                return DigTableFamily.InvalidRecords;
            }

            return tableName.EndsWith(FailTableNameSuffix, StringComparison.Ordinal)
                ? DigTableFamily.FailTable
                : DigTableFamily.LiveQueue;
        }

        /// <summary>
        /// Renders how a value has moved since the previous report, or "initial" when there is no previous one.
        /// </summary>
        static string FormatDelta(long currentValue, long previousValue)
        {
            return previousValue < 0 ? "initial" : $"{currentValue - previousValue:+#,##0;-#,##0;0}";
        }

        /// <summary>
        /// Counts the records in every queue, fail and invalid record table, in a single round trip.
        /// <para>
        /// The counts are exact. PostgreSQL's catalog estimate would be far cheaper, but it is only as current
        /// as the last time the table was analyzed - and a queue that fills up between two analyses is precisely
        /// what this report exists to show. The cost is paid once per maintenance iteration, which is once a day
        /// by default, and both engines answer COUNT from the narrowest index rather than from the table.
        /// </para>
        /// <para>
        /// Both queries are attempted once, with the configured database timeout, through the maintenance-statement
        /// policy. Under the transaction policy a count that could not finish in time - a very large table, or one
        /// another session had locked - was retried with a growing timeout, and indefinitely past an hour, and this
        /// report runs before everything else the service does, partition creation included. Now it fails once, the
        /// failure is logged, and the rest of the iteration goes ahead.
        /// </para>
        /// </summary>
        async Task<IEnumerable<DigTableDepth>> GetTableDepthsAsync(CancellationTokenSource cancellationTokenSource)
        {
            var countableTables = (await GetCountableTablesAsync(cancellationTokenSource)).ToList();
            if (countableTables.Count == 0)
            {
                return Enumerable.Empty<DigTableDepth>();
            }

            var perTableSelects = new List<string>();
            foreach (var countableTable in countableTables)
            {
                // Every name here comes from the catalog, and is validated all the same: it is interpolated into
                // a statement rather than passed as a parameter, which a table name cannot be.
                if (stringHelper.IsValidIdentifierForDatabaseObject(countableTable.TableName) == false)
                {
                    throw new Exception($"The table name '{countableTable.TableName}' is not a valid identifier for a database object.");
                }

                perTableSelects.Add(adapterContext.ProviderType switch
                {
                    ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                        $"SELECT '{countableTable.TableName}' AS name, COUNT(*) AS depth FROM \"{ManagedSchemaName}\".\"{countableTable.TableName}\"",
                    // COUNT_BIG, not COUNT: COUNT returns an int and raises an arithmetic overflow error rather
                    // than a number once a table passes 2,147,483,647 records - which is a size these tables can
                    // reach, and the exact size at which a depth report matters most.
                    ConnectionInfo.DataAccessProviderType.SQLServer =>
                        $"SELECT '{countableTable.TableName}' AS name, COUNT_BIG(*) AS depth FROM [{ManagedSchemaName}].[{countableTable.TableName}]",
                    _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
                });
            }

            var columnAliases = adapterContext.ProviderType == ConnectionInfo.DataAccessProviderType.PostgreSQL
                ? "name AS \"TableName\", depth AS \"Depth\""
                : "name AS [TableName], depth AS [Depth]";
            var sql = $"SELECT {columnAliases} FROM ({string.Join(" UNION ALL ", perTableSelects)}) perTable;";

            return await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DigTableDepth>(adapterContext, adapterContext.TimeoutSecondsForDatabaseTasks);
                return await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);
            }, new Context());
        }

        /// <summary>
        /// Lists the tables whose depth is reported: every queue and fail table in the managed schema, plus the
        /// invalid record table. Discovered from the catalog rather than listed here, so that a record type
        /// added in a later release is reported without editing this method.
        /// </summary>
        async Task<IEnumerable<DigCountableTable>> GetCountableTablesAsync(CancellationTokenSource cancellationTokenSource)
        {
            var invalidRecordsTableName = digAdapterDatabaseObjectNames.DbGdaDIGInvalidRecordsTableName.Split('.').Last();

            var sql = adapterContext.ProviderType switch
            {
                // relispartition excludes the period children of a partitioned fail table. They are ordinary
                // tables whose names start with the parent's, so without it every fail table would be counted
                // once as a whole and once more per period.
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT c.relname AS ""TableName""
                       FROM pg_catalog.pg_class c
                       JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace AND n.nspname = '{ManagedSchemaName}'
                       WHERE c.relkind IN ('r', 'p')
                         AND c.relispartition = false
                         AND (LEFT(c.relname, {QueueTableNamePrefix.Length}) = '{QueueTableNamePrefix}' OR c.relname = '{invalidRecordsTableName}')
                       ORDER BY c.relname;",
                // A partitioned table is a single sys.tables row on SQL Server, so there are no children to
                // exclude here.
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $@"SELECT t.name AS [TableName]
                       FROM sys.tables t
                       JOIN sys.schemas s ON s.schema_id = t.schema_id
                       WHERE s.name = '{ManagedSchemaName}'
                         AND (LEFT(t.name, {QueueTableNamePrefix.Length}) = '{QueueTableNamePrefix}' OR t.name = '{invalidRecordsTableName}')
                       ORDER BY t.name;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };

            return await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DigCountableTable>(adapterContext, adapterContext.TimeoutSecondsForDatabaseTasks);
                return await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);
            }, new Context());
        }

        /// <summary>
        /// Determines the earliest date to create partitions from.
        /// <para>
        /// Once the first run has happened this is simply the value the database recorded then: the procedures
        /// pin it and refuse anything earlier, because moving it would ask for partitions in a range the
        /// existing ones already cover.
        /// </para>
        /// <para>
        /// On the FIRST run it is the oldest record already in the partitioned tables, not today. Getting this
        /// wrong is permanent and silent: records older than the earliest partition sit in PostgreSQL's DEFAULT
        /// partition, or in SQL Server's open-ended leading partition, where retention can never reclaim them
        /// by period, and the value cannot be lowered afterwards.
        /// </para>
        /// </summary>
        /// <returns>The date to create partitions from, or <c>null</c> if the configuration contradicts what the
        /// database recorded and an operator has to correct it.</returns>
        async Task<DateTime?> GetMinDateTimeUtcAsync(CancellationTokenSource cancellationTokenSource)
        {
            var recordedPartitionInfo = await GetRecordedPartitionInfoAsync(cancellationTokenSource);
            if (recordedPartitionInfo != null)
            {
                if (string.Equals(recordedPartitionInfo.InitialPartitionInterval, adapterConfiguration.PartitionInterval, StringComparison.OrdinalIgnoreCase) == false)
                {
                    // The procedure would refuse this itself, but saying so here names the setting and the
                    // recorded value - which the database error does not - and avoids taking the maintenance
                    // pause to reach a refusal that is already certain.
                    logger.Error($"The configured partition interval '{adapterConfiguration.PartitionInterval}' does not match the interval '{recordedPartitionInfo.InitialPartitionInterval}' recorded in {digAdapterDatabaseObjectNames.DbGdaDigPartitionInfoTableName} when partitions were first created. The interval cannot be changed on a partitioned database: two partition layouts cannot coexist on one table. Restore the configured value to '{recordedPartitionInfo.InitialPartitionInterval}'. No partition management or retention will be performed until this is corrected; Level 1 and Level 2 maintenance continue.");
                    return null;
                }

                return recordedPartitionInfo.InitialMinDateTimeUtc;
            }

            logger.Info($"No {digAdapterDatabaseObjectNames.DbGdaDigPartitionInfoTableName} record exists, so this is the first time partitions are being created. Looking for the oldest existing record to determine the earliest period that must be covered.");

            var oldestRecordCreationTimeUtc = await GetOldestRecordCreationTimeUtcAsync(cancellationTokenSource);
            if (oldestRecordCreationTimeUtc == null)
            {
                logger.Info($"The partitioned tables are empty. Partitions will be created from today onward.");
                return DateTime.UtcNow.Date;
            }

            // Never later than today, so that an empty-but-for-a-future-dated-record table cannot push the
            // starting point beyond the periods that are about to be needed.
            var minDateTimeUtc = oldestRecordCreationTimeUtc.Value.Date;
            if (minDateTimeUtc > DateTime.UtcNow.Date)
            {
                minDateTimeUtc = DateTime.UtcNow.Date;
            }

            logger.Info($"The oldest existing record was created {oldestRecordCreationTimeUtc.Value:yyyy-MM-dd HH:mm:ss} UTC. Partitions will be created from {minDateTimeUtc:yyyy-MM-dd} onward. This value is recorded permanently and cannot be lowered afterward.");
            return minDateTimeUtc;
        }

        /// <summary>
        /// Reads the single <see cref="DbGdaDigPartitionInfo"/> record, or returns <c>null</c> if partitioning
        /// has never been performed.
        /// </summary>
        async Task<DbGdaDigPartitionInfo?> GetRecordedPartitionInfoAsync(CancellationTokenSource cancellationTokenSource)
        {
            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DbGdaDigPartitionInfo>(adapterContext);
                var records = await repo.GetAllAsync(cancellationTokenSource, useStandaloneDbConnection: true, uowContext: adapterContext);
                return records.OrderBy(record => record.id).FirstOrDefault();
            }, new Context());
        }

        /// <summary>
        /// Finds the creation time of the oldest record held by any of the partitioned tables, or <c>null</c> if
        /// they are all empty.
        /// <para>
        /// The tables are discovered from the database catalog rather than listed here, so that a table
        /// partitioned in a later release is included without editing this method - and so that this can never
        /// disagree with the two procedures about which tables are managed.
        /// </para>
        /// </summary>
        async Task<DateTime?> GetOldestRecordCreationTimeUtcAsync(CancellationTokenSource cancellationTokenSource)
        {
            var partitionedTables = (await GetPartitionedTablesAsync(cancellationTokenSource)).ToList();
            if (partitionedTables.Count == 0)
            {
                logger.Warn($"No partitioned table was found in the '{ManagedSchemaName}' schema. Partition maintenance will have nothing to do. This is expected only if the database schema predates fail table partitioning.");
                return null;
            }

            // Every identifier below comes from the catalog, and is validated all the same: this is the only
            // place in this service where an identifier is interpolated into a statement rather than passed as
            // a parameter, which identifiers cannot be.
            var perTableSelects = new List<string>();
            foreach (var partitionedTable in partitionedTables)
            {
                if (stringHelper.IsValidIdentifierForDatabaseObject(partitionedTable.TableName) == false || stringHelper.IsValidIdentifierForDatabaseObject(partitionedTable.KeyColumnName) == false)
                {
                    throw new Exception($"The table name '{partitionedTable.TableName}' and/or the partitioning column name '{partitionedTable.KeyColumnName}' are not valid identifiers for a database object.");
                }

                perTableSelects.Add(adapterContext.ProviderType switch
                {
                    ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                        $"SELECT MIN(\"{partitionedTable.KeyColumnName}\") AS oldest FROM \"{ManagedSchemaName}\".\"{partitionedTable.TableName}\"",
                    ConnectionInfo.DataAccessProviderType.SQLServer =>
                        $"SELECT MIN([{partitionedTable.KeyColumnName}]) AS oldest FROM [{ManagedSchemaName}].[{partitionedTable.TableName}]",
                    _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
                });
            }

            var oldestColumnAlias = adapterContext.ProviderType == ConnectionInfo.DataAccessProviderType.PostgreSQL
                ? "\"OldestRecordCreationTimeUtc\""
                : "[OldestRecordCreationTimeUtc]";
            var sql = $"SELECT MIN(oldest) AS {oldestColumnAlias} FROM ({string.Join(" UNION ALL ", perTableSelects)}) perTable;";

            logger.Debug($"Scanning {partitionedTables.Count} partitioned table(s) for the oldest record. This happens only before partitions are first created.");

            var results = await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DigOldestRecordResult>(adapterContext);
                return await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);
            }, new Context());

            return results.FirstOrDefault()?.OldestRecordCreationTimeUtc;
        }

        /// <summary>
        /// Lists the partitioned tables in the managed schema, together with the column each is partitioned on.
        /// The predicates below are the ones the two procedures apply to decide what they manage, and must stay
        /// in agreement with them.
        /// </summary>
        async Task<IEnumerable<DigPartitionedTable>> GetPartitionedTablesAsync(CancellationTokenSource cancellationTokenSource)
        {
            var sql = adapterContext.ProviderType switch
            {
                // RANGE-partitioned on exactly one timestamp column, with a DEFAULT child and no child whose
                // name falls outside the period naming convention.
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT p.relname AS ""TableName"", a.attname AS ""KeyColumnName""
                       FROM pg_catalog.pg_partitioned_table pt
                       JOIN pg_catalog.pg_class p     ON p.oid = pt.partrelid
                       JOIN pg_catalog.pg_namespace n ON n.oid = p.relnamespace AND n.nspname = '{ManagedSchemaName}'
                       JOIN pg_catalog.pg_attribute a ON a.attrelid = pt.partrelid AND a.attnum = pt.partattrs[0]
                       WHERE pt.partstrat = 'r'
                         AND pt.partnatts = 1
                         AND a.atttypid = 'timestamp without time zone'::regtype
                         AND EXISTS (
                             SELECT 1
                             FROM pg_catalog.pg_inherits i
                             JOIN pg_catalog.pg_class dc ON dc.oid = i.inhrelid
                             WHERE i.inhparent = p.oid
                               AND dc.relname = p.relname || '_default'
                               AND pg_catalog.pg_get_expr(dc.relpartbound, dc.oid) = 'DEFAULT')
                         AND NOT EXISTS (
                             SELECT 1
                             FROM pg_catalog.pg_inherits i
                             JOIN pg_catalog.pg_class nc ON nc.oid = i.inhrelid
                             WHERE i.inhparent = p.oid
                               AND pg_catalog.pg_get_expr(nc.relpartbound, nc.oid) <> 'DEFAULT'
                               AND nc.relname !~ ('^' || p.relname || '_[0-9]{{6}}([0-9]{{2}})?$'))
                       ORDER BY p.relname;",
                // Placed on the DIG partition scheme, which is what the SQL Server procedures key on.
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $@"SELECT t.name AS [TableName], c.name AS [KeyColumnName]
                       FROM sys.tables t
                       JOIN sys.schemas s            ON s.schema_id = t.schema_id
                       JOIN sys.indexes i            ON i.object_id = t.object_id AND i.index_id IN (0, 1)
                       JOIN sys.partition_schemes ps ON ps.data_space_id = i.data_space_id
                       JOIN sys.index_columns ic     ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.partition_ordinal = 1
                       JOIN sys.columns c            ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                       WHERE s.name = '{ManagedSchemaName}'
                         AND ps.name = '{MSSQLPartitionSchemeName}'
                       ORDER BY t.name;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };

            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DigPartitionedTable>(adapterContext);
                return await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);
            }, new Context());
        }

        /// <summary>
        /// Executes the partition management procedure, creating whatever period partitions are missing.
        /// </summary>
        async Task ManagePartitionsAsync(DateTime minDateTimeUtc, CancellationTokenSource cancellationTokenSource)
        {
            var procedureName = digAdapterDatabaseObjectNames.SpManageDigPartitionsName;
            var sql = adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT {SqlMapperExtensions.FormatTableNameForSql(procedureName, "npgsqlconnection")}(@MinDateTimeUtc::timestamp without time zone, @PartitionInterval::text);",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $"EXEC {SqlMapperExtensions.FormatTableNameForSql(procedureName, "sqlconnection")} @MinDateTimeUtc = @MinDateTimeUtc, @PartitionInterval = @PartitionInterval;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };

            var parameters = new[]
            {
                new
                {
                    MinDateTimeUtc = AsTimestampWithoutTimeZone(minDateTimeUtc),
                    PartitionInterval = adapterConfiguration.PartitionInterval
                }
            };

            logger.Info($"Starting partition maintenance. [MinDateTimeUtc: {minDateTimeUtc:yyyy-MM-dd}; PartitionInterval: {adapterConfiguration.PartitionInterval}]");

            var startTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DbGdaDigPartitionInfo>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
                await repo.ExecuteAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
            }, new Context());

            logger.Info($"Partition maintenance completed successfully. Duration: {DateTime.UtcNow - startTimeUtc}");
        }

        /// <summary>
        /// Executes the partition retirement procedure, removing the partitions whose whole period precedes the
        /// retention cutoff. THIS DELETES DATA - see the warning issued when the configuration is read.
        /// </summary>
        async Task RetirePartitionsAsync(CancellationTokenSource cancellationTokenSource)
        {
            // Truncated to a whole day, because partition boundaries fall on day, week or month boundaries and a
            // mid-day cutoff could never make a difference to which of them are retirable.
            var olderThanUtc = DateTime.UtcNow.Date.AddDays(-adapterConfiguration.FailTableRetentionDays);

            var procedureName = digAdapterDatabaseObjectNames.SpRetireDigPartitionsName;
            var sql = adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT * FROM {SqlMapperExtensions.FormatTableNameForSql(procedureName, "npgsqlconnection")}(@OlderThan::timestamp without time zone, @Execute::boolean);",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $"EXEC {SqlMapperExtensions.FormatTableNameForSql(procedureName, "sqlconnection")} @OlderThan = @OlderThan, @Execute = @Execute;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };

            var parameters = new
            {
                OlderThan = AsTimestampWithoutTimeZone(olderThanUtc),
                Execute = true
            };

            logger.Info($"Starting partition retirement. Failure records created before {olderThanUtc:yyyy-MM-dd} UTC will be removed: partitions whose whole period precedes that date are reclaimed, and on PostgreSQL the catch-all partition is emptied as well when every record in it also predates that date. [FailTableRetentionDays: {adapterConfiguration.FailTableRetentionDays}]");

            var startTimeUtc = DateTime.UtcNow;
            var reportRows = await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DigPartitionRetirementResult>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
                return await repo.QueryAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
            }, new Context());

            // The procedure's report is the only record of what was removed, so it is logged in full rather
            // than summarised. It also carries the rows the procedure declined to retire, and the reason.
            foreach (var reportRow in reportRows)
            {
                logger.Info($"> [{reportRow.Object}] {reportRow.Action}: {reportRow.Detail}");
            }

            logger.Info($"Partition retirement completed successfully. Duration: {DateTime.UtcNow - startTimeUtc}");
        }

        /// <summary>
        /// Removes the invalid records created before the retention cutoff, a batch at a time. THIS DELETES
        /// DATA - see the warning issued when the configuration is read.
        /// <para>
        /// DIGInvalidRecords is the one table in the retention scope that is not partitioned, and cannot be:
        /// its UNIQUE index on GeotabGUID alone is the duplicate-detection guarantee the
        /// InvalidRecordRetrievalService depends on, and PostgreSQL requires the partition key to appear in
        /// every unique index on a partitioned table. Partitioning it would trade that guarantee away. The
        /// exception is a safe one, because the adapter sets that table's insert rate itself by polling the
        /// Data Intake Gateway, rather than being exposed to whatever an upstream integration sends.
        /// </para>
        /// <para>
        /// Note the difference from partition retirement, which can only remove a period once the whole of it
        /// precedes the cutoff: here the cutoff is applied to each record exactly.
        /// </para>
        /// </summary>
        async Task DeleteExpiredInvalidRecordsAsync(CancellationTokenSource cancellationTokenSource)
        {
            // Truncated to a whole day so that the window means the same thing here as it does for the
            // partitioned tables, whose boundaries fall on day, week or month boundaries.
            var olderThanUtc = DateTime.UtcNow.Date.AddDays(-adapterConfiguration.FailTableRetentionDays);

            var tableName = digAdapterDatabaseObjectNames.DbGdaDIGInvalidRecordsTableName;
            var sql = adapterContext.ProviderType switch
            {
                // PostgreSQL has no DELETE ... LIMIT, so the batch is chosen by a scan of the retention index
                // and deleted by primary key.
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"WITH expired AS (
                           SELECT ""id""
                           FROM {SqlMapperExtensions.FormatTableNameForSql(tableName, "npgsqlconnection")}
                           WHERE ""RecordCreationTimeUtc"" < @OlderThan
                           ORDER BY ""RecordCreationTimeUtc""
                           LIMIT @BatchSize
                       ), deleted AS (
                           DELETE FROM {SqlMapperExtensions.FormatTableNameForSql(tableName, "npgsqlconnection")} target
                           USING expired
                           WHERE target.""id"" = expired.""id""
                           RETURNING target.""id""
                       )
                       SELECT COUNT(*)::integer AS ""DeletedRecordCount"" FROM deleted;",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $@"SET NOCOUNT ON;
                       DELETE TOP (@BatchSize) FROM {SqlMapperExtensions.FormatTableNameForSql(tableName, "sqlconnection")}
                       WHERE [RecordCreationTimeUtc] < @OlderThan;
                       SELECT @@ROWCOUNT AS [DeletedRecordCount];",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };

            var parameters = new
            {
                OlderThan = AsTimestampWithoutTimeZone(olderThanUtc),
                BatchSize = InvalidRecordDeletionBatchSize
            };

            logger.Info($"Starting invalid record retention. Invalid records created before {olderThanUtc:yyyy-MM-dd} UTC will be deleted, {InvalidRecordDeletionBatchSize} at a time. [FailTableRetentionDays: {adapterConfiguration.FailTableRetentionDays}]");

            var sweepStopwatch = Stopwatch.StartNew();
            var deadline = TimeSpan.FromMinutes(MaxInvalidRecordDeletionMinutes);
            var totalDeletedRecordCount = 0L;
            var stoppedEarly = false;

            while (true)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                // Each batch is its own statement on its own connection, so the locks it takes are released
                // before the next one starts. That is what lets this run alongside the other services.
                var batchResults = await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
                {
                    var repo = new BaseRepository<DigInvalidRecordDeletionResult>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
                    return await repo.QueryAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
                }, new Context());

                var deletedRecordCount = batchResults.FirstOrDefault()?.DeletedRecordCount ?? 0;
                totalDeletedRecordCount += deletedRecordCount;

                // A short batch means the table holds nothing else old enough to remove.
                if (deletedRecordCount < InvalidRecordDeletionBatchSize)
                {
                    break;
                }

                if (sweepStopwatch.Elapsed >= deadline)
                {
                    stoppedEarly = true;
                    break;
                }
            }

            sweepStopwatch.Stop();

            if (stoppedEarly == true)
            {
                logger.Warn($"Invalid record retention stopped after {MaxInvalidRecordDeletionMinutes} minutes with {totalDeletedRecordCount} record(s) deleted, and records older than the retention window still remain. The remainder will be deleted on subsequent iterations. This is expected on the first run against a database that accumulated invalid records before retention was enabled.");
                return;
            }

            if (totalDeletedRecordCount == 0)
            {
                logger.Debug($"Invalid record retention completed. No invalid record was old enough to delete. Duration: {sweepStopwatch.Elapsed}");
                return;
            }

            logger.Info($"Invalid record retention completed successfully. {totalDeletedRecordCount} record(s) permanently deleted. Duration: {sweepStopwatch.Elapsed}");
        }

        /// <summary>
        /// Returns <paramref name="value"/> with its <see cref="DateTimeKind"/> cleared, for use as a database
        /// parameter.
        /// <para>
        /// Npgsql sends a <see cref="DateTime"/> whose Kind is <see cref="DateTimeKind.Utc"/> as
        /// "timestamp with time zone", and every column and procedure parameter it is compared against here is
        /// "timestamp without time zone". Converting between the two is done in the SESSION's time zone, so on
        /// a server whose TimeZone is anything other than UTC the value would arrive shifted by that offset -
        /// silently, and only ever by a few hours, which is exactly the size of error that survives testing and
        /// then moves a cutoff across a partition boundary. Clearing the Kind makes Npgsql send
        /// "timestamp without time zone" to begin with, leaving nothing to convert.
        /// </para>
        /// <para>
        /// SQL Server's datetime2 carries no offset and its client ignores the Kind, so this changes nothing
        /// there.
        /// </para>
        /// </summary>
        static DateTime AsTimestampWithoutTimeZone(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Runs one maintenance task and records it in DigMaintenanceLogs: a row with Success set to true when it
        /// completes, or false when it throws - after which the exception is rethrown, so that a failed partition
        /// run still stops retention from running after it, exactly as it did before this log existed.
        /// </summary>
        async Task RunAndLogAsync(DigMaintenanceType maintenanceType, Func<Task> work, CancellationTokenSource cancellationTokenSource)
        {
            var startTimeUtc = DateTime.UtcNow;
            try
            {
                await work();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await TryPersistMaintenanceLogAsync(maintenanceType, startTimeUtc, false, cancellationTokenSource);
                throw;
            }
            await PersistMaintenanceLogAsync(maintenanceType, startTimeUtc, true, cancellationTokenSource);
        }

        /// <summary>
        /// A maintenance task that has run and has not yet been recorded in DigMaintenanceLogs.
        /// </summary>
        /// <param name="MaintenanceType">The kind of maintenance.</param>
        /// <param name="StartTimeUtc">When the task started.</param>
        /// <param name="EndTimeUtc">When the task ended - not when it is recorded, which may be much later.</param>
        /// <param name="Failure">The exception the task ended with, or <c>null</c> if it completed.</param>
        sealed record MaintenanceRunOutcome(DigMaintenanceType MaintenanceType, DateTime StartTimeUtc, DateTime EndTimeUtc, ExceptionDispatchInfo? Failure);

        /// <summary>
        /// Runs one maintenance task and returns its outcome WITHOUT recording it, for a task that runs inside the
        /// maintenance pause. <see cref="RecordMaintenanceRunsAsync"/> records it once the pause has been released.
        /// </summary>
        static async Task<MaintenanceRunOutcome> RunMaintenanceTaskAsync(DigMaintenanceType maintenanceType, Func<Task> work)
        {
            var startTimeUtc = DateTime.UtcNow;
            try
            {
                await work();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new MaintenanceRunOutcome(maintenanceType, startTimeUtc, DateTime.UtcNow, ExceptionDispatchInfo.Capture(ex));
            }
            return new MaintenanceRunOutcome(maintenanceType, startTimeUtc, DateTime.UtcNow, null);
        }

        /// <summary>
        /// Records runs returned by <see cref="RunMaintenanceTaskAsync"/>, with the times they actually ran. A failed
        /// run is recorded as <see cref="RunAndLogAsync"/> records one: logging rather than throwing if the write
        /// fails, so that the failure being recorded is the one reported.
        /// </summary>
        async Task RecordMaintenanceRunsAsync(IEnumerable<MaintenanceRunOutcome> runs, CancellationTokenSource cancellationTokenSource)
        {
            foreach (var run in runs)
            {
                if (run.Failure != null)
                {
                    await TryPersistMaintenanceLogAsync(run.MaintenanceType, run.StartTimeUtc, false, cancellationTokenSource, run.EndTimeUtc);
                }
                else
                {
                    await PersistMaintenanceLogAsync(run.MaintenanceType, run.StartTimeUtc, true, cancellationTokenSource, run.EndTimeUtc);
                }
            }
        }

        /// <summary>
        /// Reads the latest <see cref="DbGdaDigMaintenanceLog"/> row of one maintenance type, or <c>null</c> if that
        /// kind of maintenance has never run.
        /// </summary>
        async Task<DbGdaDigMaintenanceLog?> GetLatestMaintenanceLogAsync(DigMaintenanceType maintenanceType, CancellationTokenSource cancellationTokenSource)
        {
            var tableName = digAdapterDatabaseObjectNames.DbGdaDigMaintenanceLogsTableName;
            var sql = adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT ""id"", ""MaintenanceTypeId"", ""StartTimeUtc"", ""EndTimeUtc"", ""Success"", ""RecordLastChangedUtc""
                       FROM {SqlMapperExtensions.FormatTableNameForSql(tableName, "npgsqlconnection")}
                       WHERE ""MaintenanceTypeId"" = @MaintenanceTypeId
                       ORDER BY ""StartTimeUtc"" DESC
                       LIMIT 1;",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $@"SELECT TOP 1 [id], [MaintenanceTypeId], [StartTimeUtc], [EndTimeUtc], [Success], [RecordLastChangedUtc]
                       FROM {SqlMapperExtensions.FormatTableNameForSql(tableName, "sqlconnection")}
                       WHERE [MaintenanceTypeId] = @MaintenanceTypeId
                       ORDER BY [StartTimeUtc] DESC;",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };
            var parameters = new { MaintenanceTypeId = (short)maintenanceType.Id };

            var results = await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DbGdaDigMaintenanceLog>(adapterContext);
                return await repo.QueryAsync(sql, parameters, cancellationTokenSource, true, adapterContext);
            }, new Context());
            return results.FirstOrDefault();
        }

        /// <summary>
        /// Writes a <see cref="DbGdaDigMaintenanceLog"/> row for a run that started at <paramref name="startTimeUtc"/>
        /// and has just ended.
        /// </summary>
        /// <param name="endTimeUtc">When the run ended, for a run recorded some time after it ended; now if omitted.</param>
        /// <returns>The end time recorded in the row, so that a caller logging the duration of the run reports the same
        /// duration the row holds rather than one that also includes writing the row.</returns>
        async Task<DateTime> PersistMaintenanceLogAsync(DigMaintenanceType maintenanceType, DateTime startTimeUtc, bool success, CancellationTokenSource cancellationTokenSource, DateTime? endTimeUtc = null)
        {
            var recordedEndTimeUtc = endTimeUtc ?? DateTime.UtcNow;
            var dbGdaDigMaintenanceLog = new DbGdaDigMaintenanceLog
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                MaintenanceTypeId = (short)maintenanceType.Id,
                StartTimeUtc = startTimeUtc,
                EndTimeUtc = recordedEndTimeUtc,
                Success = success,
                RecordLastChangedUtc = recordedEndTimeUtc
            };

            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                // A token source of this attempt's own, linked to the iteration's, as every other service's write uses.
                // The persister cancels the source it is given when a write fails, before rethrowing, so an attempt that
                // failed on the iteration's own source left it cancelled even after a retry succeeded - and the next
                // statement of the iteration then threw OperationCanceledException, which this service treats as
                // shutdown: the adapter stopped, with exit code 0.
                using var attemptCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);
                try
                {
                    await dbGdaDigMaintenanceLogPersister.PersistEntitiesToDatabaseAsync(adapterContext, new List<DbGdaDigMaintenanceLog> { dbGdaDigMaintenanceLog }, attemptCancellationTokenSource, MyGeotabAPIAdapter.Logging.LogLevel.Debug);
                    await adapterUOW.CommitAsync();
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    await adapterUOW.RollBackAsync();
                    throw;
                }
            }, new Context());

            return recordedEndTimeUtc;
        }

        /// <summary>
        /// Writes a <see cref="DbGdaDigMaintenanceLog"/> row, logging rather than throwing if that fails. For recording a
        /// failure: the failure being recorded is the one worth reporting, and an error writing the record of it must
        /// not replace it.
        /// </summary>
        async Task TryPersistMaintenanceLogAsync(DigMaintenanceType maintenanceType, DateTime startTimeUtc, bool success, CancellationTokenSource cancellationTokenSource, DateTime? endTimeUtc = null)
        {
            try
            {
                await PersistMaintenanceLogAsync(maintenanceType, startTimeUtc, success, cancellationTokenSource, endTimeUtc);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Warn, $"{CurrentClassName} could not record a {maintenanceType} maintenance run in {digAdapterDatabaseObjectNames.DbGdaDigMaintenanceLogsTableName}");
            }
        }

        /// <summary>
        /// Performs Level 1 database maintenance if it is enabled and its interval has elapsed.
        /// <para>
        /// A failure is logged, recorded, and does NOT stop the iteration. Level 1 is independent of everything that
        /// follows it, and a statistics update that could not complete is no reason to leave the partitions uncreated.
        /// </para>
        /// </summary>
        async Task ExecuteLevel1DatabaseMaintenanceIfDueAsync(CancellationTokenSource cancellationTokenSource)
        {
            if (adapterConfiguration.EnableLevel1DigMaintenance == false)
            {
                return;
            }

            var latestLog = await GetLatestMaintenanceLogAsync(DigMaintenanceType.Level1, cancellationTokenSource);
            if (DigMaintenancePolicy.IsDue(latestLog, adapterConfiguration.Level1DigMaintenanceIntervalMinutes, DateTime.UtcNow) == false)
            {
                return;
            }

            logger.Info($"Starting Level 1 database maintenance.");
            var startTimeUtc = DateTime.UtcNow;
            string summary;
            try
            {
                summary = adapterContext.ProviderType switch
                {
                    ConnectionInfo.DataAccessProviderType.PostgreSQL => await ExecuteLevel1DatabaseMaintenance_PG_Async(cancellationTokenSource),
                    ConnectionInfo.DataAccessProviderType.SQLServer => await ExecuteLevel1DatabaseMaintenance_MSSQL_Async(cancellationTokenSource),
                    _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ex is not AdapterDatabaseConnectionException)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"{CurrentClassName} Level 1 database maintenance failed. The rest of this iteration continues, and Level 1 will be attempted again in {adapterConfiguration.Level1DigMaintenanceIntervalMinutes} minute(s)");
                await TryPersistMaintenanceLogAsync(DigMaintenanceType.Level1, startTimeUtc, false, cancellationTokenSource);
                return;
            }

            var endTimeUtc = await PersistMaintenanceLogAsync(DigMaintenanceType.Level1, startTimeUtc, true, cancellationTokenSource);
            logger.Info($"Level 1 database maintenance completed successfully: {summary}. Duration: {endTimeUtc - startTimeUtc}");
        }

        /// <summary>
        /// Level 1 on PostgreSQL: VACUUM and ANALYZE, or ANALYZE alone, the gda tables that need it.
        /// </summary>
        /// <returns>A summary of what was done, for the completion log line.</returns>
        async Task<string> ExecuteLevel1DatabaseMaintenance_PG_Async(CancellationTokenSource cancellationTokenSource)
        {
            var candidates = await ReadMaintenanceViewAsync<DbGdavwStatForLevel1DigMaintenance_PG>(digAdapterDatabaseObjectNames.VwStatsForLevel1DigMaintenanceName, cancellationTokenSource);
            if (candidates.Count == 0)
            {
                logger.Info($"No tables found that need Level 1 maintenance.");
                return "no table needed it";
            }

            logger.Info($"Found {candidates.Count} table(s) that may need Level 1 maintenance.");
            logger.Info($"> Tables are vacuumed and analyzed where the ratio of dead tuples to live tuples exceeds {DigMaintenanceThresholds.PG_VacuumThreshold_DeadTupleRatio} or the number of dead tuples exceeds {DigMaintenanceThresholds.PG_VacuumThreshold_DeadTuplesCount}, and analyzed where the ratio of modifications since the last analyze to live tuples exceeds {DigMaintenanceThresholds.PG_AnalyzeThreshold_ModsSinceLastAnalyzeRatio}.");

            int vacuumed = 0, analyzed = 0;
            foreach (var candidate in candidates)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                var action = DigMaintenancePolicy.DecideLevel1(candidate);
                if (action == DigLevel1Action.None || IsMaintainableObject(candidate.SchemaName, candidate.TableName) == false)
                {
                    continue;
                }

                if (action == DigLevel1Action.VacuumAndAnalyze)
                {
                    logger.Info($"Vacuuming & analyzing table {candidate.SchemaName}.{candidate.TableName} [dead tuple ratio: {candidate.PctDeadTuples?.ToString() ?? "n/a"}; dead tuples: {candidate.DeadTuples}].");
                    var duration = await ExecuteMaintenanceStatementAsync($"VACUUM (ANALYZE) \"{candidate.SchemaName}\".\"{candidate.TableName}\";", cancellationTokenSource);
                    logger.Info($"Vacuumed and analyzed table {candidate.SchemaName}.{candidate.TableName} in {duration.TotalSeconds} seconds.");
                    vacuumed++;
                }
                else
                {
                    logger.Info($"Analyzing table {candidate.SchemaName}.{candidate.TableName} [modification ratio since last analyze: {candidate.PctModsSinceLastAnalyze?.ToString() ?? "n/a"}].");
                    var duration = await ExecuteMaintenanceStatementAsync($"ANALYZE \"{candidate.SchemaName}\".\"{candidate.TableName}\";", cancellationTokenSource);
                    logger.Info($"Analyzed table {candidate.SchemaName}.{candidate.TableName} in {duration.TotalSeconds} seconds.");
                    analyzed++;
                }
            }
            return $"{vacuumed} table(s) vacuumed and analyzed and {analyzed} analyzed, of {candidates.Count} candidate(s)";
        }

        /// <summary>
        /// Level 1 on SQL Server: UPDATE STATISTICS on the gda tables that need it.
        /// </summary>
        /// <returns>A summary of what was done, for the completion log line.</returns>
        async Task<string> ExecuteLevel1DatabaseMaintenance_MSSQL_Async(CancellationTokenSource cancellationTokenSource)
        {
            var candidates = await ReadMaintenanceViewAsync<DbGdavwStatForLevel1DigMaintenance_MSSQL>(digAdapterDatabaseObjectNames.VwStatsForLevel1DigMaintenanceName, cancellationTokenSource);
            if (candidates.Count == 0)
            {
                logger.Info($"No tables found that need Level 1 maintenance.");
                return "no table needed it";
            }

            logger.Info($"Found {candidates.Count} table(s) that may need Level 1 maintenance.");
            logger.Info($"> Statistics are updated on tables with more than {DigMaintenanceThresholds.MSSQL_UpdateStatisticsThreshold_ModsSinceLastStatsUpdateCount} modifications since their last statistics update, where those modifications exceed {DigMaintenanceThresholds.MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate}% of the table's rows.");

            int updated = 0;
            foreach (var candidate in candidates)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                if (DigMaintenancePolicy.DecideLevel1(candidate) == DigLevel1Action.None || IsMaintainableObject(candidate.SchemaName, candidate.TableName) == false)
                {
                    continue;
                }

                logger.Info($"Updating statistics for table {candidate.SchemaName}.{candidate.TableName} [modifications since last statistics update: {candidate.PctModsSinceLastStatsUpdate:0.##}% of {candidate.RecordCount:N0} row(s)].");
                var duration = await ExecuteMaintenanceStatementAsync($"UPDATE STATISTICS [{candidate.SchemaName}].[{candidate.TableName}];", cancellationTokenSource);
                logger.Info($"Updated statistics on table {candidate.SchemaName}.{candidate.TableName} in {duration.TotalSeconds} seconds.");
                updated++;
            }
            return $"statistics updated on {updated} table(s), of {candidates.Count} candidate(s)";
        }

        /// <summary>
        /// Runs Level 2 maintenance, if it is due. It is the last thing an iteration does, and starts with the other services
        /// running: it decides whether it is due - with the time now, because everything before it has taken its time, and a
        /// window decided against a stale time could let a run and its pause begin after the window had closed - and finds the
        /// indexes to maintain. It then pauses the other services only for its offline operations, and runs its online
        /// operations after that pause is released.
        /// </summary>
        /// <returns><c>false</c> if the other services did not pause for its offline operations; otherwise <c>true</c>.</returns>
        async Task<bool> ExecuteLevel2DatabaseMaintenanceIfDueAsync(CancellationTokenSource cancellationTokenSource)
        {
            var level2Run = await PlanLevel2DatabaseMaintenanceAsync(DateTime.UtcNow, cancellationTokenSource);
            if (level2Run.IsDue == false)
            {
                return true;
            }

            var batch = await PrepareLevel2DatabaseMaintenanceAsync(level2Run.StopTimeUtc, cancellationTokenSource);
            if (batch == null)
            {
                return true;
            }

            var offlineOperations = batch.Operations.Where(operation => operation.RequiresPause == true).ToList();
            var onlineOperations = batch.Operations.Where(operation => operation.RequiresPause == false).ToList();

            // The window is judged again here, when the pause is about to be taken, and not only when the run was planned:
            // finding the indexes to maintain can take long enough for the window to close in between, and a pause taken
            // then would hold the other services still for nothing, since the window stops each operation before it
            // starts. FinishLevel2DatabaseMaintenanceAsync reports a run that maintained nothing because of it.
            if (offlineOperations.Count > 0 && batch.StopTimeUtc != null && DateTime.UtcNow > batch.StopTimeUtc.Value)
            {
                batch.StoppedByWindow = true;
            }

            if (offlineOperations.Count > 0 && batch.StoppedByWindow == false)
            {
                var otherServicesPaused = false;
                try
                {
                    otherServicesPaused = await PauseOtherServicesForDatabaseMaintenanceAsync($"This Level 2 run is recorded as failed, and will be attempted again in {adapterConfiguration.Level2DigMaintenanceIntervalMinutes} minute(s).", cancellationTokenSource);
                    if (otherServicesPaused == true)
                    {
                        await RunLevel2OperationsAsync(batch, offlineOperations, "offline index operation(s), with the other services paused", cancellationTokenSource);
                    }
                }
                finally
                {
                    // Let the other services resume, whether or not the maintenance succeeded.
                    stateMachine.SetStateReason(StateReason.AdapterDatabaseMaintenance, false);
                }

                if (otherServicesPaused == false)
                {
                    // Recorded as a failed run, so that it is spaced out like any other failure instead of being attempted
                    // again - holding the services that did pause for up to five minutes each time - on every wake. Written
                    // only once the pause has been released, like every other maintenance row.
                    await TryPersistMaintenanceLogAsync(DigMaintenanceType.Level2, batch.StartTimeUtc, false, cancellationTokenSource);
                    return false;
                }
            }

            // After the pause is released: an online operation blocks no writer, so nothing needs holding still - and a
            // concurrent rebuild that waits for a long transaction delays only itself.
            await RunLevel2OperationsAsync(batch, onlineOperations, "online index operation(s), with the other services running", cancellationTokenSource);
            await FinishLevel2DatabaseMaintenanceAsync(batch, cancellationTokenSource);
            return true;
        }

        /// <summary>
        /// Whether a Level 2 run is due now and, if it is, when it must stop starting new index operations.
        /// </summary>
        /// <param name="IsDue">Whether Level 2 should run in this iteration.</param>
        /// <param name="StopTimeUtc">The end of the maintenance window, or <c>null</c> when the window is disabled.</param>
        readonly record struct Level2Run(bool IsDue, DateTime? StopTimeUtc);

        /// <summary>
        /// Decides whether Level 2 maintenance should run in this iteration - it is enabled, its interval has elapsed
        /// and, if the window is enabled, now is within it - and on PostgreSQL that pgstattuple can be used.
        /// <para>
        /// Decided with the other services running. Where pgstattuple cannot be used the run is recorded as skipped
        /// without pausing anything: holding telemetry still to discover that Level 2 cannot run would be a pause
        /// for nothing, repeated every interval. The skip is recorded with Success set to false, so that it is
        /// retried after the interval rather than on every wake.
        /// </para>
        /// </summary>
        async Task<Level2Run> PlanLevel2DatabaseMaintenanceAsync(DateTime nowUtc, CancellationTokenSource cancellationTokenSource)
        {
            if (adapterConfiguration.EnableLevel2DigMaintenance == false)
            {
                return new Level2Run(false, null);
            }

            var latestLog = await GetLatestMaintenanceLogAsync(DigMaintenanceType.Level2, cancellationTokenSource);
            if (DigMaintenancePolicy.IsDue(latestLog, adapterConfiguration.Level2DigMaintenanceIntervalMinutes, nowUtc) == false)
            {
                return new Level2Run(false, null);
            }

            var (mayStart, stopTimeUtc) = DigMaintenancePolicy.EvaluateLevel2Window(adapterConfiguration.EnableLevel2DigMaintenanceWindow, adapterConfiguration.Level2DigMaintenanceWindowStartTimeUtc, adapterConfiguration.Level2DigMaintenanceWindowMaxMinutes, nowUtc);
            if (mayStart == false)
            {
                logger.Debug($"Level 2 database maintenance is due, but waits for its daily window, which starts at {adapterConfiguration.Level2DigMaintenanceWindowStartTimeUtc.TimeOfDay:hh\\:mm} UTC.");
                return new Level2Run(false, null);
            }

            if (adapterContext.ProviderType == ConnectionInfo.DataAccessProviderType.PostgreSQL)
            {
                var probe = await ProbePgstattupleAsync(cancellationTokenSource);
                if (probe.IsAvailable == false)
                {
                    logger.Warn($"Level 2 database maintenance is due but CANNOT RUN on this PostgreSQL database, because {DigMaintenancePolicy.DescribePgstattupleProblem(probe.ExistsInPublic, probe.ExtensionSchema, PostgreSQLClientRoleName)} It has been skipped, and will be attempted again in {adapterConfiguration.Level2DigMaintenanceIntervalMinutes} minute(s). Nothing else is affected.");
                    await PersistMaintenanceLogAsync(DigMaintenanceType.Level2, nowUtc, false, cancellationTokenSource);
                    return new Level2Run(false, null);
                }
            }

            return new Level2Run(true, stopTimeUtc);
        }

        /// <summary>
        /// Whether public.pgstattuple() exists and may be executed by the role this adapter connects as - and, when it may
        /// not, whether the function exists in public and which schema the extension is installed in, so that the warning
        /// can name the right remedy.
        /// <para>
        /// The object is resolved to an oid FIRST, and the privilege checked on that. has_function_privilege given a
        /// text signature RAISES an error when the function does not exist - exactly the case this check exists to
        /// detect - whereas given the NULL that to_regprocedure returns for a missing function it returns NULL, which
        /// COALESCE turns into false. One expression covers both halves: the extension absent, and the grant missing.
        /// </para>
        /// </summary>
        async Task<DigPgstattupleProbeResult> ProbePgstattupleAsync(CancellationTokenSource cancellationTokenSource)
        {
            const string sql = @"SELECT COALESCE(has_function_privilege(to_regprocedure('public.pgstattuple(regclass)'), 'EXECUTE'), false) AS ""IsAvailable"",
    to_regprocedure('public.pgstattuple(regclass)') IS NOT NULL AS ""ExistsInPublic"",
    (SELECT n.nspname FROM pg_catalog.pg_extension e JOIN pg_catalog.pg_namespace n ON n.oid = e.extnamespace WHERE e.extname = 'pgstattuple') AS ""ExtensionSchema"";";
            var results = await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DigPgstattupleProbeResult>(adapterContext);
                return await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);
            }, new Context());
            return results.FirstOrDefault() ?? new DigPgstattupleProbeResult();
        }

        /// <summary>
        /// One index operation a Level 2 run has decided on: its statement, what to log around it, and whether it needs the
        /// other services paused (see <see cref="DigMaintenancePolicy.RequiresPause"/>).
        /// </summary>
        sealed class Level2Operation
        {
            public string Statement { get; init; } = string.Empty;
            public string StartMessage { get; init; } = string.Empty;
            public Func<TimeSpan, string> DoneMessage { get; init; } = duration => string.Empty;
            public bool RequiresPause { get; init; }
            public string TallyKey { get; init; } = string.Empty;
        }

        /// <summary>
        /// A Level 2 run: planned before any pause is taken, its offline operations run inside the pause and its online
        /// operations after it, and then recorded once.
        /// </summary>
        sealed class Level2Batch
        {
            public DateTime StartTimeUtc { get; init; }
            public DateTime? StopTimeUtc { get; init; }
            public int CandidateCount { get; set; }
            public List<Level2Operation> Operations { get; } = new List<Level2Operation>();
            public Dictionary<string, int> Tally { get; } = new Dictionary<string, int>();
            public bool Failed { get; set; }
            public bool StoppedByWindow { get; set; }
            public int Maintained => Tally.Values.Sum();
        }

        /// <summary>
        /// Starts a Level 2 run and decides what it will do, WITHOUT pausing the other services: INVALID leftovers of
        /// interrupted concurrent rebuilds are dropped (PostgreSQL; online), the Level 2 view is read, and each candidate's
        /// operation is planned. Only the offline operations - a plain REINDEX, a REBUILD - are then run inside a pause; the
        /// online ones - REINDEX CONCURRENTLY, REORGANIZE - run after it is released, so that a concurrent rebuild waiting
        /// for a long transaction does not hold telemetry still.
        /// <para>
        /// Returns <c>null</c> when there is nothing more to do: the run failed (recorded), or no index needs an operation
        /// (recorded as a success, having taken no pause at all). A failure is logged and recorded and does NOT stop the
        /// iteration.
        /// </para>
        /// </summary>
        /// <param name="stopTimeUtc">When to stop starting new index operations: the end of the maintenance window, or
        /// <c>null</c> when the window is disabled and every candidate is to be processed.</param>
        async Task<Level2Batch?> PrepareLevel2DatabaseMaintenanceAsync(DateTime? stopTimeUtc, CancellationTokenSource cancellationTokenSource)
        {
            var windowDescription = stopTimeUtc != null
                ? $" No new index operation will be started after {stopTimeUtc.Value:yyyy-MM-dd HH:mm} UTC, the end of the maintenance window."
                : string.Empty;
            logger.Info($"Starting Level 2 database maintenance. The indexes to maintain are found before any pause, and the other services are paused only for offline operations.{windowDescription}");

            var batch = new Level2Batch { StartTimeUtc = DateTime.UtcNow, StopTimeUtc = stopTimeUtc };
            try
            {
                switch (adapterContext.ProviderType)
                {
                    case ConnectionInfo.DataAccessProviderType.PostgreSQL:
                        await DropInvalidConcurrentRebuildLeftoversAsync(stopTimeUtc, cancellationTokenSource);
                        await PlanLevel2Operations_PG_Async(batch, cancellationTokenSource);
                        break;
                    case ConnectionInfo.DataAccessProviderType.SQLServer:
                        await PlanLevel2Operations_MSSQL_Async(batch, cancellationTokenSource);
                        break;
                    default:
                        throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ex is not AdapterDatabaseConnectionException)
            {
                LogLevel2Failure(ex);
                await TryPersistMaintenanceLogAsync(DigMaintenanceType.Level2, batch.StartTimeUtc, false, cancellationTokenSource);
                return null;
            }

            if (batch.Operations.Count == 0)
            {
                // Nothing to maintain: the run is complete, and no pause is taken at all.
                await FinishLevel2DatabaseMaintenanceAsync(batch, cancellationTokenSource);
                return null;
            }
            return batch;
        }

        /// <summary>
        /// Runs one phase of a Level 2 run - its offline operations inside the pause, or its online ones after it - in the
        /// order they were planned. It stops at the end of the maintenance window, and at the first failure, which is
        /// logged and makes the run a failed one.
        /// </summary>
        async Task RunLevel2OperationsAsync(Level2Batch batch, List<Level2Operation> operations, string phaseDescription, CancellationTokenSource cancellationTokenSource)
        {
            if (operations.Count == 0 || batch.Failed == true || batch.StoppedByWindow == true)
            {
                return;
            }

            logger.Info($"Level 2: {operations.Count} {phaseDescription}.");
            foreach (var operation in operations)
            {
                cancellationTokenSource.Token.ThrowIfCancellationRequested();

                if (batch.StopTimeUtc != null && DateTime.UtcNow > batch.StopTimeUtc.Value)
                {
                    logger.Info($"Skipping the remaining index operations because the maintenance window ended at {batch.StopTimeUtc.Value:yyyy-MM-dd HH:mm} UTC.");
                    batch.StoppedByWindow = true;
                    return;
                }

                logger.Info(operation.StartMessage);
                try
                {
                    var duration = await ExecuteMaintenanceStatementAsync(operation.Statement, cancellationTokenSource);
                    logger.Info(operation.DoneMessage(duration));
                    batch.Tally[operation.TallyKey] = batch.Tally.GetValueOrDefault(operation.TallyKey) + 1;
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not AdapterDatabaseConnectionException)
                {
                    LogLevel2Failure(ex);
                    batch.Failed = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Records a Level 2 run once both of its phases are over: a failed row if either failed; nothing if the window
        /// closed before any index was maintained (so that it runs in the next window); otherwise a successful row.
        /// </summary>
        async Task FinishLevel2DatabaseMaintenanceAsync(Level2Batch batch, CancellationTokenSource cancellationTokenSource)
        {
            if (batch.Failed == true)
            {
                await TryPersistMaintenanceLogAsync(DigMaintenanceType.Level2, batch.StartTimeUtc, false, cancellationTokenSource);
                return;
            }

            if (batch.StoppedByWindow == true && batch.Maintained == 0)
            {
                // The window closed before a single index was maintained. That is not a successful run, and recording one
                // would put the next attempt a whole interval away, past the next window.
                logger.Warn($"Level 2 database maintenance stopped before maintaining any index, because its maintenance window ended at {batch.StopTimeUtc:yyyy-MM-dd HH:mm} UTC. Nothing has been recorded, so it will run in the next window. If this happens in every window, finding the indexes to maintain takes longer than the window: lengthen the window. Duration: {DateTime.UtcNow - batch.StartTimeUtc}");
                return;
            }

            var summary = batch.CandidateCount == 0
                ? "no index needed it"
                : adapterContext.ProviderType == ConnectionInfo.DataAccessProviderType.PostgreSQL
                    ? $"{batch.Tally.GetValueOrDefault("reindexed")} index(es) reindexed, of {batch.CandidateCount} candidate(s)"
                    : $"{batch.Tally.GetValueOrDefault("rebuilt whole")} index(es) rebuilt whole, {batch.Tally.GetValueOrDefault("partition rebuilt")} partition(s) rebuilt and {batch.Tally.GetValueOrDefault("partition reorganized")} reorganized, of {batch.CandidateCount} candidate(s)";
            var endTimeUtc = await PersistMaintenanceLogAsync(DigMaintenanceType.Level2, batch.StartTimeUtc, true, cancellationTokenSource);
            logger.Info($"Level 2 database maintenance completed successfully: {summary}. Duration: {endTimeUtc - batch.StartTimeUtc}");
        }

        void LogLevel2Failure(Exception ex)
        {
            exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"{CurrentClassName} Level 2 database maintenance failed. It will be attempted again in {adapterConfiguration.Level2DigMaintenanceIntervalMinutes} minute(s)");
        }

        /// <summary>
        /// Level 2 on PostgreSQL: plans a REINDEX of each gda index bloated enough to need it - CONCURRENTLY for a live queue's.
        /// </summary>
        async Task PlanLevel2Operations_PG_Async(Level2Batch batch, CancellationTokenSource cancellationTokenSource)
        {
            var retrievalStopwatch = Stopwatch.StartNew();
            var candidates = await ReadMaintenanceViewAsync<DbGdavwStatForLevel2DigMaintenance_PG>(digAdapterDatabaseObjectNames.VwStatsForLevel2DigMaintenanceName, cancellationTokenSource);
            retrievalStopwatch.Stop();
            batch.CandidateCount = candidates.Count;
            if (candidates.Count == 0)
            {
                logger.Info($"No indexes found that need Level 2 maintenance.");
                return;
            }

            logger.Info($"Found {candidates.Count} index(es) that may need Level 2 maintenance. Duration: {retrievalStopwatch.Elapsed}");
            logger.Info($"> Indexes are reindexed where the bloat ratio exceeds {DigMaintenanceThresholds.PG_ReindexThreshold_BloatRatio} AND the index size exceeds {DigMaintenanceThresholds.PG_ReindexThreshold_IndexSizeBytes} bytes.");

            foreach (var candidate in candidates)
            {
                var action = DigMaintenancePolicy.DecideLevel2(candidate);
                if (action == DigLevel2Action.None || IsMaintainableObject(candidate.SchemaName, candidate.IndexName) == false)
                {
                    continue;
                }

                // A live queue's index is rebuilt CONCURRENTLY, because the customer's application keeps writing the queue
                // and a plain REINDEX would block it for the whole rebuild; see DigMaintenancePolicy.DecideLevel2.
                var concurrently = action == DigLevel2Action.ReindexConcurrently;
                var how = concurrently ? " concurrently, because its table is a live queue" : string.Empty;
                var name = $"{candidate.SchemaName}.{candidate.IndexName}";
                batch.Operations.Add(new Level2Operation
                {
                    Statement = concurrently
                        ? $"REINDEX INDEX CONCURRENTLY \"{candidate.SchemaName}\".\"{candidate.IndexName}\";"
                        : $"REINDEX INDEX \"{candidate.SchemaName}\".\"{candidate.IndexName}\";",
                    StartMessage = $"Reindexing index {name}{how} [bloat ratio: {candidate.IndexBloatRatio:0.####}; index size: {candidate.IndexSizeText}].",
                    DoneMessage = duration => $"Reindexed index {name} in {duration.TotalSeconds} seconds.",
                    RequiresPause = DigMaintenancePolicy.RequiresPause(action),
                    TallyKey = "reindexed"
                });
            }
        }

        /// <summary>
        /// PostgreSQL only: drops every INVALID gda index left behind by an interrupted <c>REINDEX INDEX CONCURRENTLY</c> - one
        /// stopped by the one-hour statement limit, say. PostgreSQL names such a leftover after the index it was rebuilding,
        /// with a <c>_ccnew</c> or <c>_ccold</c> suffix; it serves no query but is still maintained on every write, so each
        /// interrupted run would otherwise leave one more behind. Dropped concurrently, so that writers are not blocked. Like every
        /// other index operation, no drop is started after the maintenance window has ended.
        /// </summary>
        async Task DropInvalidConcurrentRebuildLeftoversAsync(DateTime? stopTimeUtc, CancellationTokenSource cancellationTokenSource)
        {
            const string sql = @"SELECT c.relname AS ""IndexName""
FROM pg_catalog.pg_index i
JOIN pg_catalog.pg_class c ON c.oid = i.indexrelid
JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'gda' AND NOT i.indisvalid AND c.relname ~ '_cc(new|old)[0-9]*$';";
            var leftovers = await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DigInvalidIndex>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
                return await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);
            }, new Context());

            foreach (var leftover in leftovers)
            {
                if (stopTimeUtc != null && DateTime.UtcNow > stopTimeUtc.Value)
                {
                    logger.Info($"Leaving the remaining INVALID index(es) for the next run, because the maintenance window ended at {stopTimeUtc.Value:yyyy-MM-dd HH:mm} UTC.");
                    return;
                }
                if (IsMaintainableObject("gda", leftover.IndexName) == false)
                {
                    continue;
                }
                logger.Warn($"Dropping index gda.{leftover.IndexName}: it is an INVALID index left behind by an interrupted concurrent rebuild, which serves no query but is maintained on every write.");
                await ExecuteMaintenanceStatementAsync($"DROP INDEX CONCURRENTLY IF EXISTS \"gda\".\"{leftover.IndexName}\";", cancellationTokenSource);
            }
        }

        /// <summary>
        /// Level 2 on SQL Server: plans a REBUILD or REORGANIZE of each gda index partition fragmented enough to need it,
        /// rebuilding a whole index instead where most of its partitions are. A live queue's index is only ever reorganized:
        /// see <see cref="DigMaintenancePolicy.DecideLevel2(DbGdavwStatForLevel2DigMaintenance_MSSQL)"/>.
        /// </summary>
        async Task PlanLevel2Operations_MSSQL_Async(Level2Batch batch, CancellationTokenSource cancellationTokenSource)
        {
            var retrievalStopwatch = Stopwatch.StartNew();
            var candidates = await ReadMaintenanceViewAsync<DbGdavwStatForLevel2DigMaintenance_MSSQL>(digAdapterDatabaseObjectNames.VwStatsForLevel2DigMaintenanceName, cancellationTokenSource);
            retrievalStopwatch.Stop();
            batch.CandidateCount = candidates.Count;
            if (candidates.Count == 0)
            {
                logger.Info($"No indexes found that need Level 2 maintenance.");
                return;
            }

            logger.Info($"Found {candidates.Count} index partition(s) that may need Level 2 maintenance. Duration: {retrievalStopwatch.Elapsed}");
            logger.Info($"> Indexes are rebuilt where more than {DigMaintenanceThresholds.MSSQL_ReindexByTableThreshold_HighFragPartitionPct}% of their partitions are highly fragmented.");
            logger.Info($"> Index partitions are rebuilt where their fragmentation exceeds {DigMaintenanceThresholds.MSSQL_ReindexThreshold_FragmentationPct}%.");
            logger.Info($"> Index partitions are reorganized where their fragmentation exceeds {DigMaintenanceThresholds.MSSQL_ReorganizeThreshold_FragmentationPct}%.");

            (string TableName, string IndexName)? lastIndexRebuiltWhole = null;
            foreach (var candidate in candidates)
            {
                // A whole-index rebuild covers every partition of it, and the view lists partitions of one index
                // consecutively. Keyed on the table as well as the index, because index names are unique only
                // within a table.
                if (lastIndexRebuiltWhole == (candidate.TableName, candidate.IndexName))
                {
                    continue;
                }

                var action = DigMaintenancePolicy.DecideLevel2(candidate);
                if (action == DigLevel2Action.None || IsMaintainableObject(candidate.SchemaName, candidate.TableName) == false || IsMaintainableObject(candidate.SchemaName, candidate.IndexName) == false)
                {
                    continue;
                }

                var target = $"[{candidate.IndexName}] ON [{candidate.SchemaName}].[{candidate.TableName}]";
                var description = $"index {candidate.IndexName} on table {candidate.SchemaName}.{candidate.TableName}";
                // An index of a single partition is maintained whole; naming PARTITION = 1 on one that is not
                // partitioned is an error.
                var partitionClause = candidate.TotalPartitions > 1 ? $" PARTITION = {candidate.PartitionNumber}" : string.Empty;
                var partitionDescription = candidate.TotalPartitions > 1 ? $" partition {candidate.PartitionNumber}" : string.Empty;
                switch (action)
                {
                    case DigLevel2Action.RebuildIndex:
                        batch.Operations.Add(new Level2Operation
                        {
                            Statement = $"ALTER INDEX {target} REBUILD;",
                            StartMessage = $"Rebuilding {description} [highly-fragmented partitions: {candidate.PctHighFragPartitions:0.##}%].",
                            DoneMessage = duration => $"Rebuilt {description} in {duration.TotalSeconds} seconds.",
                            RequiresPause = DigMaintenancePolicy.RequiresPause(action),
                            TallyKey = "rebuilt whole"
                        });
                        lastIndexRebuiltWhole = (candidate.TableName, candidate.IndexName);
                        break;
                    case DigLevel2Action.RebuildPartition:
                        batch.Operations.Add(new Level2Operation
                        {
                            Statement = $"ALTER INDEX {target} REBUILD{partitionClause};",
                            StartMessage = $"Rebuilding {description}{partitionDescription} [fragmentation: {candidate.FragmentationPct:0.##}%].",
                            DoneMessage = duration => $"Rebuilt {description}{partitionDescription} in {duration.TotalSeconds} seconds.",
                            RequiresPause = DigMaintenancePolicy.RequiresPause(action),
                            TallyKey = "partition rebuilt"
                        });
                        break;
                    case DigLevel2Action.Reorganize:
                        batch.Operations.Add(new Level2Operation
                        {
                            Statement = $"ALTER INDEX {target} REORGANIZE{partitionClause};",
                            StartMessage = $"Reorganizing {description}{partitionDescription} [fragmentation: {candidate.FragmentationPct:0.##}%].",
                            DoneMessage = duration => $"Reorganized {description}{partitionDescription} in {duration.TotalSeconds} seconds.",
                            RequiresPause = DigMaintenancePolicy.RequiresPause(action),
                            TallyKey = "partition reorganized"
                        });
                        break;
                }
            }
        }

        /// <summary>
        /// Reads every row of one of the two maintenance views, in <c>RowId</c> order - which the SQL Server Level 2 loop
        /// depends on, since it relies on the partitions of one index arriving together.
        /// </summary>
        async Task<List<T>> ReadMaintenanceViewAsync<T>(string viewName, CancellationTokenSource cancellationTokenSource) where T : class
        {
            var sql = adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.PostgreSQL =>
                    $@"SELECT * FROM {SqlMapperExtensions.FormatTableNameForSql(viewName, "npgsqlconnection")} ORDER BY ""RowId"";",
                ConnectionInfo.DataAccessProviderType.SQLServer =>
                    $"SELECT * FROM {SqlMapperExtensions.FormatTableNameForSql(viewName, "sqlconnection")} ORDER BY [RowId];",
                _ => throw new NotSupportedException($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };

            var results = await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<T>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
                return await repo.QueryAsync(sql, null, cancellationTokenSource, true, adapterContext);
            }, new Context());
            return results.ToList();
        }

        /// <summary>
        /// Executes one Level 1 or Level 2 statement on a connection of its own and outside any transaction - VACUUM
        /// cannot run inside one - and returns how long it took.
        /// </summary>
        async Task<TimeSpan> ExecuteMaintenanceStatementAsync(string sql, CancellationTokenSource cancellationTokenSource)
        {
            var stopwatch = Stopwatch.StartNew();
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                var repo = new BaseRepository<DbGdaDigMaintenanceLog>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
                await repo.ExecuteAsync(sql, null, cancellationTokenSource, true, adapterContext);
            }, new Context());
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Whether a table or index named by a maintenance view may be maintained: it is in the managed schema, and its
        /// name is safe to place in a statement - it is interpolated, because an identifier cannot be a parameter.
        /// <para>
        /// Both views are scoped to the managed schema already. This is the second of two checks rather than the only
        /// one, and a name that fails it is skipped with a warning rather than failing the whole run: one odd name is
        /// no reason to leave every other index unmaintained.
        /// </para>
        /// </summary>
        bool IsMaintainableObject(string schemaName, string objectName)
        {
            if (string.Equals(schemaName, ManagedSchemaName, StringComparison.Ordinal) == false)
            {
                logger.Warn($"Skipping {schemaName}.{objectName}: database maintenance is confined to the '{ManagedSchemaName}' schema.");
                return false;
            }
            if (stringHelper.IsValidIdentifierForDatabaseObject(objectName) == false)
            {
                logger.Warn($"Skipping {schemaName}.{objectName}: the name is not one that can safely be placed in a maintenance statement.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Asks the other services to pause, and waits for them to say that they have.
        /// <para>
        /// Unlike a prerequisite service check this cannot wait forever on a service that is switched off: a
        /// disabled service registers as not participating, and only services that are both registered and
        /// participating are waited for. It gives up after <see cref="WaitTimeoutMinutesForPausingOtherServices"/>
        /// minutes rather than proceeding regardless, because the partitioning statements take exclusive locks
        /// that would otherwise block whatever those services are in the middle of.
        /// </para>
        /// </summary>
        /// <param name="consequenceIfAbandoned">What happens next if the services do not pause in time, for the warning that
        /// says so: it depends on which pause this is.</param>
        /// <returns><c>true</c> if every participating service has paused.</returns>
        async Task<bool> PauseOtherServicesForDatabaseMaintenanceAsync(string consequenceIfAbandoned, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            logger.Info($"{nameof(DatabaseMaintenanceService)} pausing other services for database maintenance...");
            stateMachine.SetStateReason(StateReason.AdapterDatabaseMaintenance, true);

            var pauseRequestExpiryTimeUtc = DateTime.UtcNow.AddMinutes(WaitTimeoutMinutesForPausingOtherServices);
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(PauseCheckIntervalSeconds), cancellationToken);

                // Both conditions matter. A service that has not yet registered has not yet seen the pause
                // request either, so proceeding while one is still starting up would run the partitioning
                // statements alongside the first thing it does.
                var servicesNotYetRegistered = stateMachine.GetServicesNotYetRegistered();
                var servicesNotYetPaused = stateMachine.GetServicesNotYetPausedForDbMaintenance();
                if (servicesNotYetRegistered == string.Empty && servicesNotYetPaused == string.Empty)
                {
                    return true;
                }

                var message = $"{nameof(DatabaseMaintenanceService)} waiting for other services before starting database maintenance.";
                if (servicesNotYetRegistered != string.Empty)
                {
                    message += $" Not yet registered as operational: {servicesNotYetRegistered}.";
                }
                if (servicesNotYetPaused != string.Empty)
                {
                    message += $" Not yet paused: {servicesNotYetPaused}.";
                }

                if (DateTime.UtcNow > pauseRequestExpiryTimeUtc)
                {
                    // Switch the state back so that any service that DID pause resumes, and try again shortly.
                    stateMachine.SetStateReason(StateReason.AdapterDatabaseMaintenance, false);
                    logger.Warn($"{message} Abandoning this database maintenance iteration because they did not do so within the allowed time of {WaitTimeoutMinutesForPausingOtherServices} minutes. {consequenceIfAbandoned}");
                    return false;
                }

                logger.Info(message);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        void HandleAdapterDatabaseConnectionException(AdapterDatabaseConnectionException ex)
        {
            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
            stateMachine.HandleException(ex, NLogLogLevelName.Error);
        }

        /// <summary>
        /// Logs an unexpected exception at Error, and continues.
        /// <para>
        /// Deliberately NOT Fatal, which kills the process. Everything this service does is either housekeeping
        /// that can wait for the next interval, or a refusal by one of the procedures - a partition interval
        /// that no longer matches, a retention cutoff in the future - that an operator has to correct in
        /// configuration. None of them is a reason to stop transmitting telemetry.
        /// </para>
        /// </summary>
        void HandleGenericException(Exception ex)
        {
            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
            stateMachine.HandleException(ex, NLogLogLevelName.Error);
        }

        /// <summary>
        /// Waits for the application to finish initializing, and for database connectivity, before continuing.
        /// <para>
        /// The prerequisite list is empty on purpose. This service is the one that requests the maintenance
        /// pause, so it waits for no other service and never waits for itself; the empty-list call is what makes
        /// it wait for the Orchestrator to initialize.
        /// </para>
        /// </summary>
        async Task PrepareForExecutionAsync(CancellationToken stoppingToken)
        {
            await awaiter.WaitForPrerequisiteServicesIfNeededAsync(new List<DIGAdapterService>(), stoppingToken);
            await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);
        }

        /// <summary>
        /// Starts the current <see cref="DatabaseMaintenanceService"/> instance.
        /// </summary>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbGdaOServiceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbGdaOServiceTrackings, DIGAdapterService.DatabaseMaintenanceService, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, DIGAdapterService.DatabaseMaintenanceService, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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

            // Deliberately NOT registered with the StateMachine. Registration is what makes a service one that
            // the maintenance pause waits for, and this is the service that requests the pause: registering
            // would make it wait for itself forever.

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.EnableDatabaseMaintenanceService)
            {
                logger.Info($"******** STARTING SERVICE: {CurrentClassName}");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {CurrentClassName} service has not been enabled and will NOT be started. No period partitions will be created, so failure records will accumulate in a single partition per table and cannot be reclaimed by period, and no Level 1 or Level 2 database maintenance will be performed.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="DatabaseMaintenanceService"/> instance.
        /// </summary>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Clear any outstanding pause request, so that the other services are not left waiting on a
            // maintenance run that is never going to finish.
            stateMachine.SetStateReason(StateReason.AdapterDatabaseMaintenance, false);
            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the <see cref="DbGdaOServiceTracking"/> record associated with the <see cref="DatabaseMaintenanceService"/> service.
        /// </summary>
        async Task UpdateServiceTrackingRecordAsync()
        {
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                try
                {
                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, DIGAdapterService.DatabaseMaintenanceService, DateTime.UtcNow);
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
