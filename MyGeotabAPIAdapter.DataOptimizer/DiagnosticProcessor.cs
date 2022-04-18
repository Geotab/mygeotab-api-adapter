﻿using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that handles ETL processing of Diagnostic data from the Adapter database to the Optimizer database. 
    /// </summary>
    class DiagnosticProcessor : BackgroundService
    {
        string AssemblyName { get => GetType().Assembly.GetName().Name; }
        string AssemblyVersion { get => GetType().Assembly.GetName().Version.ToString(); }
        static string CurrentClassName { get => nameof(DiagnosticProcessor); }
        static string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }
        static int ThrottleEngagingBatchRecordCount { get => 1; }

        readonly IAdapterDatabaseObjectNames adapterDatabaseObjectNames;
        readonly IConnectionInfoContainer connectionInfoContainer;
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IDbDiagnosticDbDiagnosticIdTEntityMapper dbDiagnosticDbDiagnosticIdTEntityMapper;
        readonly IDbDiagnosticDbDiagnosticTEntityMapper dbDiagnosticDbDiagnosticTEntityMapper;
        readonly IGenericEntityPersister<DbDiagnosticIdT> dbDiagnosticIdTEntityPersister;
        readonly IGenericEntityPersister<DbDiagnosticT> dbDiagnosticTEntityPersister;
        readonly IGenericDbObjectCache<DbDiagnostic> dbDiagnosticObjectCache;
        readonly DbDiagnosticIdTObjectCache dbDiagnosticIdTObjectCache;
        readonly IGenericDbObjectCache<DbDiagnosticT> dbDiagnosticTObjectCache;
        readonly IExceptionHelper exceptionHelper;
        readonly IMessageLogger messageLogger;
        readonly IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames;
        readonly IOptimizerEnvironment optimizerEnvironment;
        readonly IProcessorTracker processorTracker;
        readonly IStateMachine stateMachine;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly UnitOfWorkContext adapterContext;
        readonly UnitOfWorkContext optimizerContext;

        /// <summary>
        /// The last time a call was initiated to retrieve records from the DbDiagnostics table in the Adapter database.
        /// </summary>
        DateTime DbDiagnosticsLastQueriedUtc { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticProcessor"/> class.
        /// </summary>
        public DiagnosticProcessor(IDataOptimizerConfiguration dataOptimizerConfiguration, IOptimizerDatabaseObjectNames optimizerDatabaseObjectNames, IAdapterDatabaseObjectNames adapterDatabaseObjectNames, IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IMessageLogger messageLogger, IOptimizerEnvironment optimizerEnvironment, IStateMachine stateMachine, IConnectionInfoContainer connectionInfoContainer, IProcessorTracker processorTracker, IDbDiagnosticDbDiagnosticIdTEntityMapper dbDiagnosticDbDiagnosticIdTEntityMapper, IDbDiagnosticDbDiagnosticTEntityMapper dbDiagnosticDbDiagnosticTEntityMapper, IGenericEntityPersister<DbDiagnosticIdT> dbDiagnosticIdTEntityPersister, IGenericEntityPersister<DbDiagnosticT> dbDiagnosticTEntityPersister, IGenericDbObjectCache<DbDiagnostic> dbDiagnosticObjectCache, DbDiagnosticIdTObjectCache dbDiagnosticIdTObjectCache, IGenericDbObjectCache<DbDiagnosticT> dbDiagnosticTObjectCache, UnitOfWorkContext adapterContext, UnitOfWorkContext optimizerContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dataOptimizerConfiguration = dataOptimizerConfiguration;
            this.optimizerDatabaseObjectNames = optimizerDatabaseObjectNames;
            this.adapterDatabaseObjectNames = adapterDatabaseObjectNames;
            this.exceptionHelper = exceptionHelper;
            this.messageLogger = messageLogger;
            this.optimizerEnvironment = optimizerEnvironment;
            this.dateTimeHelper = dateTimeHelper;
            this.stateMachine = stateMachine;
            this.connectionInfoContainer = connectionInfoContainer;
            this.processorTracker = processorTracker;
            this.dbDiagnosticDbDiagnosticIdTEntityMapper = dbDiagnosticDbDiagnosticIdTEntityMapper;
            this.dbDiagnosticDbDiagnosticTEntityMapper = dbDiagnosticDbDiagnosticTEntityMapper;
            this.dbDiagnosticIdTEntityPersister = dbDiagnosticIdTEntityPersister;
            this.dbDiagnosticTEntityPersister = dbDiagnosticTEntityPersister;
            this.dbDiagnosticObjectCache = dbDiagnosticObjectCache;
            this.dbDiagnosticIdTObjectCache = dbDiagnosticIdTObjectCache;
            this.dbDiagnosticTObjectCache = dbDiagnosticTObjectCache;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(UnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            this.optimizerContext = optimizerContext;
            logger.Debug($"{nameof(UnitOfWorkContext)} [Id: {optimizerContext.Id}] associated with {CurrentClassName}.");

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
                // If configured to operate on a schedule and the present time is currently outside of an operating window, delay until the next daily start time.
                if (dataOptimizerConfiguration.DiagnosticProcessorOperationMode == OperationMode.Scheduled)
                {
                    var timeSpanToNextDailyStartTimeUTC = dateTimeHelper.GetTimeSpanToNextDailyStartTimeUTC(dataOptimizerConfiguration.DiagnosticProcessorDailyStartTimeUTC, dataOptimizerConfiguration.DiagnosticProcessorDailyRunTimeSeconds);
                    if (timeSpanToNextDailyStartTimeUTC != TimeSpan.Zero)
                    {
                        DateTime nextScheduledStartTimeUTC = DateTime.UtcNow.Add(timeSpanToNextDailyStartTimeUTC);
                        messageLogger.LogScheduledServicePause(CurrentClassName, dataOptimizerConfiguration.DiagnosticProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.DiagnosticProcessorDailyRunTimeSeconds, nextScheduledStartTimeUTC);

                        await Task.Delay(timeSpanToNextDailyStartTimeUTC, stoppingToken);

                        DateTime nextScheduledPauseTimeUTC = DateTime.UtcNow.Add(TimeSpan.FromSeconds(dataOptimizerConfiguration.DiagnosticProcessorDailyRunTimeSeconds));
                        messageLogger.LogScheduledServiceResumption(CurrentClassName, dataOptimizerConfiguration.DiagnosticProcessorDailyStartTimeUTC.TimeOfDay, dataOptimizerConfiguration.DiagnosticProcessorDailyRunTimeSeconds, nextScheduledPauseTimeUTC);
                    }
                }

                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    continue;
                }

                // Abort if the configured execution interval has not elapsed since the last time this method was executed.
                var DiagnosticProcessorInfo = await processorTracker.GetDiagnosticProcessorInfoAsync();
                if (DiagnosticProcessorInfo.EntitiesHaveBeenProcessed && !dateTimeHelper.TimeIntervalHasElapsed((DateTime)DiagnosticProcessorInfo.EntitiesLastProcessedUtc, DateTimeIntervalType.Seconds, dataOptimizerConfiguration.DiagnosticProcessorExecutionIntervalSeconds))
                {
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var engageExecutionThrottle = true;
                        var processorTrackingInfoUpdated = false;
                        DbDiagnosticsLastQueriedUtc = DateTime.UtcNow;

                        // Initialize object caches.
                        if (dbDiagnosticObjectCache.IsInitialized == false)
                        {
                            await dbDiagnosticObjectCache.InitializeAsync(Databases.AdapterDatabase);
                        }
                        if (dbDiagnosticTObjectCache.IsInitialized == false)
                        {
                            await dbDiagnosticTObjectCache.InitializeAsync(Databases.OptimizerDatabase);
                        }
                        if (dbDiagnosticIdTObjectCache.IsInitialized == false)
                        {
                            await dbDiagnosticIdTObjectCache.InitializeAsync(Databases.OptimizerDatabase);
                        }

                        if (dbDiagnosticObjectCache.Any())
                        {
#nullable enable
                            long? adapterDbLastId = null;
                            string? adapterDbLastGeotabId = null;
                            DateTime? adapterDbLastRecordCreationTimeUtc = null;
#nullable disable
                            // Get the subset of DbDiagnostics that were added or changed since the last time DbDiagnostics were processed.
                            var changedSince = (DateTime)DiagnosticProcessorInfo.EntitiesLastProcessedUtc;
                            var changedDbDiagnostics = await dbDiagnosticObjectCache.GetObjectsAsync(changedSince);
                            if (changedDbDiagnostics.Any())
                            {
                                engageExecutionThrottle = changedDbDiagnostics.Count < ThrottleEngagingBatchRecordCount;
                                var dbDiagnosticTsToPersist = new List<DbDiagnosticT>();
                                var dbDiagnosticIdTsToPersist = new List<DbDiagnosticIdT>();

                                // Iterate through the list of added/changed DbDiagnostics.
                                foreach (var changedDbDiagnostic in changedDbDiagnostics)
                                {
                                    // Try to get the DbDiagnosticT that corresponds with the DbDiagnostic based on matching the GeotabGUID.
                                    var dbDiagnosticT = await dbDiagnosticTObjectCache.GetObjectAsync(changedDbDiagnostic.GeotabGUID);
                                    if (dbDiagnosticT == null)
                                    {
                                        // The DbDiagnosticT doesn't yet exist. Create a new one along with a corresponding DbDiagnosticIdT.
                                        dbDiagnosticT = dbDiagnosticDbDiagnosticTEntityMapper.CreateEntity(changedDbDiagnostic);
                                        dbDiagnosticTsToPersist.Add(dbDiagnosticT);
                                        var dbDiagnosticIdT = dbDiagnosticDbDiagnosticIdTEntityMapper.CreateEntity(changedDbDiagnostic);
                                        dbDiagnosticIdTsToPersist.Add(dbDiagnosticIdT);

                                        adapterDbLastId = changedDbDiagnostic.id;
                                        adapterDbLastGeotabId = changedDbDiagnostic.GeotabId;
                                        adapterDbLastRecordCreationTimeUtc = changedDbDiagnostic.RecordLastChangedUtc;
                                    }
                                    else
                                    {
                                        // The DbDiagnosticT already exists. Update the existing DbDiagnosticT.
                                        dbDiagnosticDbDiagnosticTEntityMapper.UpdateEntity(dbDiagnosticT, changedDbDiagnostic);
                                        dbDiagnosticTsToPersist.Add(dbDiagnosticT);

                                        // Check to see whether the Diagnostic Id has changed and, if so, add a new DbDiagnosticIdT.
                                        var dbDiagnosticIdTs = await dbDiagnosticIdTObjectCache.GetObjectsAsync(changedDbDiagnostic.GeotabGUID, changedDbDiagnostic.GeotabId);
                                        if (dbDiagnosticIdTs.Any() == false)
                                        {
                                            var dbDiagnosticIdT = dbDiagnosticDbDiagnosticIdTEntityMapper.CreateEntity(changedDbDiagnostic);
                                            dbDiagnosticIdTsToPersist.Add(dbDiagnosticIdT);
                                        }
                                    }
                                }

                                // Persist changes to database.
                                using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                                {
                                    try
                                    {
                                        // DbDiagnosticT:
                                        await dbDiagnosticTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbDiagnosticTsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        // DbDiagnosticIdT:
                                        await dbDiagnosticIdTEntityPersister.PersistEntitiesToDatabaseAsync(optimizerContext, dbDiagnosticIdTsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        // DbOProcessorTracking:
                                        await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.DiagnosticProcessor, DbDiagnosticsLastQueriedUtc, adapterDbLastId, adapterDbLastRecordCreationTimeUtc, adapterDbLastGeotabId);

                                        // Commit transactions:
                                        await optimizerUOW.CommitAsync();
                                        processorTrackingInfoUpdated = true;
                                    }
                                    catch (Exception)
                                    {
                                        await optimizerUOW.RollBackAsync();
                                        throw;
                                    }
                                }
                                // Force the DbDiagnosticT cache to be updated so that the changes are immediately available to other consumers.
                                await dbDiagnosticTObjectCache.UpdateAsync(true);
                            }
                            else
                            {
                                logger.Debug($"There are no new or changed records in the {adapterDatabaseObjectNames.DbDiagnosticTableName} table in the {adapterDatabaseObjectNames.AdapterDatabaseNickname} database since the last check.");
                            }
                        }
                        else
                        {
                            logger.Debug($"No records were returned from the {adapterDatabaseObjectNames.DbDiagnosticTableName} table in the {adapterDatabaseObjectNames.AdapterDatabaseNickname} database.");
                        }

                        // Update processor tracking info if not already done.
                        if (processorTrackingInfoUpdated == false)
                        {
                            using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
                            {
                                try
                                {
                                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.DiagnosticProcessor, DbDiagnosticsLastQueriedUtc, null, null, null);
                                    await optimizerUOW.CommitAsync();
                                }
                                catch (Exception)
                                {
                                    await optimizerUOW.RollBackAsync();
                                    throw;
                                }
                            }
                        }

                        // If necessary, add a delay to implement the configured execution interval.
                        if (engageExecutionThrottle == true)
                        {
                            var delayTimeSpan = TimeSpan.FromSeconds(dataOptimizerConfiguration.DiagnosticProcessorExecutionIntervalSeconds);
                            logger.Info($"{CurrentClassName} pausing for {delayTimeSpan} because fewer than {ThrottleEngagingBatchRecordCount} records were processed during the current execution interval.");
                            await Task.Delay(delayTimeSpan, stoppingToken);
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
                catch (OptimizerDatabaseConnectionException optimizerDatabaseConnectionException)
                {
                    HandleException(optimizerDatabaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                }
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
            else if (exception is OptimizerDatabaseConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.OptimizerDatabaseNotAvailable);
            }

            if (logLevel == NLogLogLevelName.Fatal)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
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

            var dbOProcessorTrackings = await processorTracker.GetDbOProcessorTrackingListAsync();
            optimizerEnvironment.ValidateOptimizerEnvironment(dbOProcessorTrackings, DataOptimizerProcessor.DiagnosticProcessor);
            using (var optimizerUOW = optimizerContext.CreateUnitOfWork(Databases.OptimizerDatabase))
            {
                try
                {
                    await processorTracker.UpdateDbOProcessorTrackingRecord(optimizerContext, DataOptimizerProcessor.DiagnosticProcessor, optimizerEnvironment.OptimizerVersion.ToString(), optimizerEnvironment.OptimizerMachineName);
                    await optimizerUOW.CommitAsync();
                }
                catch (Exception)
                {
                    await optimizerUOW.RollBackAsync();
                    throw;
                }
            }

            // Only start this service if it has been configured to be enabled.
            if (dataOptimizerConfiguration.EnableDiagnosticProcessor == true)
            {
                logger.Info($"******** STARTING SERVICE: {AssemblyName}.{CurrentClassName} (v{AssemblyVersion})");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {AssemblyName}.{CurrentClassName} service has not been enabled and will NOT be started.");
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

            logger.Info($"******** STOPPED SERVICE: {AssemblyName}.{CurrentClassName} (v{AssemblyVersion}) ********");
            return base.StopAsync(cancellationToken);
        }
    }
}
