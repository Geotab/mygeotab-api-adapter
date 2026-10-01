using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
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
    /// A <see cref="BackgroundService"/> that handles tasks associated with maintenance of the Adapter database. 
    /// </summary>
    class DatabaseMaintenanceService2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Thresholds for database maintenance tasks. These are compared against columns returned by
        // vwStatsForLevel1DBMaintenance and vwStatsForLevel2DBMaintenance, so each one must be expressed in the
        // SAME UNIT as the column it is compared against. The two providers differ, deliberately:
        //   - the SQL Server views return PERCENTAGES (their expressions end in "* 100"), so every MSSQL_
        //     constant below is a percentage in the range 0..100 and is named "...Pct";
        //   - the PostgreSQL views return RATIOS (a plain division, no "* 100"), so every PG_ constant below
        //     is a ratio in the range 0..1 and is named "...Ratio".
        // Getting this wrong is silent in both directions - the view offers a candidate the service declines,
        // or the reverse - and it has happened: the statistics threshold below held 0.1, and the whole-index
        // rebuild threshold below was named "...HighFragPartitionRatio" and held 0.5. Both read as 10% and
        // 50%, both were compared against percentage columns, and so both acted at 0.1% and 0.5%.
        const double MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate = 10;
        const double MSSQL_ReorganizeThreshold_FragmentationPct = 10;
        const double MSSQL_ReindexThreshold_FragmentationPct = 30;
        const double MSSQL_ReindexByTableThreshold_HighFragPartitionPct = 50;
        const double PG_VacuumThreshold_DeadTupleRatio = 0.2;
        const int PG_VacuumThreshold_DeadTuplesCount = 1000;
        const double PG_AnalyzeThreshold_ModsSinceLastAnalyzeRatio = 0.1;
        const double PG_ReindexThreshold_BloatRatio = 0.3;
        const int PG_ReindexThreshold_IndexSizeBytes = 1000;
        const int WaitTimeoutMinutesForPausingOtherServices = 5;

        /// <summary>
        /// How many times a Level 1 or Level 2 statement, a read of either maintenance view, or the periodic run of the
        /// partition function, is retried after a transient failure - a deadlock, or a failure to open a connection; a
        /// connection lost part-way through a statement matches no retry rule. A TIMEOUT is not retried at all, and on
        /// PostgreSQL that includes a statement stopped by a lock_timeout or statement_timeout set for the adapter's login
        /// or database.
        /// <para>
        /// The transaction retry policy used everywhere else in this service retries a timeout a million times, and each
        /// repository call beneath it ten more times with a longer command timeout on each attempt. That is right for a
        /// short statement that a transient condition will release. An index rebuild or a statistics update that times
        /// out is not that: it needs more time than it was given, and every retry starts its work again from the
        /// beginning. Level 2 and the periodic partition run happen while the other services are paused, so those
        /// retries held them paused - indefinitely at first, and still for up to about 26 hours per statement once this
        /// layer's retries were bounded. So each of these statements is instead given ONE attempt at the longest command
        /// timeout any attempt could have, <see cref="DatabaseResilienceHelper.MaxTimeoutSeconds"/> (see the repositories
        /// built in the constructor), and a timeout ends its run. Once a run has failed, the next attempt waits for the
        /// configured interval - or, for the partition run, for
        /// <see cref="DatabasePartitioningSchedule.RetryDelayMinutesAfterFailure"/> minutes. The partition run at
        /// startup is the exception: see <see cref="PartitionDatabaseIfNeededAsync"/>.
        /// </para>
        /// </summary>
        const int MaxMaintenanceStatementRetries = 3;

        bool initialPartitioningOnStartupCompleted = false;

        /// <summary>
        /// When the latest successful partition run started. The run at startup always sets it, since a failed run at startup stops the adapter. See <see cref="ThrowIfPartitionsNearlyExhausted"/>.
        /// </summary>
        DateTime? latestSuccessfulPartitionRunStartTimeUtc;

        int serviceExecutionIntervalMinutes = 5;

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;
        readonly AsyncRetryPolicy asyncRetryPolicyForMaintenanceStatements;

        readonly IBaseRepository<DbDBMaintenanceLog2> dbDBMaintenanceLog2Repo;
        readonly IBaseRepository<DbDBMaintenanceLog2> maintenanceStatementRepo;
        readonly IBaseRepository<DbDBPartitionInfo2> dbDBPartitionInfo2Repo;
        readonly IBaseRepository<DbvwStatForLevel1DBMaintenance_MSSQL> dbvwStatForLevel1DBMaintenance_MSSQLRepo;
        readonly IBaseRepository<DbvwStatForLevel2DBMaintenance_MSSQL> dbvwStatForLevel2DBMaintenance_MSSQLRepo;
        readonly IBaseRepository<DbvwStatForLevel1DBMaintenance_PG> dbvwStatForLevel1DBMaintenance_PGRepo;
        readonly IBaseRepository<DbvwStatForLevel2DBMaintenance_PG> dbvwStatForLevel2DBMaintenance_PGRepo;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IBackgroundServiceAwaiter<DatabaseMaintenanceService2> awaiter;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbDBMaintenanceLog2> dbDBMaintenanceLog2Persister;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;
        readonly IStringHelper stringHelper;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// The <see cref="DbDBPartitionInfo2"/> record that contains the initial partitioning information for the database.
        /// </summary>
        public DbDBPartitionInfo2 CurrentDbDBPartitionInfo2 { get; private set; }

        /// <summary>
        /// The latest <see cref="DbDBMaintenanceLog2"/> record for <see cref="DBMaintenanceType.Level1"/> maintenance.
        /// </summary>
        public DbDBMaintenanceLog2 LatestLevel1DbDBMaintenanceLog2 { get; private set; }

        /// <summary>
        /// The latest <see cref="DbDBMaintenanceLog2"/> record for <see cref="DBMaintenanceType.Level2"/> maintenance.
        /// </summary>
        public DbDBMaintenanceLog2 LatestLevel2DbDBMaintenanceLog2 { get; private set; }

        /// <summary>
        /// The latest <see cref="DbDBMaintenanceLog2"/> record for <see cref="DBMaintenanceType.Partition"/> maintenance.
        /// </summary>
        public DbDBMaintenanceLog2 LatestPartitionDbDBMaintenanceLog2 { get; private set; }

        /// <summary>
        /// The interval (in minutes) at which this service executes. On each interval, database partitioning, Level 1 and/or Level 2 database maintenance are performed if their respective intervals (configured via appsettings.json) have elapsed.
        /// </summary>
        public int ServiceExecutionIntervalMinutes { get => serviceExecutionIntervalMinutes; private set => serviceExecutionIntervalMinutes = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseMaintenanceService2"/> class.
        /// </summary>
        public DatabaseMaintenanceService2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IBackgroundServiceAwaiter<DatabaseMaintenanceService2> awaiter, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbDBMaintenanceLog2> dbDBMaintenanceLog2Persister, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IStringHelper stringHelper, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.awaiter = awaiter;
            this.dateTimeHelper = dateTimeHelper;
            this.exceptionHelper = exceptionHelper;
            this.dbDBMaintenanceLog2Persister = dbDBMaintenanceLog2Persister;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;
            this.stringHelper = stringHelper;

            dbDBMaintenanceLog2Repo = new BaseRepository<DbDBMaintenanceLog2>(adapterContext);
            dbDBPartitionInfo2Repo = new BaseRepository<DbDBPartitionInfo2>(adapterContext);

            // The maintenance statements and the maintenance view reads get ONE attempt each, at the longest command timeout any attempt can have - see MaxMaintenanceStatementRetries.
            maintenanceStatementRepo = new BaseRepository<DbDBMaintenanceLog2>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
            dbvwStatForLevel1DBMaintenance_PGRepo = new BaseRepository<DbvwStatForLevel1DBMaintenance_PG>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
            dbvwStatForLevel2DBMaintenance_PGRepo = new BaseRepository<DbvwStatForLevel2DBMaintenance_PG>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
            dbvwStatForLevel1DBMaintenance_MSSQLRepo = new BaseRepository<DbvwStatForLevel1DBMaintenance_MSSQL>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);
            dbvwStatForLevel2DBMaintenance_MSSQLRepo = new BaseRepository<DbvwStatForLevel2DBMaintenance_MSSQL>(adapterContext, DatabaseResilienceHelper.MaxTimeoutSeconds);

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy, and a bounded one that does not retry a timeout for the maintenance work.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
            asyncRetryPolicyForMaintenanceStatements = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger, MaxMaintenanceStatementRetries, retryTimeouts: false);
        }

        /// <summary>
        /// Executes a ANALYZE command on the PostgreSQL table identified in the <paramref name="dbvwStatForLevel1DBMaintenance_PG"/>.
        /// </summary>
        /// <param name="dbvwStatForLevel1DBMaintenance_PG">The <see cref="DbvwStatForLevel1DBMaintenance_PG"/> from which the <see cref="DbvwStatForLevel1DBMaintenance_PG.SchemaName"/> and <see cref="DbvwStatForLevel1DBMaintenance_PG.TableName"/> will be used when generating the ANALYZE SQL statement.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task AnalyzePostgresTableAsync(DbvwStatForLevel1DBMaintenance_PG dbvwStatForLevel1DBMaintenance_PG, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Ensure that the provider type is PostgreSQL.
            if (adapterContext.ProviderType != ConnectionInfo.DataAccessProviderType.PostgreSQL)
            {
                throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.");
            }

            // Validate identifiers for the schema and table names.
            if (!stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel1DBMaintenance_PG.SchemaName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel1DBMaintenance_PG.TableName))
            {
                throw new Exception($"The schema name '{dbvwStatForLevel1DBMaintenance_PG.SchemaName}' and/or table name '{dbvwStatForLevel1DBMaintenance_PG.TableName}' are not valid identifiers for a database object.");
            }

            // Build the SQL statement.
            var sql = $"ANALYZE \"{dbvwStatForLevel1DBMaintenance_PG.SchemaName}\".\"{dbvwStatForLevel1DBMaintenance_PG.TableName}\";";

            // Execute the function.
            var startTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await maintenanceStatementRepo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    throw;
                }
            }, new Context());
            var duration = DateTime.UtcNow - startTimeUtc;

            logger.Info($"Analyzed table {dbvwStatForLevel1DBMaintenance_PG.SchemaName}.{dbvwStatForLevel1DBMaintenance_PG.TableName} in {duration.TotalSeconds} seconds.");

            methodCancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();

            var delayTimeSpan = TimeSpan.FromMinutes(ServiceExecutionIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService> { };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var dbOServiceTracking = await serviceTracker.GetDatabaseMaintenanceService2InfoAsync();
                        var dbDBMaintenanceLog2sToPersist = new List<DbDBMaintenanceLog2>();

                        if (initialPartitioningOnStartupCompleted == false)
                        {
                            // Execute database partition maintenance if needed.
                            var dbDBMaintenanceLog2ToPersist = await PartitionDatabaseIfNeededAsync(cancellationTokenSource);
                            if (dbDBMaintenanceLog2ToPersist != null)
                            {
                                dbDBMaintenanceLog2sToPersist.Add(dbDBMaintenanceLog2ToPersist);
                            }
                        }
                        else 
                        {
                            // Execute database partition maintenance if needed.
                            var dbDBMaintenanceLog2ToPersist = await PartitionDatabaseIfNeededAsync(cancellationTokenSource);
                            if (dbDBMaintenanceLog2ToPersist != null)
                            {
                                dbDBMaintenanceLog2sToPersist.Add(dbDBMaintenanceLog2ToPersist);
                            }

                            // Stop, as a failed run at startup does, if partition maintenance has gone without a success for so long that its partitions are about to run out.
                            ThrowIfPartitionsNearlyExhausted();

                            // Execute Level 1 database maintenance if needed.
                            dbDBMaintenanceLog2ToPersist = await ExecuteLevel1DatabaseMaintenanceIfNeededAsync(cancellationTokenSource);
                            if (dbDBMaintenanceLog2ToPersist != null)
                            {
                                dbDBMaintenanceLog2sToPersist.Add(dbDBMaintenanceLog2ToPersist);
                            }

                            // Execute Level 2 database maintenance if needed.
                            dbDBMaintenanceLog2ToPersist = await ExecuteLevel2DatabaseMaintenanceIfNeededAsync(cancellationTokenSource);
                            if (dbDBMaintenanceLog2ToPersist != null)
                            {
                                dbDBMaintenanceLog2sToPersist.Add(dbDBMaintenanceLog2ToPersist);
                            }
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbDBMaintenanceLog2.
                                    await dbDBMaintenanceLog2Persister.PersistEntitiesToDatabaseAsync(adapterContext, dbDBMaintenanceLog2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking2:
                                    // The OServiceTracking record for this service needs to be updated to show that the service is operating.
                                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DatabaseMaintenanceService2, DateTime.UtcNow);

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

                        // If any DB maintenance was actually done during the current iteration, get the latest DBMaintenanceLog2 records.
                        if (dbDBMaintenanceLog2sToPersist.Count != 0)
                        { 
                            await GetLatestDBMaintenanceLogsByTypeAsync();
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
                await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Wait, stoppingToken);
            }
        }

        /// <summary>
        /// Executes Level 1 database maintenance if it has never been run or if the interval since it was last run has elapsed.
        /// <para>
        /// A failure is logged, returned as a record whose Success is false, and does NOT stop the iteration: Level 2
        /// still follows, and every record the iteration produced is still persisted. The failure record ends the run,
        /// so the next attempt waits for the configured interval - measured from the failed run's end, where a successful
        /// run's is measured from its start (see <see cref="DatabaseMaintenanceLevelSchedule"/>). Nothing used to
        /// catch a failure here. It reached the service's last-resort handler, which logs at Fatal, and Fatal kills the
        /// process.
        /// </para>
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<DbDBMaintenanceLog2> ExecuteLevel1DatabaseMaintenanceIfNeededAsync(CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            DbDBMaintenanceLog2 dbDBMaintenanceLog2 = null;

            // Execute Level 1 maintenance if it is enabled and the interval since the last run has elapsed.
            if (adapterConfiguration.EnableLevel1DatabaseMaintenance == true && DatabaseMaintenanceLevelSchedule.IsDue(LatestLevel1DbDBMaintenanceLog2, adapterConfiguration.Level1DatabaseMaintenanceIntervalMinutes, DateTime.UtcNow))
            {
                logger.Info($"Starting Level 1 database maintenance.");

                var startTimeUtc = DateTime.UtcNow;

                try
                {
                    // Level 1 maintenance steps differ between database types.
                    switch (adapterContext.ProviderType)
                    {
                        // Postgres:
                        case ConnectionInfo.DataAccessProviderType.PostgreSQL:
                            await ExecuteLevel1DatabaseMaintenance_PG_Async(methodCancellationTokenSource);
                            break;
                        // SQL Server:
                        case ConnectionInfo.DataAccessProviderType.SQLServer:
                            await ExecuteLevel1DatabaseMaintenance_MSSQL_Async(methodCancellationTokenSource);
                            break;
                        default:
                            throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not AdapterDatabaseConnectionException)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"{CurrentClassName} Level 1 database maintenance failed. The rest of this iteration continues, and Level 1 will not be attempted again for {adapterConfiguration.Level1DatabaseMaintenanceIntervalMinutes} minute(s)");

                    // Record the failed run. Its EndTimeUtc is what lets the interval gate above space the next attempt.
                    var failedRunEndTimeUtc = DateTime.UtcNow;
                    return new DbDBMaintenanceLog2
                    {
                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                        MaintenanceTypeId = (short)DBMaintenanceType.Level1.Id,
                        StartTimeUtc = startTimeUtc,
                        EndTimeUtc = failedRunEndTimeUtc,
                        Success = false,
                        RecordLastChangedUtc = failedRunEndTimeUtc
                    };
                }

                var endTimeUtc = DateTime.UtcNow;
                var duration = endTimeUtc - startTimeUtc;
                logger.Info($"Level 1 database maintenance completed successfully. Duration: {duration}");

                // Create a new DbDBMaintenanceLog2 record for the Level 1 maintenance.
                dbDBMaintenanceLog2 = new DbDBMaintenanceLog2
                {
                    DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                    MaintenanceTypeId = (short)DBMaintenanceType.Level1.Id,
                    StartTimeUtc = startTimeUtc,
                    EndTimeUtc = endTimeUtc,
                    Success = true,
                    RecordLastChangedUtc = DateTime.UtcNow
                };

                methodCancellationToken.ThrowIfCancellationRequested();
            }
            return dbDBMaintenanceLog2;
        }

        /// <summary>
        /// Executes Level 1 database maintenance (SQL Server version).
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ExecuteLevel1DatabaseMaintenance_MSSQL_Async(CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Get a list of tables for which statistics should be recalculated.
            var dbvwStatForLevel1DBMaintenance_MSSQLs = await ReadMaintenanceViewAsync(dbvwStatForLevel1DBMaintenance_MSSQLRepo, methodCancellationTokenSource);

            if (dbvwStatForLevel1DBMaintenance_MSSQLs.Count != 0)
            {
                logger.Info($"Found {dbvwStatForLevel1DBMaintenance_MSSQLs.Count} tables that may need Level 1 maintenance.");
                logger.Info($"> Statistics will be updated on tables where the percentage of modifications since last statistics update exceeds {MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate}%.");

                // Perform the necessary Level 1 maintanance steps for each table in need.
                foreach (var dbvwStatForLevel1DBMaintenance_MSSQL in dbvwStatForLevel1DBMaintenance_MSSQLs)
                {
                    methodCancellationToken.ThrowIfCancellationRequested();

                    if (dbvwStatForLevel1DBMaintenance_MSSQL.PctModsSinceLastStatsUpdate > MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate)
                    {
                        logger.Info($"Updating statistics for table {dbvwStatForLevel1DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel1DBMaintenance_MSSQL.TableName} [modifications since last statistics update:  {dbvwStatForLevel1DBMaintenance_MSSQL.PctModsSinceLastStatsUpdate}%].");
                        await UpdateStatisticsForMSSQLTableAsync(dbvwStatForLevel1DBMaintenance_MSSQL, methodCancellationTokenSource);
                    }
                }
            }
            else
            {
                logger.Info($"No tables found that need Level 1 maintenance.");
            }
        }

        /// <summary>
        /// Executes Level 1 database maintenance (Postgres version).
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ExecuteLevel1DatabaseMaintenance_PG_Async(CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Get a list of tables that need to be vacuumed and/or analyzed (based on the thresholds in the database view).
            var dbvwStatForLevel1DBMaintenance_PGs = await ReadMaintenanceViewAsync(dbvwStatForLevel1DBMaintenance_PGRepo, methodCancellationTokenSource);

            if (dbvwStatForLevel1DBMaintenance_PGs.Count != 0)
            {
                logger.Info($"Found {dbvwStatForLevel1DBMaintenance_PGs.Count} tables that may need Level 1 maintenance.");
                logger.Info($"> Tables will be vacuumed and analyzed where the dead tuple ratio exceeds {PG_VacuumThreshold_DeadTupleRatio} OR the number of dead tuples exceeds {PG_VacuumThreshold_DeadTuplesCount}.");
                logger.Info($"> Tables will be analyzed where the ratio of modifications since last analyze exceeds {PG_AnalyzeThreshold_ModsSinceLastAnalyzeRatio}.");

                // Perform the necessary Level 1 maintanance steps for each table in need.
                foreach (var dbvwStatForLevel1DBMaintenance_PG in dbvwStatForLevel1DBMaintenance_PGs)
                {
                    methodCancellationToken.ThrowIfCancellationRequested();

                    if (dbvwStatForLevel1DBMaintenance_PG.PctDeadTuples > PG_VacuumThreshold_DeadTupleRatio || dbvwStatForLevel1DBMaintenance_PG.DeadTuples > PG_VacuumThreshold_DeadTuplesCount)
                    {
                        logger.Info($"Vacuuming & analyzing table {dbvwStatForLevel1DBMaintenance_PG.SchemaName}.{dbvwStatForLevel1DBMaintenance_PG.TableName} [dead tuple ratio: {dbvwStatForLevel1DBMaintenance_PG.PctDeadTuples}; dead tuples: {dbvwStatForLevel1DBMaintenance_PG.DeadTuples}].");
                        await VacuumPostgresTableAsync(dbvwStatForLevel1DBMaintenance_PG, methodCancellationTokenSource);
                        await AnalyzePostgresTableAsync(dbvwStatForLevel1DBMaintenance_PG, methodCancellationTokenSource);
                    }
                    else if (dbvwStatForLevel1DBMaintenance_PG.PctModsSinceLastAnalyze > PG_AnalyzeThreshold_ModsSinceLastAnalyzeRatio)
                    {
                        logger.Info($"Analyzing table {dbvwStatForLevel1DBMaintenance_PG.SchemaName}.{dbvwStatForLevel1DBMaintenance_PG.TableName} [modification ratio since last analyze: {dbvwStatForLevel1DBMaintenance_PG.PctModsSinceLastAnalyze}].");
                        await AnalyzePostgresTableAsync(dbvwStatForLevel1DBMaintenance_PG, methodCancellationTokenSource);
                    }
                }
            }
            else
            {
                logger.Info($"No tables found that need Level 1 maintenance.");
            }
        }

        /// <summary>
        /// Executes Level 2 database maintenance if it has never been run or if the interval since it was last run has elapsed.
        /// <para>
        /// A failure is handled as Level 1's is: logged, returned as a record whose Success is false, and the iteration
        /// continues. The other services are released from the pause either way.
        /// </para>
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<DbDBMaintenanceLog2> ExecuteLevel2DatabaseMaintenanceIfNeededAsync(CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            DbDBMaintenanceLog2 dbDBMaintenanceLog2 = null;

            // Calculate metrics to determine if Level 2 maintenance should be executed.
            var currentDateTimeUtc = DateTime.UtcNow;
            var level2DbMaintenanceEnabled = adapterConfiguration.EnableLevel2DatabaseMaintenance;
            // A failed run is spaced from its END: see DatabaseMaintenanceLevelSchedule.
            var level2DbMaintenanceIntervalElapsed = DatabaseMaintenanceLevelSchedule.IsDue(LatestLevel2DbDBMaintenanceLog2, adapterConfiguration.Level2DatabaseMaintenanceIntervalMinutes, currentDateTimeUtc);

            // Whether the maintenance window allows a run now and, if it does, when the run must stop starting new index
            // operations. Two things this used to get wrong are corrected in Level2DatabaseMaintenanceWindow. The end of
            // the window was passed as a stop time even with the window DISABLED - the shipped default - so every run
            // beginning after 07:00 UTC with the default window paused every other service and then maintained nothing,
            // while logging that it had completed successfully. And only TODAY's window was considered, so the part of an
            // enabled window falling after midnight UTC - when it is yesterday's window that is open - was never used.
            var (withinMaintenanceWindow, level2DbMaintenanceStopTimeUtc) = Level2DatabaseMaintenanceWindow.Evaluate(adapterConfiguration.EnableLevel2DatabaseMaintenanceWindow, adapterConfiguration.Level2DatabaseMaintenanceWindowStartTimeUTC, adapterConfiguration.Level2DatabaseMaintenanceWindowMaxMinutes, currentDateTimeUtc);

            var executeLevel2DbMaintenance = level2DbMaintenanceEnabled && level2DbMaintenanceIntervalElapsed && withinMaintenanceWindow;

            // If Level 2 maintenance is enabled and hasn't been run today and the current time is within the maintenance window, execute Level 2 maintenance.
            if (executeLevel2DbMaintenance)
            {
                var startTimeUtc = DateTime.UtcNow;
                try
                {
                    // Trigger a pause and wait for the other services to pause before proceeding with database maintenance. If the other services don't pause within the timeout period, don't proceed with Level 2 database maintenance.
                    var otherServicesPaused = await PauseOtherServicesForDatabaseMaintenanceAsync(methodCancellationTokenSource);
                    if (otherServicesPaused == false)
                    {
                        // Recorded as a failed run, so that it is spaced out like any other failure. Returning nothing left
                        // Level 2 due on every five-minute tick, each attempt holding the services that did pause for up to
                        // the timeout while waiting for the one that did not.
                        logger.Warn($"Skipping Level 2 database maintenance because other services did not pause within the timeout period of {WaitTimeoutMinutesForPausingOtherServices} minutes. It is recorded as a failed run and will not be attempted again for {adapterConfiguration.Level2DatabaseMaintenanceIntervalMinutes} minute(s).");
                        var skippedAtUtc = DateTime.UtcNow;
                        return new DbDBMaintenanceLog2
                        {
                            DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                            MaintenanceTypeId = (short)DBMaintenanceType.Level2.Id,
                            StartTimeUtc = skippedAtUtc,
                            EndTimeUtc = skippedAtUtc,
                            Success = false,
                            RecordLastChangedUtc = skippedAtUtc
                        };
                    }

                    logger.Info($"Starting Level 2 database maintenance.");

                    startTimeUtc = DateTime.UtcNow;

                    // Level 2 maintenance steps differ between database types.
                    switch (adapterContext.ProviderType)
                    {
                        // Postgres:
                        case ConnectionInfo.DataAccessProviderType.PostgreSQL:
                            await ExecuteLevel2DatabaseMaintenance_PG_Async(level2DbMaintenanceStopTimeUtc, methodCancellationTokenSource);
                            break;
                        // SQL Server:
                        case ConnectionInfo.DataAccessProviderType.SQLServer:
                            await ExecuteLevel2DatabaseMaintenance_MSSQL_Async(level2DbMaintenanceStopTimeUtc, methodCancellationTokenSource);
                            break;
                        default:
                            throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.");
                    }

                    var endTimeUtc = DateTime.UtcNow;
                    var duration = endTimeUtc - startTimeUtc;
                    logger.Info($"Level 2 database maintenance completed successfully. Duration: {duration}");

                    // Create a new DbDBMaintenanceLog2 record for the Level 1 maintenance.
                    dbDBMaintenanceLog2 = new DbDBMaintenanceLog2
                    {
                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                        MaintenanceTypeId = (short)DBMaintenanceType.Level2.Id,
                        StartTimeUtc = startTimeUtc,
                        EndTimeUtc = endTimeUtc,
                        Success = true,
                        RecordLastChangedUtc = DateTime.UtcNow
                    };

                    methodCancellationToken.ThrowIfCancellationRequested();
                }
                catch (Exception ex) when (ex is not OperationCanceledException && ex is not AdapterDatabaseConnectionException)
                {
                    // A failure is logged and recorded, and does NOT stop the iteration - the pause is released below and
                    // the iteration's records are still persisted. Nothing used to catch a failure here: it reached the
                    // service's last-resort handler, which logs at Fatal, and Fatal kills the process. A missing grant on
                    // pgstattuple did exactly that, on the second iteration after every start.
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"{CurrentClassName} Level 2 database maintenance failed. The other services will now resume, and Level 2 will not be attempted again for {adapterConfiguration.Level2DatabaseMaintenanceIntervalMinutes} minute(s)");

                    // Record the failed run. Its EndTimeUtc is what lets the interval gate above space the next attempt.
                    var failedRunEndTimeUtc = DateTime.UtcNow;
                    dbDBMaintenanceLog2 = new DbDBMaintenanceLog2
                    {
                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                        MaintenanceTypeId = (short)DBMaintenanceType.Level2.Id,
                        StartTimeUtc = startTimeUtc,
                        EndTimeUtc = failedRunEndTimeUtc,
                        Success = false,
                        RecordLastChangedUtc = failedRunEndTimeUtc
                    };
                }
                finally
                {
                    // Reset the state machine to normal so that other services can resume opertaion.
                    stateMachine.SetStateReason(StateReason.AdapterDatabaseMaintenance, false);
                }
            }
            return dbDBMaintenanceLog2;
        }

        /// <summary>
        /// Executes Level 2 database maintenance (SQL Server version).
        /// </summary>
        /// <param name="level2DbMaintenanceStopTimeUtc">The time at which to stop processing further indexes, or <c>null</c> when the maintenance window is disabled and the run should process every candidate.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ExecuteLevel2DatabaseMaintenance_MSSQL_Async(DateTime? level2DbMaintenanceStopTimeUtc, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Get a list of indexes that need to be reorganized or reindexed (based on the thresholds in the database view).
            var indexRetrievalStartTimeUtc = DateTime.UtcNow;
            var dbvwStatForLevel2DBMaintenance_MSSQLs = await ReadMaintenanceViewAsync(dbvwStatForLevel2DBMaintenance_MSSQLRepo, methodCancellationTokenSource);
            var indexRetrievalDuration = DateTime.UtcNow - indexRetrievalStartTimeUtc;

            if (dbvwStatForLevel2DBMaintenance_MSSQLs.Count != 0)
            {
                logger.Info($"Found {dbvwStatForLevel2DBMaintenance_MSSQLs.Count} indexes that may need Level 2 maintenance. Duration: {indexRetrievalDuration}");
                logger.Info($"> Indexes will be rebuilt where the percentage of highly-fragmented partitions exceeds {MSSQL_ReindexByTableThreshold_HighFragPartitionPct}%.");
                logger.Info($"> Index partitions will be rebuilt where the fragmentation percentage exceeds {MSSQL_ReindexThreshold_FragmentationPct}.");
                logger.Info($"> Index partitions will be reorganized where the fragmentation percentage exceeds {MSSQL_ReorganizeThreshold_FragmentationPct}.");

                // Perform the necessary Level 2 maintanance steps for each index in need.
                var lastRebuiltIndexName = string.Empty;
                foreach (var dbvwStatForLevel2DBMaintenance_MSSQL in dbvwStatForLevel2DBMaintenance_MSSQLs)
                {
                    methodCancellationToken.ThrowIfCancellationRequested();

                    // Stop reindexing if the current time is past the maintenance window end time.
                    if (level2DbMaintenanceStopTimeUtc != null && DateTime.UtcNow > level2DbMaintenanceStopTimeUtc.Value)
                    {
                        logger.Info($"Skipping processing of remaining indexes because current time is past the maintenance window end time of {level2DbMaintenanceStopTimeUtc.Value}.");
                        break;
                    }

                    // If an entire index was rebuilt, skip any index partitions since they will have been included in the rebuild.
                    if (dbvwStatForLevel2DBMaintenance_MSSQL.IndexName == lastRebuiltIndexName)
                    { 
                        continue;
                    }

                    if (dbvwStatForLevel2DBMaintenance_MSSQL.PctHighFragPartitions > MSSQL_ReindexByTableThreshold_HighFragPartitionPct)
                    {
                        logger.Info($"Rebuilding index {dbvwStatForLevel2DBMaintenance_MSSQL.IndexName} on table {dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel2DBMaintenance_MSSQL.TableName} [highly-fragmented partitions: {dbvwStatForLevel2DBMaintenance_MSSQL.PctHighFragPartitions}%].");
                        await RebuildMSSQLIndexAsync(dbvwStatForLevel2DBMaintenance_MSSQL, methodCancellationTokenSource);
                        lastRebuiltIndexName = dbvwStatForLevel2DBMaintenance_MSSQL.IndexName;
                    }
                    else if (dbvwStatForLevel2DBMaintenance_MSSQL.FragmentationPct > MSSQL_ReindexThreshold_FragmentationPct)
                    {
                        logger.Info($"Rebuilding index {dbvwStatForLevel2DBMaintenance_MSSQL.IndexName} partition {dbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber} on table {dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel2DBMaintenance_MSSQL.TableName} [fragmentation percentage: {dbvwStatForLevel2DBMaintenance_MSSQL.FragmentationPct}].");
                        await RebuildMSSQLIndexPartitionAsync(dbvwStatForLevel2DBMaintenance_MSSQL, methodCancellationTokenSource);
                    }
                    else if (dbvwStatForLevel2DBMaintenance_MSSQL.FragmentationPct > MSSQL_ReorganizeThreshold_FragmentationPct)
                    {
                        logger.Info($"Reorganizing index {dbvwStatForLevel2DBMaintenance_MSSQL.IndexName} partition {dbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber} on table {dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel2DBMaintenance_MSSQL.TableName} [fragmentation percentage: {dbvwStatForLevel2DBMaintenance_MSSQL.FragmentationPct}].");
                        await ReorganizeMSSQLIndexPartitionAsync(dbvwStatForLevel2DBMaintenance_MSSQL, methodCancellationTokenSource);
                    }
                }
            }
            else
            {
                logger.Info($"No indexes found that need Level 2 maintenance.");
            }
        }

        /// <summary>
        /// Executes Level 2 database maintenance (Postgres version).
        /// </summary>
        /// <param name="level2DbMaintenanceStopTimeUtc">The time at which to stop processing further indexes, or <c>null</c> when the maintenance window is disabled and the run should process every candidate.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ExecuteLevel2DatabaseMaintenance_PG_Async(DateTime? level2DbMaintenanceStopTimeUtc, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Get a list of indexes that need to be reindexed (based on the thresholds in the database view).
            var indexRetrievalStartTimeUtc = DateTime.UtcNow;
            var dbvwStatForLevel2DBMaintenance_PGs = await ReadMaintenanceViewAsync(dbvwStatForLevel2DBMaintenance_PGRepo, methodCancellationTokenSource);
            var indexRetrievalDuration = DateTime.UtcNow - indexRetrievalStartTimeUtc;

            if (dbvwStatForLevel2DBMaintenance_PGs.Count != 0)
            {
                logger.Info($"Found {dbvwStatForLevel2DBMaintenance_PGs.Count} indexes that may need Level 2 maintenance. Duration: {indexRetrievalDuration}");
                logger.Info($"> Indexes will be rebuilt where the bloat ratio exceeds {PG_ReindexThreshold_BloatRatio} AND the index size exceeds {PG_ReindexThreshold_IndexSizeBytes} bytes.");

                // Perform the necessary Level 2 maintanance steps for each index in need.
                foreach (var dbvwStatForLevel2DBMaintenance_PG in dbvwStatForLevel2DBMaintenance_PGs)
                {
                    methodCancellationToken.ThrowIfCancellationRequested();

                    // Stop reindexing if the current time is past the maintenance window end time.
                    if (level2DbMaintenanceStopTimeUtc != null && DateTime.UtcNow > level2DbMaintenanceStopTimeUtc.Value)
                    {
                        logger.Info($"Skipping processing of remaining indexes because current time is past the maintenance window end time of {level2DbMaintenanceStopTimeUtc.Value}.");
                        break;
                    }

                    if (dbvwStatForLevel2DBMaintenance_PG.IndexBloatRatio > PG_ReindexThreshold_BloatRatio && dbvwStatForLevel2DBMaintenance_PG.IndexSize > PG_ReindexThreshold_IndexSizeBytes)
                    {
                        logger.Info($"Reindexing index {dbvwStatForLevel2DBMaintenance_PG.SchemaName}.{dbvwStatForLevel2DBMaintenance_PG.IndexName} [bloat ratio: {dbvwStatForLevel2DBMaintenance_PG.IndexBloatRatio}; index size: {dbvwStatForLevel2DBMaintenance_PG.IndexSizeText}].");
                        await ReindexPostgresIndexAsync(dbvwStatForLevel2DBMaintenance_PG, methodCancellationTokenSource);
                    }
                }
            }
            else
            {
                logger.Info($"No indexes found that need Level 2 maintenance.");
            }
        }

        /// <summary>
        /// Gets the <see cref="DbDBPartitionInfo2"/> record and populates the <see cref="CurrentDbDBPartitionInfo2"/> property of this class.
        /// </summary>
        /// <returns></returns>
        async Task GetCurrentDBPartitionInfoAsync()
        {
            try
            {
                // Get all DbDBPartitionInfo2 records.
                var dbDBPartitionInfo2s = new List<DbDBPartitionInfo2>();
                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                {
                    using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                    {
                        // No deadline: BaseRepository checks this token only once the read has succeeded, so a deadline here turned a read
                        // that outlasted it - completed by the repository's own retry - into a cancellation after the fact. The command
                        // timeout bounds the read.
                        using var readCancellationTokenSource = new CancellationTokenSource();
                        dbDBPartitionInfo2s = (List<DbDBPartitionInfo2>)await dbDBPartitionInfo2Repo.GetAllAsync(readCancellationTokenSource);
                    }
                }, new Context());

                // If no DbDBPartitionInfo2 records were found, it means that the partitioning procedure was not executed to partition the database prior to running this application. Throw an exception.
                if (dbDBPartitionInfo2s.Count == 0)
                {
                    throw new Exception($"No {nameof(DbDBPartitionInfo2)} record was found, indicating that {adapterDatabaseObjectNames.PartitioningProcedureName} was not executed to partition the database before this application was started.");
                }
                else if (dbDBPartitionInfo2s.Count > 1)
                {
                    throw new Exception($"More than one {nameof(DbDBPartitionInfo2)} record was found. This should not happen.");
                }
                else
                {
                    // Get the first DbDBPartitionInfo2 record.
                    CurrentDbDBPartitionInfo2 = dbDBPartitionInfo2s[0];
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"{DefaultErrorMessagePrefix}\nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
                logger.Error(errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        /// <summary>
        /// Gets the latest <see cref="DbDBMaintenanceLog2"/> record (if any) for each <see cref="DBMaintenanceType"/> and populates the corresponding properties of this class.
        /// </summary>
        /// <returns></returns>
        async Task GetLatestDBMaintenanceLogsByTypeAsync()
        {
            try
            {
                // Get all DBMaintenanceLog2 records.
                var dbDBMaintenanceLog2s = new List<DbDBMaintenanceLog2>();
                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                {
                    using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                    {
                        // No deadline: BaseRepository checks this token only once the read has succeeded, so a deadline here turned a read
                        // that outlasted it - completed by the repository's own retry - into a cancellation after the fact. The command
                        // timeout bounds the read.
                        using var readCancellationTokenSource = new CancellationTokenSource();
                        dbDBMaintenanceLog2s = (List<DbDBMaintenanceLog2>)await dbDBMaintenanceLog2Repo.GetAllAsync(readCancellationTokenSource);
                    }
                }, new Context());

                // If any DBMaintenanceLog2 records were returned, get the latest one for each maintenance type and set the properties accordingly.
                if (dbDBMaintenanceLog2s.Count != 0)
                {
                    LatestLevel1DbDBMaintenanceLog2 = dbDBMaintenanceLog2s
                        .Where(x => x.MaintenanceTypeId == DBMaintenanceType.Level1.Id)
                        .OrderByDescending(x => x.StartTimeUtc)
                        .FirstOrDefault();

                    LatestLevel2DbDBMaintenanceLog2 = dbDBMaintenanceLog2s
                        .Where(x => x.MaintenanceTypeId == DBMaintenanceType.Level2.Id)
                        .OrderByDescending(x => x.StartTimeUtc)
                        .FirstOrDefault();

                    LatestPartitionDbDBMaintenanceLog2 = dbDBMaintenanceLog2s
                        .Where(x => x.MaintenanceTypeId == DBMaintenanceType.Partition.Id)
                        .OrderByDescending(x => x.StartTimeUtc)
                        .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"{DefaultErrorMessagePrefix}\nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
                logger.Error(errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        /// <summary>
        /// Executes the database partition function if it is due - at startup, and then as <see cref="DatabasePartitioningSchedule.IsDue"/> decides.
        /// <para>
        /// The run at startup and the periodic run fail differently, deliberately. At startup the other services are waiting for this one and have written nothing yet, and partitions must exist before they do, so a failure there still reaches the service's last-resort handler, which logs at Fatal and stops the adapter. The periodic run happens while the adapter is running, under the maintenance pause, with the partitions created by the previous run still in place. A failure there is logged, returned as a record whose Success is false, and does NOT stop the iteration: the other services are released from the pause, and the run is attempted again after <see cref="DatabasePartitioningSchedule.RetryDelayMinutesAfterFailure"/> minutes - until the partitions created by the last successful run are about to run out, when the adapter stops after all (see <see cref="ThrowIfPartitionsNearlyExhausted"/>). Nothing used to catch a failure of the periodic run, so it stopped the adapter exactly as a failure at startup does.
        /// </para>
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<DbDBMaintenanceLog2> PartitionDatabaseIfNeededAsync(CancellationTokenSource methodCancellationTokenSource)
        {
            const string PartitionFunctionSQL_Postgres = @"SELECT public.""spManagePartitions""(@MinDateTimeUTC::timestamp without time zone, @PartitionInterval::text);";
            const string PartitionProcedureSQL_SQLServer = "EXEC [dbo].[spManagePartitions] @MinDateTimeUTC = @MinDateTimeUTC, @PartitionInterval = @PartitionInterval;";

            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            DbDBMaintenanceLog2 dbDBMaintenanceLog2 = null;

            // Partitioning needs to happen regardless of whether Level 1 or Level 2 maintenance are enabled. Otherwise, new data that doesn't fit into any existing partition will be inserted into the default partition, which defeats the purpose of partitioning.
            if (DatabasePartitioningSchedule.IsDue(initialPartitioningOnStartupCompleted, LatestPartitionDbDBMaintenanceLog2, DateTime.UtcNow))
            {
                var isStartupRun = initialPartitioningOnStartupCompleted == false;
                var startTimeUtc = DateTime.UtcNow;
                try
                {
                    // On initial application start up, partitioning needs to happen before any other services write data to tables. This is to avoid data being written to the default partition when it should in fact be written to a yet-to-be-created partition (this could occur if other services started before the partitioning function is executed). Since other services all depend on this DatabaseMaintenanceService2 to be running before they start processing, it is necessary to run the initial partitioning logic without waiting for them. On subsequent iterations, wait for the other services to pause before executing database maintenance.
                    bool otherServicesPaused = false;
                    if (isStartupRun)
                    {
                        otherServicesPaused = true;
                    }
                    else
                    {
                        // Trigger a pause and wait for the other services to pause before proceeding with database partition maintenance. If the other services don't pause within the timeout period, don't proceed with database partition maintenance.
                        otherServicesPaused = await PauseOtherServicesForDatabaseMaintenanceAsync(methodCancellationTokenSource, true);
                    }

                    if (otherServicesPaused == false)
                    {
                        logger.Warn($"Skipping database partition maintenance because other services did not pause within the timeout period of {WaitTimeoutMinutesForPausingOtherServices} minutes.");
                        return null;
                    }

                    logger.Info($"Starting database partition maintenance.");

                    // Define parameters for the partition function.
                    var parameters = new[]
                    {
                        new 
                        { 
                            MinDateTimeUTC = CurrentDbDBPartitionInfo2.InitialMinDateTimeUTC, 
                            PartitionInterval = CurrentDbDBPartitionInfo2.InitialPartitionInterval 
                        }
                    };

                    // Build the SQL statement to execute the partition function.
                    var sql = adapterContext.ProviderType switch
                    {
                        ConnectionInfo.DataAccessProviderType.PostgreSQL => PartitionFunctionSQL_Postgres,
                        ConnectionInfo.DataAccessProviderType.SQLServer => PartitionProcedureSQL_SQLServer,
                        _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                    };

                    startTimeUtc = DateTime.UtcNow;

                    // Execute the partition function. The periodic run is treated as the Level 1 and Level 2 statements are -
                    // one attempt at the longest timeout, and bounded retries of anything but a timeout: it runs under the
                    // pause, and its failure is now recorded rather than fatal. The run at startup keeps the unbounded policy
                    // and the ordinary repository, because its failure still stops the adapter, and bounding the retries
                    // that precede a stop would only make the adapter stop sooner.
                    var partitionRetryPolicy = isStartupRun ? asyncRetryPolicyForDatabaseTransactions : asyncRetryPolicyForMaintenanceStatements;
                    var partitionRepo = isStartupRun ? dbDBMaintenanceLog2Repo : maintenanceStatementRepo;
                    await partitionRetryPolicy.ExecuteAsync(async pollyContext =>
                    {
                        try
                        {
                            await partitionRepo.ExecuteAsync(sql, parameters, methodCancellationTokenSource, true, adapterContext);
                        }
                        catch (Exception ex)
                        {
                            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                            throw;
                        }
                    }, new Context());

                    logger.Info($"Database partition maintenance completed successfully.");

                    // Create a new DbDBMaintenanceLog2 record for the partition maintenance.
                    dbDBMaintenanceLog2 = new DbDBMaintenanceLog2
                    {
                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                        MaintenanceTypeId = (short)DBMaintenanceType.Partition.Id,
                        StartTimeUtc = startTimeUtc,
                        EndTimeUtc = DateTime.UtcNow,
                        Success = true,
                        RecordLastChangedUtc = DateTime.UtcNow
                    };

                    // Mark initial partitioning as complete so that subsequent iterations will pause other services before executing maintenance and will also run Level 1 and Level 2 maintenance.
                    initialPartitioningOnStartupCompleted = true;
                    latestSuccessfulPartitionRunStartTimeUtc = startTimeUtc;

                    methodCancellationToken.ThrowIfCancellationRequested();
                }
                catch (Exception ex) when (isStartupRun == false && ex is not OperationCanceledException && ex is not AdapterDatabaseConnectionException)
                {
                    // Only the PERIODIC run is caught - see this method's summary. A failure at startup propagates, as it
                    // always has, and stops the adapter before any other service writes.
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"{CurrentClassName} database partition maintenance failed. The other services will now resume, and partition maintenance will be attempted again in {DatabasePartitioningSchedule.RetryDelayMinutesAfterFailure} minute(s)");

                    // Record the failed run. Its EndTimeUtc is what DatabasePartitioningSchedule measures the retry delay from.
                    var failedRunEndTimeUtc = DateTime.UtcNow;
                    dbDBMaintenanceLog2 = new DbDBMaintenanceLog2
                    {
                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                        MaintenanceTypeId = (short)DBMaintenanceType.Partition.Id,
                        StartTimeUtc = startTimeUtc,
                        EndTimeUtc = failedRunEndTimeUtc,
                        Success = false,
                        RecordLastChangedUtc = failedRunEndTimeUtc
                    };
                }
                finally
                {
                    // Reset the state machine to normal so that other services can resume opertaion.
                    stateMachine.SetStateReason(StateReason.AdapterDatabaseMaintenance, false);
                }
            }

            return dbDBMaintenanceLog2;
        }

        /// <summary>
        /// Stops the adapter, as a failed partition run at startup does, once the partitions created by the latest successful partition run are about to run out - see <see cref="DatabasePartitioningSchedule.ArePartitionsNearlyExhausted"/>.
        /// <para>
        /// A periodic partition run that fails lets the adapter carry on, and the run is attempted again. (It is never skipped: it waits for the other services to pause for as long as that takes.) This is what stops a cause that persists from carrying the adapter past the end of its partitions, after which every new record would land in the DEFAULT partition on PostgreSQL, or in the last, open-ended partition on SQL Server, and would have to be moved when its partition was eventually created.
        /// </para>
        /// </summary>
        void ThrowIfPartitionsNearlyExhausted()
        {
            if (latestSuccessfulPartitionRunStartTimeUtc == null
                || DatabasePartitioningSchedule.ArePartitionsNearlyExhausted(latestSuccessfulPartitionRunStartTimeUtc.Value, DateTime.UtcNow) == false)
            {
                return;
            }

            var partitionsEndUtc = DatabasePartitioningSchedule.GetPartitionsEndUtc(latestSuccessfulPartitionRunStartTimeUtc.Value);
            throw new Exception($"{CurrentClassName} is stopping the adapter: database partition maintenance has not succeeded since {latestSuccessfulPartitionRunStartTimeUtc.Value:yyyy-MM-dd HH:mm:ss} UTC, and the partitions that run created end at {partitionsEndUtc:yyyy-MM-dd HH:mm:ss} UTC, less than {DatabasePartitioningSchedule.StopMarginDays} days from now. Records dated after that would land outside the partitions - in the DEFAULT partition on PostgreSQL, or in the last, open-ended partition on SQL Server - and each would have to be moved later. Correct the cause given by the failed attempts logged at Error, then start the adapter again: partition maintenance runs at startup.");
        }

        /// <summary>
        /// Waits for other services to pause before proceeding with database maintenance.
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="ignoreWaitTimeoutMinutesForPausingOtherServices">If true, waits indefinitely for other services to pause before proceeding with database maintenance. Otherwise, respects <see cref="WaitTimeoutMinutesForPausingOtherServices"/>.</param>
        /// <returns></returns>
        async Task<bool> PauseOtherServicesForDatabaseMaintenanceAsync(CancellationTokenSource methodCancellationTokenSource, bool ignoreWaitTimeoutMinutesForPausingOtherServices = false)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Change the StateMachine state and reason to trigger other services to pause.
            logger.Info($"{nameof(DatabaseMaintenanceService2)} pausing other services for database maintenance...");
            stateMachine.SetStateReason(StateReason.AdapterDatabaseMaintenance, true);

            var pauseRequestExpiryTimeUtc = DateTime.UtcNow.AddMinutes(WaitTimeoutMinutesForPausingOtherServices);
            bool otherServicesPaused = false;
            while (!otherServicesPaused)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), methodCancellationToken);

                var servicesNotYetRegistered = stateMachine.GetServicesNotYetRegistered();
                var servicesNotYetPaused = stateMachine.GetServicesNotYetPausedForDbMaintenance();
                if (servicesNotYetPaused != string.Empty)
                {
                    // Abort if other services have not paused within the timeout period and switch state back to normal so that paused services can resume operation.
                    if (ignoreWaitTimeoutMinutesForPausingOtherServices == false && DateTime.UtcNow > pauseRequestExpiryTimeUtc)
                    {
                        stateMachine.SetStateReason(StateReason.AdapterDatabaseMaintenance, false);
                        var message = $"{nameof(DatabaseMaintenanceService2)} stopped waiting for other services to pause because the following services failed to pause within the allowed time of {WaitTimeoutMinutesForPausingOtherServices} minutes: {servicesNotYetPaused}.";
                        if (servicesNotYetRegistered != string.Empty)
                        {
                            message += $" Note: the following services have not yet registered as being operational: {servicesNotYetRegistered}.";
                        }
                        logger.Info(message);
                        return false;
                    }
                    else
                    {
                        var message = $"{nameof(DatabaseMaintenanceService2)} waiting for the following services to pause: {servicesNotYetPaused}";
                        if (servicesNotYetRegistered != string.Empty)
                        {
                            message += $" Note: the following services have not yet registered as being operational: {servicesNotYetRegistered}.";
                        }
                        logger.Info(message);
                    }
                }
                else
                {
                    otherServicesPaused = true;
                }

                methodCancellationToken.ThrowIfCancellationRequested();
            }
            return true;
        }

        /// <summary>
        /// Reads every row of one of the Level 1 or Level 2 maintenance views.
        /// <para>
        /// The read is given the iteration's own token source, which has no deadline, and is bounded by its command timeout alone. <see cref="IBaseRepository{T}.GetAllAsync"/> checks the token it is given only once the read has finished, and each read used to be given a new token source that expired after TimeoutSecondsForDatabaseTasks. The view repositories now allow their one attempt the longest timeout there is, so with that token a read taking longer than TimeoutSecondsForDatabaseTasks would throw an <see cref="OperationCanceledException"/> after it had SUCCEEDED - and that exception ends this service. On SQL Server the same could already happen when a retry completed a read whose first attempt had timed out. A maintenance view is the kind of read that runs long: the SQL Server Level 2 view reads every index in the schema, and the PostgreSQL one calls pgstattuple on each.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The model type of the view's rows.</typeparam>
        /// <param name="viewRepo">The repository for the view.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns>The rows of the view.</returns>
        async Task<List<T>> ReadMaintenanceViewAsync<T>(IBaseRepository<T> viewRepo, CancellationTokenSource methodCancellationTokenSource) where T : class
        {
            var rows = new List<T>();
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                {
                    rows = (List<T>)await viewRepo.GetAllAsync(methodCancellationTokenSource);
                }
            }, new Context());
            return rows;
        }

        /// <summary>
        /// Rebuilds the SQL Server index identified in the <paramref name="dbvwStatForLevel2DBMaintenance_MSSQL"/>.
        /// </summary>
        /// <param name="dbvwStatForLevel2DBMaintenance_MSSQL">The <see cref="DbvwStatForLevel2DBMaintenance_MSSQL"/> from which the <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.SchemaName"/>, <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.TableName"/> and <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.IndexName"/> will be used when generating the SQL statement to rebuild the index.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task RebuildMSSQLIndexAsync(DbvwStatForLevel2DBMaintenance_MSSQL dbvwStatForLevel2DBMaintenance_MSSQL, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Ensure that the provider type is SQLServer.
            if (adapterContext.ProviderType != ConnectionInfo.DataAccessProviderType.SQLServer)
            {
                throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.");
            }

            // Validate identifiers for the schema, table and index names.
            if (!stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_MSSQL.TableName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_MSSQL.IndexName))
            {
                throw new Exception($"The schema name '{dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}' and/or table name '{dbvwStatForLevel2DBMaintenance_MSSQL.TableName}' and/or index name '{dbvwStatForLevel2DBMaintenance_MSSQL.IndexName}' are not valid identifiers for a database object.");
            }

            // Build the SQL statement.
            var sql = $"ALTER INDEX [{dbvwStatForLevel2DBMaintenance_MSSQL.IndexName}] ON [{dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}].[{dbvwStatForLevel2DBMaintenance_MSSQL.TableName}] REBUILD;";

            // Execute the function.
            var startTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await maintenanceStatementRepo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    throw;
                }
            }, new Context());
            var duration = DateTime.UtcNow - startTimeUtc;

            logger.Info($"Rebuilt index {dbvwStatForLevel2DBMaintenance_MSSQL.IndexName} on table {dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel2DBMaintenance_MSSQL.TableName} in {duration.TotalSeconds} seconds.");

            methodCancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Rebuilds the SQL Server index partition identified in the <paramref name="dbvwStatForLevel2DBMaintenance_MSSQL"/>.
        /// </summary>
        /// <param name="dbvwStatForLevel2DBMaintenance_MSSQL">The <see cref="DbvwStatForLevel2DBMaintenance_MSSQL"/> from which the <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.SchemaName"/>, <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.TableName"/>, <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.IndexName"/> and <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber"/> will be used when generating the SQL statement to rebuild the index partition.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task RebuildMSSQLIndexPartitionAsync(DbvwStatForLevel2DBMaintenance_MSSQL dbvwStatForLevel2DBMaintenance_MSSQL, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Ensure that the provider type is SQLServer.
            if (adapterContext.ProviderType != ConnectionInfo.DataAccessProviderType.SQLServer)
            {
                throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.");
            }

            // Validate identifiers for the schema, table and index names.
            if (!stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_MSSQL.TableName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_MSSQL.IndexName))
            {
                throw new Exception($"The schema name '{dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}' and/or table name '{dbvwStatForLevel2DBMaintenance_MSSQL.TableName}' and/or index name '{dbvwStatForLevel2DBMaintenance_MSSQL.IndexName}' are not valid identifiers for a database object.");
            }

            // Build the SQL statement.
            var sql = "";
            if (dbvwStatForLevel2DBMaintenance_MSSQL.TotalPartitions == 1)
            {
                // If there is only one partition, rebuild the entire index.
                sql = $"ALTER INDEX [{dbvwStatForLevel2DBMaintenance_MSSQL.IndexName}] ON [{dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}].[{dbvwStatForLevel2DBMaintenance_MSSQL.TableName}] REBUILD;";
            }
            else
            {
                // If there are multiple partitions, rebuild the specified partition.
                sql = $"ALTER INDEX [{dbvwStatForLevel2DBMaintenance_MSSQL.IndexName}] ON [{dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}].[{dbvwStatForLevel2DBMaintenance_MSSQL.TableName}] REBUILD PARTITION = {dbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber};";
            }

            // Execute the function.
            var startTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await maintenanceStatementRepo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    throw;
                }
            }, new Context());
            var duration = DateTime.UtcNow - startTimeUtc;

            if (dbvwStatForLevel2DBMaintenance_MSSQL.TotalPartitions == 1)
            {
                logger.Info($"Rebuilt index {dbvwStatForLevel2DBMaintenance_MSSQL.IndexName} on table {dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel2DBMaintenance_MSSQL.TableName} in {duration.TotalSeconds} seconds.");
            }
            else
            {
                logger.Info($"Rebuilt index {dbvwStatForLevel2DBMaintenance_MSSQL.IndexName} partition {dbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber} on table {dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel2DBMaintenance_MSSQL.TableName} in {duration.TotalSeconds} seconds.");
            }

            methodCancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Executes a REINDEX command on the PostgreSQL index identified in the <paramref name="dbvwStatForLevel2DBMaintenance_PG"/>.
        /// </summary>
        /// <param name="dbvwStatForLevel2DBMaintenance_PG">The <see cref="DbvwStatForLevel2DBMaintenance_PG"/> from which the <see cref="DbvwStatForLevel2DBMaintenance_PG.SchemaName"/> and <see cref="DbvwStatForLevel2DBMaintenance_PG.IndexName"/> will be used when generating the REINDEX SQL statement.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task ReindexPostgresIndexAsync(DbvwStatForLevel2DBMaintenance_PG dbvwStatForLevel2DBMaintenance_PG, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Ensure that the provider type is PostgreSQL.
            if (adapterContext.ProviderType != ConnectionInfo.DataAccessProviderType.PostgreSQL)
            {
                throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.");
            }

            // Validate identifiers for the schema and table names.
            if (!stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_PG.SchemaName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_PG.IndexName))
            {
                throw new Exception($"The schema name '{dbvwStatForLevel2DBMaintenance_PG.SchemaName}' and/or index name '{dbvwStatForLevel2DBMaintenance_PG.IndexName}' are not valid identifiers for a database object.");
            }

            // Build the SQL statement.
            var sql = $"REINDEX INDEX \"{dbvwStatForLevel2DBMaintenance_PG.SchemaName}\".\"{dbvwStatForLevel2DBMaintenance_PG.IndexName}\";";

            // Execute the function.
            var startTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await maintenanceStatementRepo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    throw;
                }
            }, new Context());
            var duration = DateTime.UtcNow - startTimeUtc;

            logger.Info($"Reindexed index {dbvwStatForLevel2DBMaintenance_PG.SchemaName}.{dbvwStatForLevel2DBMaintenance_PG.IndexName} in {duration.TotalSeconds} seconds.");

            methodCancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Reorganizes the SQL Server index partition identified in the <paramref name="dbvwStatForLevel2DBMaintenance_MSSQL"/>.
        /// </summary>
        /// <param name="dbvwStatForLevel2DBMaintenance_MSSQL">The <see cref="DbvwStatForLevel2DBMaintenance_MSSQL"/> from which the <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.SchemaName"/>, <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.TableName"/>, <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.IndexName"/> and <see cref="DbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber"/> will be used when generating the SQL statement to reorganize the index partition.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task ReorganizeMSSQLIndexPartitionAsync(DbvwStatForLevel2DBMaintenance_MSSQL dbvwStatForLevel2DBMaintenance_MSSQL, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Ensure that the provider type is SQLServer.
            if (adapterContext.ProviderType != ConnectionInfo.DataAccessProviderType.SQLServer)
            {
                throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.");
            }

            // Validate identifiers for the schema, table and index names.
            if (!stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_MSSQL.TableName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel2DBMaintenance_MSSQL.IndexName))
            {
                throw new Exception($"The schema name '{dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}' and/or table name '{dbvwStatForLevel2DBMaintenance_MSSQL.TableName}' and/or index name '{dbvwStatForLevel2DBMaintenance_MSSQL.IndexName}' are not valid identifiers for a database object.");
            }

            // Build the SQL statement.
            var sql = "";
            if (dbvwStatForLevel2DBMaintenance_MSSQL.TotalPartitions == 1)
            {
                // If there is only one partition, reorganize the entire index.
                sql = $"ALTER INDEX [{dbvwStatForLevel2DBMaintenance_MSSQL.IndexName}] ON [{dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}].[{dbvwStatForLevel2DBMaintenance_MSSQL.TableName}] REORGANIZE;";
            }
            else
            {
                // If there are multiple partitions, reorganize the specified partition.
                sql = $"ALTER INDEX [{dbvwStatForLevel2DBMaintenance_MSSQL.IndexName}] ON [{dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}].[{dbvwStatForLevel2DBMaintenance_MSSQL.TableName}] REORGANIZE PARTITION = {dbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber};";
            }

            // Execute the function.
            var startTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await maintenanceStatementRepo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    throw;
                }
            }, new Context());
            var duration = DateTime.UtcNow - startTimeUtc;

            logger.Info($"Reorganized index {dbvwStatForLevel2DBMaintenance_MSSQL.IndexName} partition {dbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber} on table {dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel2DBMaintenance_MSSQL.TableName} in {duration.TotalSeconds} seconds.");

            methodCancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Starts the current <see cref="DatabaseMaintenanceService2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DatabaseMaintenanceService2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DatabaseMaintenanceService2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            logger.Info($"******** STARTING SERVICE: {CurrentClassName}");
            await base.StartAsync(cancellationToken);
            await GetCurrentDBPartitionInfoAsync();
            await GetLatestDBMaintenanceLogsByTypeAsync();
        }

        /// <summary>
        /// Stops the current <see cref="DatabaseMaintenanceService2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Executes an UPDATE STATISTICS command on the SQL Server table identified in the <paramref name="dbvwStatForLevel1DBMaintenance_MSSQL"/>.
        /// </summary>
        /// <param name="dbvwStatForLevel1DBMaintenance_MSSQL">The <see cref="DbvwStatForLevel1DBMaintenance_MSSQL"/> from which the <see cref="DbvwStatForLevel1DBMaintenance_MSSQL.SchemaName"/> and <see cref="DbvwStatForLevel1DBMaintenance_MSSQL.TableName"/> will be used when generating the UPDATE STATISTICS statement.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        public async Task UpdateStatisticsForMSSQLTableAsync(DbvwStatForLevel1DBMaintenance_MSSQL dbvwStatForLevel1DBMaintenance_MSSQL, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Ensure that the provider type is SQLServer.
            if (adapterContext.ProviderType != ConnectionInfo.DataAccessProviderType.SQLServer)
            {
                throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.");
            }

            // Validate identifiers for the schema and table names.
            if (!stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel1DBMaintenance_MSSQL.SchemaName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel1DBMaintenance_MSSQL.TableName))
            {
                throw new Exception($"The schema name '{dbvwStatForLevel1DBMaintenance_MSSQL.SchemaName}' and/or table name '{dbvwStatForLevel1DBMaintenance_MSSQL.TableName}' are not valid identifiers for a database object.");
            }

            // Build the SQL statement.
            var sql = $"UPDATE STATISTICS [{dbvwStatForLevel1DBMaintenance_MSSQL.SchemaName}].[{dbvwStatForLevel1DBMaintenance_MSSQL.TableName}];";

            // Execute the function.
            var startTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await maintenanceStatementRepo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    throw;
                }
            }, new Context());
            var duration = DateTime.UtcNow - startTimeUtc;

            logger.Info($"Updated statistics on table {dbvwStatForLevel1DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel1DBMaintenance_MSSQL.TableName} in {duration.TotalSeconds} seconds.");

            methodCancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Executes a VACUUM command on the PostgreSQL table identified in the <paramref name="dbvwStatForLevel1DBMaintenance"/>.
        /// </summary>
        /// <param name="dbvwStatForLevel1DBMaintenance">The <see cref="DbvwStatForLevel1DBMaintenance_PG"/> from which the <see cref="DbvwStatForLevel1DBMaintenance_PG.SchemaName"/> and <see cref="DbvwStatForLevel1DBMaintenance_PG.TableName"/> will be used when generating the VACUUM SQL statement.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task VacuumPostgresTableAsync(DbvwStatForLevel1DBMaintenance_PG dbvwStatForLevel1DBMaintenance, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Ensure that the provider type is PostgreSQL.
            if (adapterContext.ProviderType != ConnectionInfo.DataAccessProviderType.PostgreSQL)
            {
                throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.");
            }

            // Validate identifiers for the schema and table names.
            if (!stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel1DBMaintenance.SchemaName) || !stringHelper.IsValidIdentifierForDatabaseObject(dbvwStatForLevel1DBMaintenance.TableName))
            {
                throw new Exception($"The schema name '{dbvwStatForLevel1DBMaintenance.SchemaName}' and/or table name '{dbvwStatForLevel1DBMaintenance.TableName}' are not valid identifiers for a database object.");
            }

            // Build the SQL statement.
            var sql = $"VACUUM \"{dbvwStatForLevel1DBMaintenance.SchemaName}\".\"{dbvwStatForLevel1DBMaintenance.TableName}\";";

            // Execute the function.
            var startTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForMaintenanceStatements.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await maintenanceStatementRepo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    throw;
                }
            }, new Context());
            var duration = DateTime.UtcNow - startTimeUtc;

            logger.Info($"Vacuumed table {dbvwStatForLevel1DBMaintenance.SchemaName}.{dbvwStatForLevel1DBMaintenance.TableName} in {duration.TotalSeconds} seconds.");

            methodCancellationToken.ThrowIfCancellationRequested();
        }
    }
}
