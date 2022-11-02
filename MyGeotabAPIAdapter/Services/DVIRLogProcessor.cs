using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="DVIRLog"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class DVIRLogProcessor : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        DateTime defectListPartDefectCacheExpiryTime = DateTime.MinValue;
        readonly IBaseRepository<DbDVIRDefect> dbDVIRDefectRepo;
        readonly IBaseRepository<DbDVIRDefectRemark> dbDVIRDefectRemarkRepo;
        readonly IBaseRepository<DbDVIRLog> dbDVIRLogRepo;
        readonly IDictionary<Id, DefectListPartDefect> defectListPartDefectsDictionary;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment adapterEnvironment;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbDVIRDefect> dbDVIRDefectEntityPersister;
        readonly IGenericEntityPersister<DbDVIRDefectRemark> dbDVIRDefectRemarkEntityPersister;
        readonly IGenericEntityPersister<DbDVIRLog> dbDVIRLogEntityPersister;
        readonly IGenericGeotabObjectFeeder<DVIRLog> dvirLogGeotabObjectFeeder;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabDVIRDefectDbDVIRDefectObjectMapper geotabDVIRDefectDbDVIRDefectObjectMapper;
        readonly IGeotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper geotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper;
        readonly IGeotabDVIRLogDbDVIRLogObjectMapper geotabDVIRLogDbDVIRLogObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IPrerequisiteServiceChecker prerequisiteServiceChecker;
        readonly IServiceTracker serviceTracker;
        readonly IStateMachine stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DVIRLogProcessor"/> class.
        /// </summary>
        public DVIRLogProcessor(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment adapterEnvironment, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbDVIRDefect> dbDVIRDefectEntityPersister, IGenericEntityPersister<DbDVIRDefectRemark> dbDVIRDefectRemarkEntityPersister, IGenericEntityPersister<DbDVIRLog> dbDVIRLogEntityPersister, IGenericGeotabObjectFeeder<DVIRLog> dvirLogGeotabObjectFeeder, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabDVIRDefectDbDVIRDefectObjectMapper geotabDVIRDefectDbDVIRDefectObjectMapper, IGeotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper geotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper, IGeotabDVIRLogDbDVIRLogObjectMapper geotabDVIRLogDbDVIRLogObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IPrerequisiteServiceChecker prerequisiteServiceChecker, IServiceTracker serviceTracker, IStateMachine stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.exceptionHelper = exceptionHelper;
            this.dbDVIRDefectEntityPersister = dbDVIRDefectEntityPersister;
            this.dbDVIRDefectRemarkEntityPersister = dbDVIRDefectRemarkEntityPersister;
            this.dbDVIRLogEntityPersister = dbDVIRLogEntityPersister;
            this.dvirLogGeotabObjectFeeder = dvirLogGeotabObjectFeeder;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.geotabDVIRDefectDbDVIRDefectObjectMapper = geotabDVIRDefectDbDVIRDefectObjectMapper;
            this.geotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper = geotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper;
            this.geotabDVIRLogDbDVIRLogObjectMapper = geotabDVIRLogDbDVIRLogObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.prerequisiteServiceChecker = prerequisiteServiceChecker;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            dbDVIRLogRepo = new BaseRepository<DbDVIRLog>(adapterContext);
            dbDVIRDefectRepo = new BaseRepository<DbDVIRDefect>(adapterContext);
            dbDVIRDefectRemarkRepo = new BaseRepository<DbDVIRDefectRemark>(adapterContext);
            defectListPartDefectsDictionary = new Dictionary<Id, DefectListPartDefect>();

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(MaxRetries, logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Creates a <see cref="DefectListPartDefect"/> using the input parameter values and adds it to the <paramref name="defectListPartDefectsDictionary"/>.
        /// </summary>
        /// <param name="defectListAssetType">The value for <see cref="DefectListPartDefect.DefectListAssetType"/>.</param>
        /// <param name="defectListID">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectListName">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="partID">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="partName">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectID">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectName">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectSeverity">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectListPartDefectsDictionary">The <see cref="IDictionary{TKey, TValue}"/> to which the new <see cref="DefectListPartDefect"/> is to be added.</param>
        /// <returns></returns>
        void AddDefectListPartDefectToList(string defectListAssetType, string defectListID, string defectListName, string partID, string partName, string defectID, string defectName, string defectSeverity, IDictionary<Id, DefectListPartDefect> defectListPartDefectsDictionary)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var defectListPartDefect = new DefectListPartDefect
            {
                DefectListAssetType = defectListAssetType,
                DefectListID = defectListID,
                DefectListName = defectListName,
                PartID = partID,
                PartName = partName,
                DefectID = defectID,
                DefectName = defectName,
                DefectSeverity = defectSeverity
            };

            // Note: Use the defectListPartDefect.DefectID as the Id for this dictionary since it is the DefectId that is used for searching for DefectListPartDefects from this dictionary.
            defectListPartDefectsDictionary.Add(Id.Create(defectListPartDefect.DefectID), defectListPartDefect);

            logger.Debug($"DefectListId {defectListPartDefect.DefectListID}|DefectListName {defectListPartDefect.DefectListName}|DefectListAssetType {defectListPartDefect.DefectListAssetType}|PartId {defectListPartDefect.PartID}|PartName {defectListPartDefect.PartName}|DefectId {defectListPartDefect.DefectID}|DefectName {defectListPartDefect.DefectName}|DefectSeverity {defectListPartDefect.DefectSeverity}");

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
                await WaitForPrerequisiteServicesIfNeededAsync(stoppingToken);

                // Abort if waiting for connectivity restoration.
                if (stateMachine.CurrentState == State.Waiting)
                {
                    feedVersionRollbackRequired = true;
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var dbOServiceTracking = await serviceTracker.GetDVIRLogServiceInfoAsync();

                        // Initialize the Geotab object feeder.
                        if (dvirLogGeotabObjectFeeder.IsInitialized == false)
                        {
                            await dvirLogGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.DVIRLogFeedIntervalSeconds, myGeotabAPIHelper.GetFeedResultLimitDefault, (long?)dbOServiceTracking.LastProcessedFeedVersion);
                        }

                        // If this is the first iteration after a connectivity disruption, roll-back the LastFeedVersion of the GeotabObjectFeeder to the last processed feed version that was committed to the database and set the LastFeedRetrievalTimeUtc to DateTime.MinValue to start processing without further delay.
                        if (feedVersionRollbackRequired == true)
                        {
                            dvirLogGeotabObjectFeeder.LastFeedVersion = dbOServiceTracking.LastProcessedFeedVersion;
                            dvirLogGeotabObjectFeeder.LastFeedRetrievalTimeUtc = DateTime.MinValue;
                            feedVersionRollbackRequired = false;
                        }

                        // Update the DefectListPartDefect cache.
                        await UpdateDefectListPartDefectsDictionaryAsync(cancellationTokenSource);

                        // Get a batch of DVIRLog objects from Geotab.
                        await dvirLogGeotabObjectFeeder.GetFeedDataBatchAsync(cancellationTokenSource);
                        stoppingToken.ThrowIfCancellationRequested();

                        // Process any returned DVIRLogs.
                        var dvirLogs = dvirLogGeotabObjectFeeder.GetFeedResultDataValuesList();
                        DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                        var dbDVIRLogsToPersist = new List<DbDVIRLog>();
                        var dbDVIRDefectsToPersist = new List<DbDVIRDefect>();
                        var dbDVIRDefectRemarksToPersist = new List<DbDVIRDefectRemark>();
                        if (dvirLogs.Count > 0)
                        {
                            // Apply tracked device filter (if configured in appsettings.json).
                            var filteredDVIRLogs = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, dvirLogs);

                            // Process the filtered DVIRLog entities.
                            foreach (var filteredDVIRLog in filteredDVIRLogs)
                            {
                                // Create a DbDVIRLog, set its properties and add it to the dbDVIRLogsToPersist list for later write to the database.
                                var dbDVIRLog = geotabDVIRLogDbDVIRLogObjectMapper.CreateEntity(filteredDVIRLog);
                                dbDVIRLog.RecordCreationTimeUtc = recordChangedTimestampUtc;
                                dbDVIRLogsToPersist.Add(dbDVIRLog);

                                // Process any DVIRDefects associated with the subject DVIRLog.
                                if (filteredDVIRLog.DVIRDefects != null)
                                {
                                    foreach (var dvirDefect in filteredDVIRLog.DVIRDefects)
                                    {
                                        Defect defect = dvirDefect.Defect;
                                        DbDVIRDefect existingDbDVIRDefectForRemarkProcessing = null;

                                        // Get the DefectListPartDefect associated with the subject Defect.
                                        if (defectListPartDefectsDictionary.TryGetValue(defect.Id, out var defectListPartDefect))
                                        {
                                            // Try to find the existing database record for the DbDVIRDefect representing the subject DVIRDefect.
                                            var existingDbDVIRDefect = await GetDbDVIRDefectAsync(dvirDefect, cancellationTokenSource);
                                            if (existingDbDVIRDefect != null)
                                            {
                                                // The DVIRDefect has already been added to the database.
                                                if (geotabDVIRDefectDbDVIRDefectObjectMapper.EntityRequiresUpdate(existingDbDVIRDefect, dvirDefect))
                                                {
                                                    var updatedDbDVIRDefect = geotabDVIRDefectDbDVIRDefectObjectMapper.UpdateEntity(existingDbDVIRDefect, filteredDVIRLog, dvirDefect, defect, defectListPartDefect);
                                                    dbDVIRDefectsToPersist.Add(updatedDbDVIRDefect);
                                                    existingDbDVIRDefectForRemarkProcessing = updatedDbDVIRDefect;
                                                }
                                                else
                                                {
                                                    existingDbDVIRDefectForRemarkProcessing = existingDbDVIRDefect;
                                                }
                                            }
                                            else
                                            {
                                                // The DVIRDefect has not yet been added to the database. Create a DbDVIRDefect, set its properties and add it to the list for later write to the database.
                                                var newDbDVIRDefect = geotabDVIRDefectDbDVIRDefectObjectMapper.CreateEntity(filteredDVIRLog, dvirDefect, defect, defectListPartDefect);
                                                dbDVIRDefectsToPersist.Add(newDbDVIRDefect);
                                            }
                                        }
                                        else
                                        {
                                            logger.Debug($"No DefectListPartDefect could be found for the Defect '{defect.Id} ({defect.Name})' associated with the DVIRDefect '{dvirDefect.Id}' of DVIRLog '{filteredDVIRLog.Id}'. This DVIRDefect will not be processed.");
                                        }

                                        // Process any DefectRemarks associated with the subject DVIRDefect.
                                        if (dvirDefect.DefectRemarks != null)
                                        {
                                            // If the DVIRDefect has already been written to the database, retrieve any associated DefectRemarks that may have also been written to the database. 
                                            var existingDbDVIRDefectRemarks = new List<DbDVIRDefectRemark>();
                                            if (existingDbDVIRDefectForRemarkProcessing != null)
                                            {
                                                existingDbDVIRDefectRemarks = await GetDbDVIRDefectRemarksAsync(dvirDefect.Id.ToString(), cancellationTokenSource);
                                            }

                                            // Iterate through the individual DefectRemarks, adding only those that have not already beed added to the database.
                                            foreach (var dvirDefectRemark in dvirDefect.DefectRemarks)
                                            {
                                                // If the DefectRemark has not yet been added to the database, create a DbDVIRDefectRemark, set its properties and add it to the list for later write to the database.
                                                #region Temporary - Use DefectRemark DateTime and Remark combination as the Id
                                                // MyGeotab Bug: the DefectRemark Ids change whenever a new DefectRemark is added to a DVIRLog, resulting in all existing DefectRemarks being duplicated in the adapter database each time a new one is added. Once this bug has been resolved, the code can revert to the commented-out version below which uses DefectRemark.Id.
                                                if (!existingDbDVIRDefectRemarks.Where(dbDVIRDefectRemark => dbDVIRDefectRemark.DateTime == dvirDefectRemark.DateTime && dbDVIRDefectRemark.Remark == dvirDefectRemark.Remark).Any())
                                                {
                                                    var newDbDVIRDefectRemark = geotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper.CreateEntity(dvirDefectRemark);
                                                    dbDVIRDefectRemarksToPersist.Add(newDbDVIRDefectRemark);
                                                }
                                                #endregion
                                                //var existingDbDVIRDefectRemark = await GetDbDVIRDefectRemarkAsync(dvirDefectRemark, cancellationTokenSource);
                                                //if (existingDbDVIRDefectRemark == null)
                                                //{
                                                //    var newDbDVIRDefectRemark = geotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper.CreateEntity(dvirDefectRemark);
                                                //    dbDVIRDefectRemarksToPersist.Add(newDbDVIRDefectRemark);
                                                //}
                                            }
                                        }
                                    }
                                }
                            }
                            stoppingToken.ThrowIfCancellationRequested();
                        }

                        // Persist changes to database.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    // DbDVIRLog:
                                    await dbDVIRLogEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDVIRLogsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbDVIRDefect:
                                    await dbDVIRDefectEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDVIRDefectsToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbDVIRDefectRemark:
                                    await dbDVIRDefectRemarkEntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbDVIRDefectRemarksToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                    // DbOServiceTracking:
                                    if (dbDVIRLogsToPersist.Count > 0)
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DVIRLogProcessor, dvirLogGeotabObjectFeeder.LastFeedRetrievalTimeUtc, dvirLogGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No DVIRLogs were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DVIRLogProcessor, DateTime.UtcNow);
                                    }

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

                        // Clear FeedResultData.
                        dvirLogGeotabObjectFeeder.FeedResultData.Clear();
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
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    HandleException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                }

                // If the feed is up-to-date, add a delay equivalent to the configured update interval.
                if (dvirLogGeotabObjectFeeder.FeedCurrent == true)
                {
                    var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.DVIRLogFeedIntervalSeconds);
                    logger.Info($"{CurrentClassName} pausing for the configured feed interval ({delayTimeSpan}).");
                    await Task.Delay(delayTimeSpan, stoppingToken);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Retrieves the <see cref="DbDVIRDefect"/> associated with the <paramref name="dvirDefect"/> from its associated database table. If no matching database record is found, the return value will be <c>null</c>.
        /// </summary>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> for which to retrieve the matching <see cref="DbDVIRDefect"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<DbDVIRDefect> GetDbDVIRDefectAsync(DVIRDefect dvirDefect, CancellationTokenSource cancellationTokenSource)
        {
            DbDVIRDefect dbDVIRDefect = null;
            if (dvirDefect.Id != null)
            {
                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                {
                    using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                    {
                        dbDVIRDefect = await dbDVIRDefectRepo.GetAsync(dvirDefect.Id.ToString(), cancellationTokenSource);
                    }
                }, new Context());
            }
            return dbDVIRDefect;
        }

        /// <summary>
        /// Retrieves the <see cref="DbDVIRDefectRemark"/> associated with the <paramref name="dvirDefectRemark"/> from its associated database table. If no matching database record is found, the return value will be <c>null</c>.
        /// </summary>
        /// <param name="dvirDefectRemark">The <see cref="DefectRemark"/> for which to retrieve the matching <see cref="DbDVIRDefectRemark"/>.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<DbDVIRDefectRemark> GetDbDVIRDefectRemarkAsync(DefectRemark dvirDefectRemark, CancellationTokenSource cancellationTokenSource)
        {
            DbDVIRDefectRemark dbDVIRDefectRemark = null;
            if (dvirDefectRemark.Id != null)
            {
                await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                {
                    using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                    {
                        dbDVIRDefectRemark = await dbDVIRDefectRemarkRepo.GetAsync(dvirDefectRemark.Id.ToString(), cancellationTokenSource);
                    }
                }, new Context());
            }
            return dbDVIRDefectRemark;
        }

        /// <summary>
        /// Retrieves the list of all <see cref="DbDVIRDefectRemark"/>s bearing the <paramref name="dvirDefectId"/> from the associated database table. If no matching database records are found, the list will be empty.
        /// </summary>
        /// <param name="dvirDefectId">The <see cref="DbDVIRDefectRemark.DVIRDefectId"/> of any <see cref="DbDVIRDefectRemark"/>s to be returned.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<List<DbDVIRDefectRemark>> GetDbDVIRDefectRemarksAsync(string dvirDefectId, CancellationTokenSource cancellationTokenSource)
        {
            // Use dynamic parameters since a column other than the Id or GeotabId is being queried on.
            var parameters = new Dictionary<string, object>()
            {
                ["DVIRDefectId"] = dvirDefectId
            };

            var dbDVIRDefectRemarks = new List<DbDVIRDefectRemark>();
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var uow = adapterContext.CreateUnitOfWork(adapterContext.Database))
                {
                    dbDVIRDefectRemarks = (List<DbDVIRDefectRemark>)await dbDVIRDefectRemarkRepo.GetAsync(parameters, cancellationTokenSource);
                }
            }, new Context());

            return dbDVIRDefectRemarks;
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
            else if (exception is MyGeotabConnectionException)
            {
                stateMachine.SetState(State.Waiting, StateReason.MyGeotabNotAvailable);
            }

            if (logLevel == NLogLogLevelName.Fatal)
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// Starts the current <see cref="DVIRLogProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DVIRLogProcessor);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DVIRLogProcessor, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
            if (adapterConfiguration.EnableDVIRLogFeed == true)
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
        /// Stops the current <see cref="DVIRLogProcessor"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Builds a list of <see cref="DefectListPartDefect"/> objects by retrieving all defect lists in the database and flattening the trees - capturing the defect list, part and part defect information required when processing <see cref="DVIRDefect"/> information associated with <see cref="DVIRLog"/>s. 
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateDefectListPartDefectsDictionaryAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Only update the cache if the defectListPartDefectCacheExpiryTime has been reached.
            var currentTime = DateTime.Now;
            if (currentTime < defectListPartDefectCacheExpiryTime)
            {
                return;
            }

            // Clear the defectListPartDefects cache and set a new cache expiry time.
            defectListPartDefectsDictionary.Clear();
            defectListPartDefectCacheExpiryTime = currentTime.AddMinutes(adapterConfiguration.DVIRDefectListCacheRefreshIntervalMinutes);

            // Get all defect lists.
            DefectSearch defectSearch = new()
            {
                IncludeAllTrees = true
            };
            var defectLists = await myGeotabAPIHelper.GetAsync<Defect>(defectSearch, adapterConfiguration.TimeoutSecondsForMyGeotabTasks);

            cancellationToken.ThrowIfCancellationRequested();

            // Enumerate the defect lists, getting the parts associated with each defect list.
            foreach (var defectList in defectLists)
            {
                var defectListParts = defectList.Children;
                // Enumerate the parts associated with the current defect list, getting the defects associated with each part.
                foreach (var part in defectListParts)
                {
                    // First, add a virtual DefectListPartDefect to account for situations where the user chooses "other" when loggng a DVIRDefect (vs. selecting a part or defined defect from the lists provided). This will simplify DVIRLog processing logic later.
                    Defect virtualDefect = (Defect)part;
                    AddDefectListPartDefectToList(defectList.AssetType.ToString(), defectList.Id.ToString(), defectList.Name, part.Id.ToString(), part.Name, virtualDefect.Id.ToString(), virtualDefect.Name, virtualDefect.Severity.ToString(), defectListPartDefectsDictionary);

                    // Now, process the defects associated with the subject part.
                    var partDefects = part.Children;
                    foreach (var partDefect in partDefects)
                    {
                        if (partDefect is Defect defect)
                        {
                            AddDefectListPartDefectToList(defectList.AssetType.ToString(), defectList.Id.ToString(), defectList.Name, part.Id.ToString(), part.Name, defect.Id.ToString(), defect.Name, defect.Severity.ToString(), defectListPartDefectsDictionary);
                        }
                        else
                        {
                            logger.Debug($"partDefect '{partDefect.Id} ({partDefect.Name})' is not a Defect.");
                        }
                    }
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Checks whether any prerequisite services have been run and are currently running. If any of prerequisite services have not yet been run or are not currently running, details will be logged and this service will pause operation, repeating this check intermittently until all prerequisite services are running.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        async Task WaitForPrerequisiteServicesIfNeededAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var prerequisiteServices = new List<AdapterService>
            {
                AdapterService.DeviceProcessor,
                AdapterService.UserProcessor
            };

            await prerequisiteServiceChecker.WaitForPrerequisiteServicesIfNeededAsync(CurrentClassName, prerequisiteServices, cancellationToken);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
