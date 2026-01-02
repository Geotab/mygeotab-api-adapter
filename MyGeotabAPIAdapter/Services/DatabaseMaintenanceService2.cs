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

        // Thresholds for Postgres database maintenance tasks. These should match the thresholds set in the database view vwStatsForDBMaintenance.
        const double MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate = 0.1;
        const double MSSQL_ReorganizeThreshold_FragmentationPct = 10;
        const double MSSQL_ReindexThreshold_FragmentationPct = 30;
        const double MSSQL_ReindexByTableThreshold_HighFragPartitionRatio = 0.5;
        const double PG_VacuumThreshold_DeadTupleRatio = 0.2;
        const int PG_VacuumThreshold_DeadTuplesCount = 1000;
        const double PG_AnalyzeThreshold_ModsSinceLastAnalyzeRatio = 0.1;
        const double PG_ReindexThreshold_BloatRatio = 0.3;
        const int PG_ReindexThreshold_IndexSizeBytes = 1000;
        const int WaitTimeoutMinutesForPausingOtherServices = 5;

        bool initialPartitioningOnStartupCompleted = false;
        int serviceExecutionIntervalMinutes = 5;

        // Polly-related items:
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IBaseRepository<DbDBMaintenanceLog2> dbDBMaintenanceLog2Repo;
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
            dbvwStatForLevel1DBMaintenance_PGRepo = new BaseRepository<DbvwStatForLevel1DBMaintenance_PG>(adapterContext);
            dbvwStatForLevel2DBMaintenance_PGRepo = new BaseRepository<DbvwStatForLevel2DBMaintenance_PG>(adapterContext);
            dbvwStatForLevel1DBMaintenance_MSSQLRepo = new BaseRepository<DbvwStatForLevel1DBMaintenance_MSSQL>(adapterContext);
            dbvwStatForLevel2DBMaintenance_MSSQLRepo = new BaseRepository<DbvwStatForLevel2DBMaintenance_MSSQL>(adapterContext);

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
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
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await dbDBMaintenanceLog2Repo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
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
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<DbDBMaintenanceLog2> ExecuteLevel1DatabaseMaintenanceIfNeededAsync(CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            DbDBMaintenanceLog2 dbDBMaintenanceLog2 = null;

            // Execute Level 1 maintenance if it is enabled and the interval since the last run has elapsed.
            if (adapterConfiguration.EnableLevel1DatabaseMaintenance == true && (LatestLevel1DbDBMaintenanceLog2 == null || (LatestLevel1DbDBMaintenanceLog2.EndTimeUtc != null && (DateTime.UtcNow - LatestLevel1DbDBMaintenanceLog2.StartTimeUtc).TotalMinutes >= adapterConfiguration.Level1DatabaseMaintenanceIntervalMinutes)))
            {
                logger.Info($"Starting Level 1 database maintenance.");

                var startTimeUtc = DateTime.UtcNow;

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
            var dbvwStatForLevel1DBMaintenance_MSSQLs = new List<DbvwStatForLevel1DBMaintenance_MSSQL>();
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                {
                    dbvwStatForLevel1DBMaintenance_MSSQLs = (List<DbvwStatForLevel1DBMaintenance_MSSQL>)await dbvwStatForLevel1DBMaintenance_MSSQLRepo.GetAllAsync(new CancellationTokenSource(TimeSpan.FromSeconds(adapterConfiguration.TimeoutSecondsForDatabaseTasks)));
                }
            }, new Context());

            if (dbvwStatForLevel1DBMaintenance_MSSQLs.Count != 0)
            {
                logger.Info($"Found {dbvwStatForLevel1DBMaintenance_MSSQLs.Count} tables that may need Level 1 maintenance.");
                logger.Info($"> Statistics will be updated on tables where the modification ratio since last statistics update exceeds {MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate}.");

                // Perform the necessary Level 1 maintanance steps for each table in need.
                foreach (var dbvwStatForLevel1DBMaintenance_MSSQL in dbvwStatForLevel1DBMaintenance_MSSQLs)
                {
                    methodCancellationToken.ThrowIfCancellationRequested();

                    if (dbvwStatForLevel1DBMaintenance_MSSQL.PctModsSinceLastStatsUpdate > MSSQL_UpdateStatisticsThreshold_PctModsSinceLastStatsUpdate)
                    {
                        logger.Info($"Updating statistics for table {dbvwStatForLevel1DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel1DBMaintenance_MSSQL.TableName} [modification ratio since last statistics update:  {dbvwStatForLevel1DBMaintenance_MSSQL.PctModsSinceLastStatsUpdate}].");
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
            var dbvwStatForLevel1DBMaintenance_PGs = new List<DbvwStatForLevel1DBMaintenance_PG>();
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                {
                    dbvwStatForLevel1DBMaintenance_PGs = (List<DbvwStatForLevel1DBMaintenance_PG>)await dbvwStatForLevel1DBMaintenance_PGRepo.GetAllAsync(new CancellationTokenSource(TimeSpan.FromSeconds(adapterConfiguration.TimeoutSecondsForDatabaseTasks)));
                }
            }, new Context());

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
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<DbDBMaintenanceLog2> ExecuteLevel2DatabaseMaintenanceIfNeededAsync(CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            DbDBMaintenanceLog2 dbDBMaintenanceLog2 = null;

            // Calculate the start and end times of the maintenance window for today.
            var currentDateTimeUtc = DateTime.UtcNow;
            var todaysLevel2DbMaintenanceWindowStartTimeUtc = currentDateTimeUtc.Date + adapterConfiguration.Level2DatabaseMaintenanceWindowStartTimeUTC.TimeOfDay;
            var todaysLevel2DbMaintenanceWindowEndTimeUtc = todaysLevel2DbMaintenanceWindowStartTimeUtc.AddMinutes(adapterConfiguration.Level2DatabaseMaintenanceWindowMaxMinutes);

            // Calculate metrics to determine if Level 2 maintenance should be executed.
            var level2DbMaintenanceEnabled = adapterConfiguration.EnableLevel2DatabaseMaintenance;
            var level2DbMaintenanceWindowEnabled = adapterConfiguration.EnableLevel2DatabaseMaintenanceWindow;
            var level2DbMaintenanceIntervalElapsed = LatestLevel2DbDBMaintenanceLog2 == null || (LatestLevel2DbDBMaintenanceLog2.EndTimeUtc != null && (currentDateTimeUtc - LatestLevel2DbDBMaintenanceLog2.StartTimeUtc).TotalMinutes >= adapterConfiguration.Level2DatabaseMaintenanceIntervalMinutes);
            var withinTodaysMaintenanceWindow = currentDateTimeUtc >= todaysLevel2DbMaintenanceWindowStartTimeUtc && currentDateTimeUtc <= todaysLevel2DbMaintenanceWindowEndTimeUtc;

            var executeLevel2DbMaintenance = false;
            if (level2DbMaintenanceEnabled && level2DbMaintenanceIntervalElapsed && level2DbMaintenanceWindowEnabled && withinTodaysMaintenanceWindow)
            {
                executeLevel2DbMaintenance = true;
            }
            else if (level2DbMaintenanceEnabled && level2DbMaintenanceIntervalElapsed && !level2DbMaintenanceWindowEnabled)
            {
                executeLevel2DbMaintenance = true;
            }

            // If Level 2 maintenance is enabled and hasn't been run today and the current time is within the maintenance window, execute Level 2 maintenance.
            if (executeLevel2DbMaintenance)
            {
                try
                {
                    // Trigger a pause and wait for the other services to pause before proceeding with database maintenance. If the other services don't pause within the timeout period, don't proceed with Level 2 database maintenance.
                    var otherServicesPaused = await PauseOtherServicesForDatabaseMaintenanceAsync(methodCancellationTokenSource);
                    if (otherServicesPaused == false)
                    {
                        logger.Warn($"Skipping Level 2 database maintenance because other services did not pause within the timeout period of {WaitTimeoutMinutesForPausingOtherServices} minutes.");
                        return null;
                    }

                    logger.Info($"Starting Level 2 database maintenance.");

                    var startTimeUtc = DateTime.UtcNow;

                    // Level 2 maintenance steps differ between database types.
                    switch (adapterContext.ProviderType)
                    {
                        // Postgres:
                        case ConnectionInfo.DataAccessProviderType.PostgreSQL:
                            await ExecuteLevel2DatabaseMaintenance_PG_Async(todaysLevel2DbMaintenanceWindowEndTimeUtc, methodCancellationTokenSource);
                            break;
                        // SQL Server:
                        case ConnectionInfo.DataAccessProviderType.SQLServer:
                            await ExecuteLevel2DatabaseMaintenance_MSSQL_Async(todaysLevel2DbMaintenanceWindowEndTimeUtc, methodCancellationTokenSource);
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
        /// <param name="todaysLevel2DbMaintenanceWindowEndTimeUtc">The end time of the maintenance window for today.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ExecuteLevel2DatabaseMaintenance_MSSQL_Async(DateTime todaysLevel2DbMaintenanceWindowEndTimeUtc, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Get a list of indexes that need to be reorganized or reindexed (based on the thresholds in the database view).
            var dbvwStatForLevel2DBMaintenance_MSSQLs = new List<DbvwStatForLevel2DBMaintenance_MSSQL>();
            var indexRetrievalStartTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                {
                    dbvwStatForLevel2DBMaintenance_MSSQLs = (List<DbvwStatForLevel2DBMaintenance_MSSQL>)await dbvwStatForLevel2DBMaintenance_MSSQLRepo.GetAllAsync(new CancellationTokenSource(TimeSpan.FromSeconds(adapterConfiguration.TimeoutSecondsForDatabaseTasks)));
                }
            }, new Context());
            var indexRetrievalDuration = DateTime.UtcNow - indexRetrievalStartTimeUtc;

            if (dbvwStatForLevel2DBMaintenance_MSSQLs.Count != 0)
            {
                logger.Info($"Found {dbvwStatForLevel2DBMaintenance_MSSQLs.Count} indexes that may need Level 2 maintenance. Duration: {indexRetrievalDuration}");
                logger.Info($"> Indexes will be rebuilt where the high fragmentation partition ratio exceeds {MSSQL_ReindexByTableThreshold_HighFragPartitionRatio}.");
                logger.Info($"> Index partitions will be rebuilt where the fragmentation percentage exceeds {MSSQL_ReindexThreshold_FragmentationPct}.");
                logger.Info($"> Index partitions will be reorganized where the fragmentation percentage exceeds {MSSQL_ReorganizeThreshold_FragmentationPct}.");

                // Perform the necessary Level 2 maintanance steps for each index in need.
                var lastRebuiltIndexName = string.Empty;
                foreach (var dbvwStatForLevel2DBMaintenance_MSSQL in dbvwStatForLevel2DBMaintenance_MSSQLs)
                {
                    methodCancellationToken.ThrowIfCancellationRequested();

                    // Stop reindexing if the current time is past the maintenance window end time.
                    if (DateTime.UtcNow > todaysLevel2DbMaintenanceWindowEndTimeUtc)
                    {
                        logger.Info($"Skipping processing of remaining indexes because current time is past the maintenance window end time of {todaysLevel2DbMaintenanceWindowEndTimeUtc}.");
                        break;
                    }

                    // If an entire index was rebuilt, skip any index partitions since they will have been included in the rebuild.
                    if (dbvwStatForLevel2DBMaintenance_MSSQL.IndexName == lastRebuiltIndexName)
                    { 
                        continue;
                    }

                    if (dbvwStatForLevel2DBMaintenance_MSSQL.PctHighFragPartitions > MSSQL_ReindexByTableThreshold_HighFragPartitionRatio)
                    {
                        logger.Info($"Rebuilding index {dbvwStatForLevel2DBMaintenance_MSSQL.IndexName} on table {dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel2DBMaintenance_MSSQL.TableName} [high fragmentation partition ratio: {dbvwStatForLevel2DBMaintenance_MSSQL.PctHighFragPartitions}].");
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
        /// <param name="todaysLevel2DbMaintenanceWindowEndTimeUtc">The end time of the maintenance window for today.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ExecuteLevel2DatabaseMaintenance_PG_Async(DateTime todaysLevel2DbMaintenanceWindowEndTimeUtc, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            // Get a list of indexes that need to be reindexed (based on the thresholds in the database view).
            var dbvwStatForLevel2DBMaintenance_PGs = new List<DbvwStatForLevel2DBMaintenance_PG>();
            var indexRetrievalStartTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                {
                    dbvwStatForLevel2DBMaintenance_PGs = (List<DbvwStatForLevel2DBMaintenance_PG>)await dbvwStatForLevel2DBMaintenance_PGRepo.GetAllAsync(new CancellationTokenSource(TimeSpan.FromSeconds(adapterConfiguration.TimeoutSecondsForDatabaseTasks)));
                }
            }, new Context());
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
                    if (DateTime.UtcNow > todaysLevel2DbMaintenanceWindowEndTimeUtc)
                    {
                        logger.Info($"Skipping processing of remaining indexes because current time is past the maintenance window end time of {todaysLevel2DbMaintenanceWindowEndTimeUtc}.");
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
                        dbDBPartitionInfo2s = (List<DbDBPartitionInfo2>)await dbDBPartitionInfo2Repo.GetAllAsync(new CancellationTokenSource(TimeSpan.FromSeconds(adapterConfiguration.TimeoutSecondsForDatabaseTasks)));
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
                        dbDBMaintenanceLog2s = (List<DbDBMaintenanceLog2>)await dbDBMaintenanceLog2Repo.GetAllAsync(new CancellationTokenSource(TimeSpan.FromSeconds(adapterConfiguration.TimeoutSecondsForDatabaseTasks)));
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
        /// Executes the database partition function if it has never been run or if the interval (30 days) since it was last run has elapsed.
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<DbDBMaintenanceLog2> PartitionDatabaseIfNeededAsync(CancellationTokenSource methodCancellationTokenSource)
        {
            const double DatabasePartitioningIntervalDays = 20;
            const string PartitionFunctionSQL_Postgres = @"SELECT public.""spManagePartitions""(@MinDateTimeUTC::timestamp without time zone, @PartitionInterval::text);";
            const string PartitionProcedureSQL_SQLServer = "EXEC [dbo].[spManagePartitions] @MinDateTimeUTC = @MinDateTimeUTC, @PartitionInterval = @PartitionInterval;";

            CancellationToken methodCancellationToken = methodCancellationTokenSource.Token;

            DbDBMaintenanceLog2 dbDBMaintenanceLog2 = null;

            // Partitioning needs to happen regardless of whether Level 1 or Level 2 maintenance are enabled. Otherwise, new data that doesn't fit into any existing partition will be inserted into the default partition, which defeats the purpose of partitioning.
            if (initialPartitioningOnStartupCompleted == false || LatestPartitionDbDBMaintenanceLog2 == null ||
                (LatestPartitionDbDBMaintenanceLog2.EndTimeUtc != null &&
                (DateTime.UtcNow - LatestPartitionDbDBMaintenanceLog2.StartTimeUtc).TotalDays >= DatabasePartitioningIntervalDays))
            {
                try
                {
                    // On initial application start up, partitioning needs to happen before any other services write data to tables. This is to avoid data being written to the default partition when it should in fact be written to a yet-to-be-created partition (this could occur if other services started before the partitioning function is executed). Since other services all depend on this DatabaseMaintenanceService2 to be running before they start processing, it is necessary to run the initial partitioning logic without waiting for them. On subsequent iterations, wait for the other services to pause before executing database maintenance.
                    bool otherServicesPaused = false;
                    if (initialPartitioningOnStartupCompleted == false)
                    {
                        otherServicesPaused = true;
                    }
                    else 
                    {
                        // Set this to true so that all subsequent iterations will actually pause for other services.
                        initialPartitioningOnStartupCompleted = true;

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

                    var startTimeUtc = DateTime.UtcNow;

                    // Execute the partition function.
                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        try
                        {
                            await dbDBMaintenanceLog2Repo.ExecuteAsync(sql, parameters, methodCancellationTokenSource, true, adapterContext);
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

                    methodCancellationToken.ThrowIfCancellationRequested();
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
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await dbDBMaintenanceLog2Repo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
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
            var sql = $"ALTER INDEX [{dbvwStatForLevel2DBMaintenance_MSSQL.IndexName}] ON [{dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}].[{dbvwStatForLevel2DBMaintenance_MSSQL.TableName}] REBUILD PARTITION = {dbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber};";

            // Execute the function.
            var startTimeUtc = DateTime.UtcNow;
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await dbDBMaintenanceLog2Repo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    throw;
                }
            }, new Context());
            var duration = DateTime.UtcNow - startTimeUtc;

            logger.Info($"Rebuilt index {dbvwStatForLevel2DBMaintenance_MSSQL.IndexName} partition {dbvwStatForLevel2DBMaintenance_MSSQL.PartitionNumber} on table {dbvwStatForLevel2DBMaintenance_MSSQL.SchemaName}.{dbvwStatForLevel2DBMaintenance_MSSQL.TableName} in {duration.TotalSeconds} seconds.");

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
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await dbDBMaintenanceLog2Repo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
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
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await dbDBMaintenanceLog2Repo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
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
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await dbDBMaintenanceLog2Repo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
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
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                try
                {
                    await dbDBMaintenanceLog2Repo.ExecuteAsync(sql, null, methodCancellationTokenSource, true, adapterContext);
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
