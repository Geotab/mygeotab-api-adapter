using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Logic;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// The "brain" of the application. Initialization tasks are executed in the <see cref="StartAsync(CancellationToken)"/> method and then, business logic is executed iteratively on an indefinite basis in the <see cref="ExecuteAsync(CancellationToken)"/> method.
    /// </summary>
    class Worker : BackgroundService
    {
        const int ConnectivityRestorationCheckIntervalMilliseconds = 10000;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IConfiguration configuration;
        CacheManager cacheManager;
        FeedManager feedManager;
        ConnectionInfo connectionInfo;
        IDictionary<Id, Device> trackedDevicesDictionary;
        bool trackedDevicesDictionaryIsPopulated;
        IDictionary<Id, Diagnostic> trackedDiagnosticsDictionary;
        bool trackedDiagnosticsDictionaryIsPopulated;
        List<DbConfigFeedVersion> dbConfigFeedVersions;
        IDictionary<Id, DbDevice> dbDevicesDictionary;
        IDictionary<Id, DbDiagnostic> dbDiagnosticsDictionary;
        IDictionary<Id, DbDVIRDefect> dbDVIRDefectsDictionary;
        IDictionary<Id, DbDVIRDefectRemark> dbDVIRDefectRemarksDictionary;
        IDictionary<Id, DbRuleObject> dbRuleObjectDictionary;
        IDictionary<Id, DbUser> dbUsersDictionary;
        IDictionary<Id, DbZone> dbZonesDictionary;
        IDictionary<Id, DefectListPartDefect> defectListPartDefectsDictionary;
        DateTime defectListPartDefectCacheExpiryTime = DateTime.MinValue;
        bool initializationCompleted;

        /// <summary>
        /// Instantiates a new instance of the <see cref="Worker"/> class.
        /// </summary>
        public Worker(IConfiguration configuration)
        {
            this.configuration = configuration;
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            StateMachine.CurrentState = State.Normal;
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
        /// Evaluates the supplied list of <see cref="ExceptionEvent"/>s and filters-out any whose <see cref="Geotab.Checkmate.ObjectModel.Exceptions.Rule"/> does not match an item in the <see cref="CacheManager.RuleCacheContainer"/>. These would include ZoneStop exceptions if ZoneStops are not being tracked.
        /// </summary>
        /// <param name="exceptionEventsToBeFiltered">The list of <see cref="ExceptionEvent"/>s to be filtered.</param>
        /// <returns></returns>
        public List<ExceptionEvent> ApplyRuleFilterToExceptionEventList(List<ExceptionEvent> exceptionEventsToBeFiltered)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var ruleCache = cacheManager.RuleCacheContainer.Cache;
            List<ExceptionEvent> filteredList = new List<ExceptionEvent>();
            foreach (var exceptionEventToBeEvaluated in exceptionEventsToBeFiltered)
            {
                var exceptionEventRule = exceptionEventToBeEvaluated.Rule;
                if (ruleCache.Contains(exceptionEventRule.Id))
                {
                    filteredList.Add(exceptionEventToBeEvaluated);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return filteredList;
        }

        /// <summary>
        /// Evaluates the supplied list of entities and filters-out any entities whose <see cref="Device"/> property does not match an item in the <see cref="trackedDevicesDictionary"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> in the supplied list.</typeparam>
        /// <param name="listToBeFiltered">The list of entities to be filtered.</param>
        /// <returns></returns>
        public List<T> ApplyTrackedDevicesFilterToList<T>(List<T> listToBeFiltered) where T : Entity
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            List<T> filteredList = new List<T>();
            if (trackedDevicesDictionary.Any())
            {
                // Certain Devices are being tracked. Iterate through the list of entities to be filtered and keep only those that represent Devices that are being tracked.
                string entityTypeName = typeof(T).Name;
                foreach (var itemToBeEvaluated in listToBeFiltered)
                {
                    Device itemToBeEvaluatedDevice = NoDevice.Value;
                    string errorMessage = "";
                    switch (entityTypeName)
                    {
                        case nameof(FaultData):
                            var faultDataToBeEvaluated = itemToBeEvaluated as FaultData;
                            itemToBeEvaluatedDevice = faultDataToBeEvaluated.Device;
                            break;
                        case nameof(LogRecord):
                            var logRecordToBeEvaluated = itemToBeEvaluated as LogRecord;
                            itemToBeEvaluatedDevice = logRecordToBeEvaluated.Device;
                            break;
                        case nameof(Trip):
                            var tripToBeEvaluated = itemToBeEvaluated as Trip;
                            itemToBeEvaluatedDevice = tripToBeEvaluated.Device;
                            break;
                        case nameof(StatusData):
                            var statusDataToBeEvaluated = itemToBeEvaluated as StatusData;
                            itemToBeEvaluatedDevice = statusDataToBeEvaluated.Device;
                            break;
                        default:
                            errorMessage = $"The entity type '{entityTypeName}' is not supported by the '{methodBase.ReflectedType.Name}' method.";
                            logger.Error(errorMessage);
                            throw new Exception(errorMessage);
                    }

                    if (trackedDevicesDictionary.ContainsKey(itemToBeEvaluatedDevice.Id))
                    {
                        filteredList.Add(itemToBeEvaluated);
                    }
                }
            }
            else
            {
                // No specific Devices are being tracked. All entities in the list should be kept.
                filteredList.AddRange(listToBeFiltered);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return filteredList;
        }

        /// <summary>
        /// Evaluates the supplied list of entities and filters-out any entities whose <see cref="Diagnostic"/> property does not match an item in the <see cref="trackedDiagnosticsDictionary"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> in the supplied list.</typeparam>
        /// <param name="listToBeFiltered">The list of entities to be filtered.</param>
        /// <returns></returns>
        public List<T> ApplyTrackedDiagnosticsFilterToList<T>(List<T> listToBeFiltered) where T : Entity
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            List<T> filteredList = new List<T>();
            if (trackedDiagnosticsDictionary.Any())
            {
                // Certain Diagnostics are being tracked. Iterate through the list of entities to be filtered and keep only those that represent Diagnostics that are being tracked.
                string entityTypeName = typeof(T).Name;
                foreach (var itemToBeEvaluated in listToBeFiltered)
                {
                    Diagnostic itemToBeEvaluatedDiagnostic = NoDiagnostic.Value;
                    string errorMessage = "";
                    switch (entityTypeName)
                    {
                        case nameof(FaultData):
                            var faultDataToBeEvaluated = itemToBeEvaluated as FaultData;
                            itemToBeEvaluatedDiagnostic = faultDataToBeEvaluated.Diagnostic;
                            break;
                        case nameof(StatusData):
                            var statusDataToBeEvaluated = itemToBeEvaluated as StatusData;
                            itemToBeEvaluatedDiagnostic = statusDataToBeEvaluated.Diagnostic;
                            break;
                        default:
                            errorMessage = $"The entity type '{entityTypeName}' is not supported by the '{methodBase.ReflectedType.Name}' method.";
                            logger.Error(errorMessage);
                            throw new Exception(errorMessage);
                    }

                    if (trackedDiagnosticsDictionary.ContainsKey(itemToBeEvaluatedDiagnostic.Id))
                    {
                        filteredList.Add(itemToBeEvaluated);
                    }
                }
            }
            else
            {
                // No specific Diagnostics are being tracked. All entities in the list should be kept.
                filteredList.AddRange(listToBeFiltered);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return filteredList;
        }

        /// <summary>
        /// Builds a list of <see cref="Device"/> objects that are to be tracked with respect to data feeds based on the <see cref="ConfigurationManager.DevicesToTrackList"/>. The list is only populated the first time this method is called. If all devices are to be tracked, the list will be empty and <see cref="ConfigurationManager.TrackAllDevices"/> will be <c>true</c>. Note that the <see cref="CacheManager.DeviceCacheContainer"/> must be populated before this method can work.
        /// </summary>
        /// <returns></returns>
        void BuildTrackedDevicesDictionary()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (!trackedDevicesDictionaryIsPopulated)
            {
                if (!Globals.ConfigurationManager.TrackAllDevices)
                {
                    var deviceCache = cacheManager.DeviceCacheContainer.Cache;
                    string[] deviceList = Globals.ConfigurationManager.DevicesToTrackList.Split(",");
                    for (int deviceListIndex = 0; deviceListIndex < deviceList.Length; deviceListIndex++)
                    {
                        string deviceId = deviceList[deviceListIndex];
                        if (deviceCache.Contains(deviceId))
                        {
                            var checkedDevice = (Device)deviceCache[deviceId];
                            trackedDevicesDictionary.Add(checkedDevice.Id, checkedDevice);
                        }
                        else
                        {
                            logger.Warn($"'{deviceId}' is not a valid device Id; as such the intended device will not be tracked.");
                        }
                    }
                }
                trackedDevicesDictionaryIsPopulated = true;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Builds a list of <see cref="Diagnostic"/> types that are to be tracked with respect to data feeds based on the <see cref="ConfigurationManager.DiagnosticsToTrackList"/>. The list is only populated the first time this method is called. If all diagnostics are to be tracked, the list will be empty and <see cref="ConfigurationManager.TrackAllDiagnostics"/> will be <c>true</c>. Note that the <see cref="CacheManager.DiagnosticCacheContainer"/> must be populated before this method can work.
        /// </summary>
        /// <returns></returns>
        void BuildTrackedDiagnosticsDictionary()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (!trackedDiagnosticsDictionaryIsPopulated)
            {
                if (!Globals.ConfigurationManager.TrackAllDiagnostics)
                {
                    var diagnosticCache = cacheManager.DiagnosticCacheContainer.Cache;
                    string[] diagnosticList = Globals.ConfigurationManager.DiagnosticsToTrackList.Split(",");
                    for (int diagnosticListIndex = 0; diagnosticListIndex < diagnosticList.Length; diagnosticListIndex++)
                    {
                        string diagnosticId = diagnosticList[diagnosticListIndex];
                        if (diagnosticCache.Contains(diagnosticId))
                        {
                            var checkedDiagnostic = (Diagnostic)diagnosticCache[diagnosticId];
                            trackedDiagnosticsDictionary.Add(checkedDiagnostic.Id, checkedDiagnostic);
                        }
                        else
                        {
                            logger.Warn($"'{diagnosticId}' is not a valid diagnostic Id; as such the intended diagnostic will not be tracked.");
                            continue;
                        }
                    }
                }
                trackedDiagnosticsDictionaryIsPopulated = true;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Disposes the current <see cref="Worker"/> instance.
        /// </summary>
        public override void Dispose()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            base.Dispose();
        }

        /// <summary>
        /// Iteratively executes the business logic until the application is stopped.
        /// </summary>
        /// <param name="stoppingToken">The <see cref="CancellationToken"/> that can be used to stop execution of the application.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // This is the loop containing all of the business logic that is executed iteratively throughout the lifeime of the application.
            while (!stoppingToken.IsCancellationRequested)
            {
                // Abort if waiting for restoration of connectivity to the MyGeotab server or to the database.
                if (StateMachine.CurrentState == State.Waiting)
                {
                    continue;
                }

                if (initializationCompleted == false)
                {
                    await PerformInitializationTasksAsync();
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    await cacheManager.UpdateCachesAsync();
                    BuildTrackedDevicesDictionary();
                    BuildTrackedDiagnosticsDictionary();
                    await feedManager.GetDataFromFeedsAsync();
                    PropagateAllCacheUpdatesToDatabase();
                    ProcessAllFeedResults();

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"Worker process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (DatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLog.LogLevel.Error, "Worker process caught an exception");
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    HandleException(myGeotabConnectionException, NLog.LogLevel.Error, "Worker process caught an exception");
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLog.LogLevel.Fatal, "******** Worker process caught an unhandled exception and will self-terminate now.");
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>. If the <paramref name="exception"/> is a <see cref="MyGeotabConnectionException"/> or a <see cref="DatabaseConnectionException"/>, the <see cref="WaitForConnectivityRestorationAsync(StateReason)"/> method will be executed after logging the error message.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="errorMessageLogLevel">The <see cref="NLog.LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        void HandleException(Exception exception, NLog.LogLevel errorMessageLogLevel, string errorMessagePrefix = "An exception was encountered")
        {
            Globals.LogException(exception, errorMessageLogLevel, errorMessagePrefix);

            if (exception is MyGeotabConnectionException)
            {
                _ = WaitForConnectivityRestorationAsync(StateReason.MyGeotabNotAvailable);
            }
            if (exception is DatabaseConnectionException)
            {
                _ = WaitForConnectivityRestorationAsync(StateReason.DatabaseNotAvailable);
            }
        }

        /// <summary>
        /// Returns a list of <see cref="DbConfigFeedVersion"/> objects populated by loading any existing items from the database and creating new records for any <see cref="Utilities.Common.SupportedFeedTypes"/> that do not yet have a database record.
        /// </summary>
        /// <returns></returns>
        async Task<List<DbConfigFeedVersion>> InitializeDbConfigFeedVersionListAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Get any existing feed versions from the database.
            List<DbConfigFeedVersion> dbConfigFeedVersionList = null;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    IEnumerable<DbConfigFeedVersion> dbConfigFeedVersions = await DbConfigFeedVersionService.GetAllAsync(connectionInfo, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    dbConfigFeedVersionList = dbConfigFeedVersions.ToList();
                }
                catch (DatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLog.LogLevel.Error, "Worker process caught an exception");
                }
            }

            // Make sure that a feed version record exists for each type of feed that is utilized in this application (i.e. each MyGeotab object type for which there is a FeedContainer in the FeedManager class). For any that don't have a record (e.g. when the application is run for the first time), create a new record with its FeedVersion set to zero.
            var supportedFeedTypes = Enum.GetValues(typeof(Globals.SupportedFeedTypes));
            foreach (var supportedFeedType in supportedFeedTypes)
            {
                if (!dbConfigFeedVersionList.Where(dbConfigVersion => dbConfigVersion.FeedTypeId == supportedFeedType.ToString()).Any())
                {
                    DbConfigFeedVersion newDbConfigFeedVersion = new DbConfigFeedVersion
                    {
                        DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                        FeedTypeId = supportedFeedType.ToString(),
                        LastProcessedFeedVersion = 0,
                        RecordLastChangedUtc = DateTime.UtcNow
                    };
                    dbConfigFeedVersionList.Add(newDbConfigFeedVersion);
                }
            }

            // Insert any newly-added items into the database.
            List<DbConfigFeedVersion> newDbConfigFeedVersions = dbConfigFeedVersionList.Where(dbConfigFeedVersion => dbConfigFeedVersion.DatabaseWriteOperationType == Common.DatabaseWriteOperationType.Insert).ToList();

            if (newDbConfigFeedVersions.Any())
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    await DbConfigFeedVersionService.InsertAsync(connectionInfo, newDbConfigFeedVersions, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                }
                catch (DatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLog.LogLevel.Error, "Worker process caught an exception");
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return dbConfigFeedVersionList;
        }

        /// <summary>
        /// Retrieves lists of existing database entities of various types. Later, during data processing, MyGeotab objects are compared against these lists rather than executing select queries on a per-object basis - thereby reducing chattiness of the application and boosting performance. 
        /// </summary>
        /// <returns></returns>
        void InitializeListsOfExistingDatabaseEntities()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var getAllDbDevicesTask = DbDeviceService.GetAllAsync(connectionInfo, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    var getAllDbDiagnosticsTask = DbDiagnosticService.GetAllAsync(connectionInfo, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    var getAllDbUsersTask = DbUserService.GetAllAsync(connectionInfo, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    var getAllDbDVIRDefectsTask = DbDVIRDefectService.GetAllAsync(connectionInfo, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    var getAllDbDVIRDefectRemarksTask = DbDVIRDefectRemarkService.GetAllAsync(connectionInfo, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    var getAllDbRuleObjectsTask = RuleHelper.GetDatabaseRuleObjectsAsync(cancellationTokenSource);
                    var getAllDbZonesTask = DbZoneService.GetAllAsync(connectionInfo, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);

                    Task[] tasks = { getAllDbDevicesTask, getAllDbDiagnosticsTask, getAllDbUsersTask, getAllDbDVIRDefectsTask, getAllDbDVIRDefectRemarksTask, getAllDbRuleObjectsTask, getAllDbZonesTask };

                    Task.WaitAll(tasks);

                    // Sort lists on Id.
                    dbDevicesDictionary = getAllDbDevicesTask.Result.ToDictionary(device => Id.Create(device.Id));
                    dbDiagnosticsDictionary = getAllDbDiagnosticsTask.Result.ToDictionary(diagnostic => Id.Create(diagnostic.Id));
                    dbUsersDictionary = getAllDbUsersTask.Result.ToDictionary(user => Id.Create(user.Id));
                    dbDVIRDefectsDictionary = getAllDbDVIRDefectsTask.Result.ToDictionary(dvirDefect => Id.Create(dvirDefect.Id));
                    dbDVIRDefectRemarksDictionary = getAllDbDVIRDefectRemarksTask.Result.ToDictionary(dvirDefectRemark => Id.Create(dvirDefectRemark.Id));
                    dbRuleObjectDictionary = getAllDbRuleObjectsTask.Result.ToDictionary(dbRuleObject => Id.Create(dbRuleObject.Id));
                    dbZonesDictionary = getAllDbZonesTask.Result.ToDictionary(dbZone => Id.Create(dbZone.Id));
                }
                catch (AggregateException aggregateException)
                {
                    Globals.HandleConnectivityRelatedAggregateException(aggregateException, Globals.ConnectivityIssueType.Database, "One or more exceptions were encountered during retrieval of lists of existing database entities due to an apparent loss of connectivity with the database.");
                }
                catch (TaskCanceledException taskCanceledException)
                {
                    string errorMessage = $"Task was cancelled. TaskCanceledException: \nMESSAGE [{taskCanceledException.Message}]; \nSOURCE [{taskCanceledException.Source}]; \nSTACK TRACE [{taskCanceledException.StackTrace}]";
                    logger.Warn(errorMessage);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Performs startup tasks.
        /// </summary>
        /// <returns></returns>
        async Task PerformInitializationTasksAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            try
            {
                // Setup the ConfigurationManager Globals reference
                using (ConfigurationManager configurationManager = new ConfigurationManager(configuration))
                {
                    Globals.ConfigurationManager = configurationManager;
                }
                logger.Info($"******** INITIALIZING APPLICATION ********");
                connectionInfo = new ConnectionInfo(Globals.ConfigurationManager.DatabaseConnectionString, Globals.ConfigurationManager.DatabaseProviderType);
                await Globals.AuthenticateMyGeotabApiAsync();
                await ValidateMyGeotabVersionInformationAsync();
                dbConfigFeedVersions = await InitializeDbConfigFeedVersionListAsync();
                cacheManager = CacheManager.Create();
                feedManager = FeedManager.Create(dbConfigFeedVersions);
                trackedDevicesDictionary = new Dictionary<Id, Device>();
                trackedDiagnosticsDictionary = new Dictionary<Id, Diagnostic>();
                defectListPartDefectsDictionary = new Dictionary<Id, DefectListPartDefect>();
                InitializeListsOfExistingDatabaseEntities();

                initializationCompleted = true;
                logger.Info("Initialization completed.");
            }
            catch (DatabaseConnectionException databaseConnectionException)
            {
                HandleException(databaseConnectionException, NLog.LogLevel.Error, "Worker process caught an exception");
            }
            catch (MyGeotabConnectionException myGeotabConnectionException)
            {
                HandleException(myGeotabConnectionException, NLog.LogLevel.Error, "Worker process caught an exception");
            }
            catch (Exception ex)
            {
                string errorMessage = $"Worker process caught an exception: \nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
                logger.Error(errorMessage);
                throw new Exception(errorMessage, ex);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Concurrently processes the results returned by all of the supported feeds.
        /// </summary>
        /// <returns></returns>
        void ProcessAllFeedResults()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var processDVIRLogFeedResultsAsyncTask = ProcessDVIRLogFeedResultsAsync(cancellationTokenSource);
                    var processLogRecordFeedResultsAsyncTask = ProcessLogRecordFeedResultsAsync(cancellationTokenSource);
                    var processStatusDataFeedResultsAsync = ProcessStatusDataFeedResultsAsync(cancellationTokenSource);
                    var processFaultDataFeedResultsAsync = ProcessFaultDataFeedResultsAsync(cancellationTokenSource);
                    var processTripFeedResultsAsync = ProcessTripFeedResultsAsync(cancellationTokenSource);
                    var processExceptionEventFeedResultsAsync = ProcessExceptionEventFeedResultsAsync(cancellationTokenSource);

                    Task[] tasks = { processDVIRLogFeedResultsAsyncTask, processLogRecordFeedResultsAsyncTask, processStatusDataFeedResultsAsync, processFaultDataFeedResultsAsync, processTripFeedResultsAsync, processExceptionEventFeedResultsAsync };

                    Task.WaitAll(tasks);
                }
                catch (AggregateException aggregateException)
                {
                    Globals.HandleConnectivityRelatedAggregateException(aggregateException, Globals.ConnectivityIssueType.Database, "One or more exceptions were encountered during processing of data feed results due to an apparent loss of connectivity with the database.");
                }
                catch (TaskCanceledException taskCanceledException)
                {
                    string errorMessage = $"Task was cancelled. TaskCanceledException: \nMESSAGE [{taskCanceledException.Message}]; \nSOURCE [{taskCanceledException.Source}]; \nSTACK TRACE [{taskCanceledException.StackTrace}]";
                    logger.Warn(errorMessage);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Processes <see cref="DVIRLog"/> entities returned by the data feed.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ProcessDVIRLogFeedResultsAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Update the DefectListPartDefect cache.
            try
            {
                await UpdateDefectListPartDefectCacheAsync(cancellationTokenSource);
            }
            catch (Exception)
            {
                cancellationTokenSource.Cancel();
                throw;
            }

            DateTime recordChangedTimestampUtc = DateTime.UtcNow;
            var dbDVIRDefectsToInsert = new List<DbDVIRDefect>();
            var dbDVIRDefectsToUpdate = new List<DbDVIRDefect>();
            var dbDVIRDefectRemarksToInsert = new List<DbDVIRDefectRemark>();

            // Add any returned DVIRLog entities to the database, filtering-out those representing Devices that are not being tracked.
            if (feedManager.DVIRLogFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultDVIRLogs = feedManager.DVIRLogFeedContainer.GetFeedResultDataValuesList<DVIRLog>();
                var filteredDVIRLogs = ApplyTrackedDevicesFilterToList<DVIRLog>(feedResultDVIRLogs);
                var dbDVIRLogsForDatabaseWrite = new List<DbDVIRLog>();

                // Process the filtered DVIRLog entities.
                foreach (var filteredDVIRLog in filteredDVIRLogs)
                {
                    // Create a DbDVIRLog, set its properties and add it to the dbDVIRLogsForDatabaseWrite list for later write to the database.
                    DbDVIRLog dbDVIRLog = ObjectMapper.GetDbDVIRLog(filteredDVIRLog);
                    dbDVIRLog.RecordCreationTimeUtc = recordChangedTimestampUtc;
                    dbDVIRLogsForDatabaseWrite.Add(dbDVIRLog);

                    // Process any DVIRDefects associated with the subject DVIRLog.
                    if (filteredDVIRLog.DVIRDefects != null)
                    {
                        foreach (var dvirDefect in filteredDVIRLog.DVIRDefects)
                        {
                            Defect defect = dvirDefect.Defect;

                            // Get the DefectListPartDefect associated with the subject Defect.
                            if (defectListPartDefectsDictionary.TryGetValue(defect.Id, out var defectListPartDefect))
                            {
                                // Try to find the existing database record for the DbDVIRDefect representing the subject DVIRDefect.
                                if (dbDVIRDefectsDictionary.TryGetValue(dvirDefect.Id, out var existingDbDVIRDefect))
                                {
                                    // The DVIRDefect has already been added to the database.
                                    bool dbDVIRDefectRequiresUpdate = ObjectMapper.DbDVIRDefectRequiresUpdate(existingDbDVIRDefect, dvirDefect);
                                    if (dbDVIRDefectRequiresUpdate)
                                    {
                                        DbDVIRDefect updatedDbDVIRDefect = ObjectMapper.GetDbDVIRDefect(filteredDVIRLog, dvirDefect, defect, defectListPartDefect);
                                        updatedDbDVIRDefect.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                                        updatedDbDVIRDefect.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        updatedDbDVIRDefect.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

                                        dbDVIRDefectsDictionary[Id.Create(updatedDbDVIRDefect.Id)] = updatedDbDVIRDefect;
                                        dbDVIRDefectsToUpdate.Add(updatedDbDVIRDefect);
                                    }
                                }
                                else
                                {
                                    // The DVIRDefect has not yet been added to the database. Create a DbDVIRDefect, set its properties and add it to the list for later write to the database.
                                    DbDVIRDefect newDbDVIRDefect = ObjectMapper.GetDbDVIRDefect(filteredDVIRLog, dvirDefect, defect, defectListPartDefect);
                                    newDbDVIRDefect.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                                    newDbDVIRDefect.RecordLastChangedUtc = recordChangedTimestampUtc;
                                    newDbDVIRDefect.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;

                                    dbDVIRDefectsDictionary[Id.Create(newDbDVIRDefect.Id)] = newDbDVIRDefect;
                                    dbDVIRDefectsToInsert.Add(newDbDVIRDefect);
                                }
                            }
                            else
                            {
                                logger.Debug($"No DefectListPartDefect could be found for the Defect '{defect.Id.ToString()} ({defect.Name})' associated with the DVIRDefect '{dvirDefect.Id.ToString()}' of DVIRLog '{filteredDVIRLog.Id.ToString()}'. This DVIRDefect will not be processed.");
                            }

                            // Process any DefectRemarks associated with the subject DVIRDefect.
                            if (dvirDefect.DefectRemarks != null)
                            {
                                foreach (var defectRemark in dvirDefect.DefectRemarks)
                                {
                                    // If the DefectRemark has not yet been added to the database, create a DbDVIRDefectRemark, set its properties and add it to the list for later write to the database.
                                    if (!dbDVIRDefectRemarksDictionary.TryGetValue(defectRemark.Id, out var existingDbDVIRDefectRemark))
                                    {
                                        DbDVIRDefectRemark newDbDVIRDefectRemark = ObjectMapper.GetDbDVIRDefectRemark(defectRemark);
                                        newDbDVIRDefectRemark.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                                        newDbDVIRDefectRemark.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        newDbDVIRDefectRemark.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;

                                        dbDVIRDefectRemarksDictionary.Add(Id.Create(newDbDVIRDefectRemark.Id), newDbDVIRDefectRemark);
                                        dbDVIRDefectRemarksToInsert.Add(newDbDVIRDefectRemark);
                                    }
                                }
                            }
                        }
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Process all inserts and updates of DbDVIRLogs, DbDVIRDefects and DbDVIRDefectRemarks for the current batch of DVIRLogs within a single transaction. Include persistence of the feed version within the same transaction to prevent issues in the event that an exception occurs between persistence of the data and persistence of the feed version.
                if (dbDVIRLogsForDatabaseWrite.Any())
                {
                    // Get the feed version information.
                    var dvirLogDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.DVIRLog.ToString()).First();
                    dvirLogDbConfigFeedVersion.LastProcessedFeedVersion = (long)feedManager.DVIRLogFeedContainer.LastFeedVersion;
                    dvirLogDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                    dvirLogDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        string resultCounts = await DbDVIRLogService.PersistAllDVIRLogChangesToDatabase(connectionInfo, dbDVIRLogsForDatabaseWrite, dbDVIRDefectsToInsert, dbDVIRDefectsToUpdate, dbDVIRDefectRemarksToInsert, dvirLogDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);

                        string[] individualResultCounts = resultCounts.Split(",");
                        logger.Info($"Completed insertion of {individualResultCounts[0]} records into {Globals.ConfigurationManager.DbDVIRLogTableName} table, insertion of {individualResultCounts[1]} records into {Globals.ConfigurationManager.DbDVIRDefectTableName} table, update of {individualResultCounts[2]} records in {Globals.ConfigurationManager.DbDVIRDefectTableName} table and insertion of {individualResultCounts[3]} records into {Globals.ConfigurationManager.DbDVIRDefectRemarkTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds.");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }

                // Clear FeedResultData.
                feedManager.DVIRLogFeedContainer.FeedResultData.Clear();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Processes <see cref="ExceptionEvent"/> entities returned by the data feed.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ProcessExceptionEventFeedResultsAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Add any returned ExceptionEvents to the database, filtering-out those representing Devices that are not being tracked. Include persistence of the feed version within the same transaction to prevent issues in the event that an exception occurs between persistence of the data and persistence of the feed version.
            if (feedManager.ExceptionEventFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultExceptionEvents = feedManager.ExceptionEventFeedContainer.GetFeedResultDataValuesList<ExceptionEvent>();
                var filteredExceptionEvents = ApplyRuleFilterToExceptionEventList(feedResultExceptionEvents);
                filteredExceptionEvents = ApplyTrackedDevicesFilterToList<ExceptionEvent>(filteredExceptionEvents);

                // Map ExceptionEvents to DbExceptionEvents.
                var dbExpectionEvents = ObjectMapper.GetDbExceptionEvents(filteredExceptionEvents);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var exceptionEventDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.ExceptionEvent.ToString()).First();
                exceptionEventDbConfigFeedVersion.LastProcessedFeedVersion = (long)feedManager.ExceptionEventFeedContainer.LastFeedVersion;
                exceptionEventDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                exceptionEventDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbExceptionEvents into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long noOfInserts = await DbExceptionEventService.InsertAsync(connectionInfo, dbExpectionEvents, exceptionEventDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)feedManager.ExceptionEventFeedContainer.FeedResultData.Count / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {feedManager.ExceptionEventFeedContainer.FeedResultData.Count.ToString()} records into " +
                        $"{Globals.ConfigurationManager.DbExceptionEventTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds " +
                        $"({recordsProcessedPerSecond.ToString()} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                feedManager.ExceptionEventFeedContainer.FeedResultData.Clear();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Processes <see cref="FaultData"/> entities returned by the data feed.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ProcessFaultDataFeedResultsAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Add any returned FaultData entities to the database, filtering-out those representing Devices or Diagnostics that are not being tracked. Include persistence of the feed version within the same transaction to prevent issues in the event that an exception occurs between persistence of the data and persistence of the feed version.
            if (feedManager.FaultDataFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultFaultDatas = feedManager.FaultDataFeedContainer.GetFeedResultDataValuesList<FaultData>();
                var filteredFaultDatas = ApplyTrackedDevicesFilterToList<FaultData>(feedResultFaultDatas);
                filteredFaultDatas = ApplyTrackedDiagnosticsFilterToList<FaultData>(filteredFaultDatas);

                // Hydrate child objects of filtered FaultData entities.
                foreach (var filteredFaultData in filteredFaultDatas)
                {
                    Controller hydratedController = cacheManager.HydrateController(filteredFaultData.Controller);
                    filteredFaultData.Controller = hydratedController;
                    FailureMode hydratedFailureMode = cacheManager.HydrateFailureMode(filteredFaultData.FailureMode);
                    filteredFaultData.FailureMode = hydratedFailureMode;
                }

                // Map FaultData entities to DbFaultData entities.
                var dbFaultDatas = ObjectMapper.GetDbFaultDatas(filteredFaultDatas);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var faultDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.FaultData.ToString()).First();
                faultDataDbConfigFeedVersion.LastProcessedFeedVersion = (long)feedManager.FaultDataFeedContainer.LastFeedVersion;
                faultDataDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                faultDataDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbFaultData entities into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long faultDataEntitiesInserted = await DbFaultDataService.InsertAsync(connectionInfo, dbFaultDatas, faultDataDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)faultDataEntitiesInserted / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {faultDataEntitiesInserted.ToString()} records into {Globals.ConfigurationManager.DbFaultDataTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                feedManager.FaultDataFeedContainer.FeedResultData.Clear();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Processes <see cref="LogRecord"/> entities returned by the data feed.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ProcessLogRecordFeedResultsAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Add any returned LogRecords to the database, filtering-out those representing Devices that are not being tracked. Include persistence of the feed version within the same transaction to prevent issues in the event that an exception occurs between persistence of the data and persistence of the feed version.
            if (feedManager.LogRecordFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultLogRecords = feedManager.LogRecordFeedContainer.GetFeedResultDataValuesList<LogRecord>();
                var filteredLogRecords = ApplyTrackedDevicesFilterToList<LogRecord>(feedResultLogRecords);

                // Map LogRecords to DbLogRecords.
                var dbLogRecords = ObjectMapper.GetDbLogRecords(filteredLogRecords);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var logRecordDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.LogRecord.ToString()).First();
                logRecordDbConfigFeedVersion.LastProcessedFeedVersion = (long)feedManager.LogRecordFeedContainer.LastFeedVersion;
                logRecordDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                logRecordDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbLogRecords into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long logRecordsInserted = await DbLogRecordService.InsertAsync(connectionInfo, dbLogRecords, logRecordDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)feedManager.LogRecordFeedContainer.FeedResultData.Count / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {feedManager.LogRecordFeedContainer.FeedResultData.Count.ToString()} records into {Globals.ConfigurationManager.DbLogRecordTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                feedManager.LogRecordFeedContainer.FeedResultData.Clear();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Processes <see cref="StatusData"/> entities returned by the data feed.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ProcessStatusDataFeedResultsAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Add any returned StatusData entities to the database, filtering-out those representing Devices or Diagnostics that are not being tracked. Include persistence of the feed version within the same transaction to prevent issues in the event that an exception occurs between persistence of the data and persistence of the feed version.
            if (feedManager.StatusDataFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultStatusDatas = feedManager.StatusDataFeedContainer.GetFeedResultDataValuesList<StatusData>();
                var filteredStatusDatas = ApplyTrackedDevicesFilterToList<StatusData>(feedResultStatusDatas);
                filteredStatusDatas = ApplyTrackedDiagnosticsFilterToList<StatusData>(filteredStatusDatas);

                // Map StatusData entities to DbStatusData entities.
                var dbStatusDatas = ObjectMapper.GetDbStatusDatas(filteredStatusDatas);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var statusDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.StatusData.ToString()).First();
                statusDataDbConfigFeedVersion.LastProcessedFeedVersion = (long)feedManager.StatusDataFeedContainer.LastFeedVersion;
                statusDataDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                statusDataDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbStatusData entities into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long statusDataEntitiesInserted = await DbStatusDataService.InsertAsync(connectionInfo, dbStatusDatas, statusDataDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)statusDataEntitiesInserted / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {statusDataEntitiesInserted.ToString()} records into {Globals.ConfigurationManager.DbStatusDataTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                feedManager.StatusDataFeedContainer.FeedResultData.Clear();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Processes <see cref="Trip"/> entities returned by the data feed.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task ProcessTripFeedResultsAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Add any returned Trip entities to the database, filtering-out those representing Devices that are not being tracked. Include persistence of the feed version within the same transaction to prevent issues in the event that an exception occurs between persistence of the data and persistence of the feed version.
            if (feedManager.TripFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultTrips = feedManager.TripFeedContainer.GetFeedResultDataValuesList<Trip>();
                var filteredTrips = ApplyTrackedDevicesFilterToList<Trip>(feedResultTrips);

                // Map Trip entities to DbTrip entities.
                List<DbTrip> dbTripList = ObjectMapper.GetDbTrips(filteredTrips);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var tripDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.Trip.ToString()).First();
                tripDbConfigFeedVersion.LastProcessedFeedVersion = (long)feedManager.TripFeedContainer.LastFeedVersion;
                tripDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                tripDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbTrip entities into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long tripsNoInserted = await DbTripService.InsertAsync(connectionInfo, dbTripList, tripDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)tripsNoInserted / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {tripsNoInserted.ToString()} records into {Globals.ConfigurationManager.DbTripTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                feedManager.TripFeedContainer.FeedResultData.Clear();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Concurrently propagates all relevant cache updates to the database.
        /// </summary>
        /// <returns></returns>
        void PropagateAllCacheUpdatesToDatabase()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var propagateDeviceCacheUpdatesToDatabaseTask = PropagateDeviceCacheUpdatesToDatabaseAsync(cancellationTokenSource);
                    var propagateDiagnosticCacheUpdatesToDatabaseTask = PropagateDiagnosticCacheUpdatesToDatabaseAsync(cancellationTokenSource);
                    var propagateUserCacheUpdatesToDatabaseTask = PropagateUserCacheUpdatesToDatabaseAsync(cancellationTokenSource);
                    var propagateRuleCacheUpdatesToDatabaseTask = PropagateRuleCacheUpdatesToDatabaseAsync(cancellationTokenSource);
                    var propagateZoneCacheUpdatesToDatabaseTask = PropagateZoneCacheUpdatesToDatabaseAsync(cancellationTokenSource);

                    Task[] tasks = { propagateDeviceCacheUpdatesToDatabaseTask, propagateDiagnosticCacheUpdatesToDatabaseTask, propagateUserCacheUpdatesToDatabaseTask, propagateRuleCacheUpdatesToDatabaseTask, propagateZoneCacheUpdatesToDatabaseTask };

                    Task.WaitAll(tasks);
                }
                catch (AggregateException aggregateException)
                {
                    Globals.HandleConnectivityRelatedAggregateException(aggregateException, Globals.ConnectivityIssueType.Database, "One or more exceptions were encountered during propagation of cache updates to database due to an apparent loss of connectivity with the database.");
                }
                catch (TaskCanceledException taskCanceledException)
                {
                    string errorMessage = $"Task was cancelled. TaskCanceledException: \nMESSAGE [{taskCanceledException.Message}]; \nSOURCE [{taskCanceledException.Source}]; \nSTACK TRACE [{taskCanceledException.StackTrace}]";
                    logger.Warn(errorMessage);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Propagates <see cref="Device"/> information received from MyGeotab to the database - inserting new records, updating existing records (if the values of any of the utilized fields have changed), and marking as deleted any database device records that no longer exist in MyGeotab (based on matching on ID). 
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task PropagateDeviceCacheUpdatesToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
            if (cacheManager.DeviceCacheContainer.LastUpdatedTimeUtc > cacheManager.DeviceCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbDevicesToInsert = new List<DbDevice>();
                var dbDevicesToUpdate = new List<DbDevice>();

                // Get cached devices.
                var deviceCache = (Dictionary<Id, Device>)cacheManager.DeviceCacheContainer.Cache;

                // Find any devices that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbDevicesDictionary.Any())
                {
                    foreach (DbDevice dbDevice in dbDevicesDictionary.Values.ToList())
                    {
                        if (dbDevice.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool deviceExistsInCache = deviceCache.ContainsKey(Id.Create(dbDevice.Id));
                            if (!deviceExistsInCache)
                            {
                                logger.Debug($"Device '{dbDevice.Id}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbDevice.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbDevice.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbDevice.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbDevicesDictionary[Id.Create(dbDevice.Id)] = dbDevice;
                                dbDevicesToUpdate.Add(dbDevice);
                            }
                        }
                    }
                }

                // Iterate through cached devices.
                foreach (Device cachedDevice in deviceCache.Values.ToList())
                {
                    // Try to find the existing database record for the cached device.
                    if (dbDevicesDictionary.TryGetValue(cachedDevice.Id, out var existingDbDevice))
                    {
                        // The device has already been added to the database.
                        bool dbDeviceRequiresUpdate = ObjectMapper.DbDeviceRequiresUpdate(existingDbDevice, cachedDevice);
                        if (dbDeviceRequiresUpdate)
                        {
                            DbDevice updatedDbDevice = ObjectMapper.GetDbDevice(cachedDevice);
                            updatedDbDevice.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                            updatedDbDevice.RecordLastChangedUtc = recordChangedTimestampUtc;
                            updatedDbDevice.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

                            dbDevicesDictionary[Id.Create(updatedDbDevice.Id)] = updatedDbDevice;
                            dbDevicesToUpdate.Add(updatedDbDevice);
                        }
                    }
                    else
                    {
                        // The device has not yet been added to the database. Create a DbDevice, set its properties and add it to the cache.
                        DbDevice newDbDevice = ObjectMapper.GetDbDevice(cachedDevice);
                        newDbDevice.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                        newDbDevice.RecordLastChangedUtc = recordChangedTimestampUtc;
                        newDbDevice.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;
                        dbDevicesDictionary.Add(Id.Create(newDbDevice.Id), newDbDevice);
                        dbDevicesToInsert.Add(newDbDevice);

                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Send any inserts to the database.
                if (dbDevicesToInsert.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long deviceEntitiesInserted = await DbDeviceService.InsertAsync(connectionInfo, dbDevicesToInsert, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)deviceEntitiesInserted / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed insertion of {deviceEntitiesInserted.ToString()} records into {Globals.ConfigurationManager.DbDeviceTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }

                // Send any updates/deletes to the database.
                if (dbDevicesToUpdate.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long deviceEntitiesUpdated = await DbDeviceService.UpdateAsync(connectionInfo, dbDevicesToUpdate, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)deviceEntitiesUpdated / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed updating of {deviceEntitiesUpdated.ToString()} records in {Globals.ConfigurationManager.DbDeviceTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                cacheManager.DeviceCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
            }
            else
            {
                logger.Debug($"Device cache in database is up-to-date.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Propagates <see cref="Diagnostic"/> information received from MyGeotab to the database - inserting new records, updating existing records (if the values of any of the utilized fields have changed), and marking as deleted any database diagnostic records that no longer exist in MyGeotab (based on matching on ID). 
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task PropagateDiagnosticCacheUpdatesToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
            if (cacheManager.DiagnosticCacheContainer.LastUpdatedTimeUtc > cacheManager.DiagnosticCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbDiagnosticsToInsert = new List<DbDiagnostic>();
                var dbDiagnosticsToUpdate = new List<DbDiagnostic>();

                // Get cached diagnostics.
                var diagnosticCache = (Dictionary<Id, Diagnostic>)cacheManager.DiagnosticCacheContainer.Cache;

                // Find any diagnostics that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbDiagnosticsDictionary.Any())
                {
                    foreach (DbDiagnostic dbDiagnostic in dbDiagnosticsDictionary.Values.ToList())
                    {
                        if (dbDiagnostic.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool diagnosticExistsInCache = diagnosticCache.ContainsKey(Id.Create(dbDiagnostic.Id));
                            if (!diagnosticExistsInCache)
                            {
                                logger.Debug($"Diagnostic '{dbDiagnostic.Id}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbDiagnostic.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbDiagnostic.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbDiagnostic.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbDiagnosticsDictionary[Id.Create(dbDiagnostic.Id)] = dbDiagnostic;
                                dbDiagnosticsToUpdate.Add(dbDiagnostic);
                            }
                        }
                    }
                }

                // Iterate through cached diagnostics.
                foreach (Diagnostic cachedDiagnostic in diagnosticCache.Values.ToList())
                {
                    // Try to find the existing database record for the cached diagnostic.
                    if (dbDiagnosticsDictionary.TryGetValue(cachedDiagnostic.Id, out var existingDbDiagnostic))
                    {
                        // The diagnostic has already been added to the database.
                        bool dbDiagnosticRequiresUpdate = ObjectMapper.DbDiagnosticRequiresUpdate(existingDbDiagnostic, cachedDiagnostic);
                        if (dbDiagnosticRequiresUpdate)
                        {
                            DbDiagnostic updatedDbDiagnostic = ObjectMapper.GetDbDiagnostic(cachedDiagnostic);
                            updatedDbDiagnostic.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                            updatedDbDiagnostic.RecordLastChangedUtc = recordChangedTimestampUtc;
                            updatedDbDiagnostic.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

                            dbDiagnosticsDictionary[Id.Create(updatedDbDiagnostic.Id)] = updatedDbDiagnostic;
                            dbDiagnosticsToUpdate.Add(updatedDbDiagnostic);
                        }
                    }
                    else
                    {
                        // The diagnostic has not yet been added to the database. Create a DbDiagnostic, set its properties and add it to the cache.
                        DbDiagnostic newDbDiagnostic = ObjectMapper.GetDbDiagnostic(cachedDiagnostic);
                        newDbDiagnostic.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                        newDbDiagnostic.RecordLastChangedUtc = recordChangedTimestampUtc;
                        newDbDiagnostic.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;
                        dbDiagnosticsDictionary.Add(Id.Create(newDbDiagnostic.Id), newDbDiagnostic);
                        dbDiagnosticsToInsert.Add(newDbDiagnostic);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Send any inserts to the database.
                if (dbDiagnosticsToInsert.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long diagnosticEntitiesInserted = await DbDiagnosticService.InsertAsync(connectionInfo, dbDiagnosticsToInsert, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)diagnosticEntitiesInserted / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed insertion of {diagnosticEntitiesInserted.ToString()} records into {Globals.ConfigurationManager.DbDiagnosticTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }

                // Send any updates/deletes to the database.
                if (dbDiagnosticsToUpdate.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long diagnosticEntitiesUpdated = await DbDiagnosticService.UpdateAsync(connectionInfo, dbDiagnosticsToUpdate, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)diagnosticEntitiesUpdated / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed updating of {diagnosticEntitiesUpdated.ToString()} records in {Globals.ConfigurationManager.DbDiagnosticTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                cacheManager.DiagnosticCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
            }
            else
            {
                logger.Debug($"Diagnostic cache in database is up-to-date.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Propagates <see cref="Geotab.Checkmate.ObjectModel.Exceptions.Rule"/> information received from MyGeotab to the database - inserting new records, updating existing records (if the values of any of the utilized fields have changed), and marking as deleted any database device records that no longer exist in MyGeotab (based on matching on ID). 
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task PropagateRuleCacheUpdatesToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
            if (cacheManager.RuleCacheContainer.LastUpdatedTimeUtc > cacheManager.RuleCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbRuleObjectsToInsert = new List<DbRuleObject>();
                var dbRuleObjectsToUpdate = new List<DbRuleObject>();


                // Get cached rules (Geotab object).
                var ruleCache = (Dictionary<Id, Geotab.Checkmate.ObjectModel.Exceptions.Rule>)cacheManager.RuleCacheContainer.Cache;

                // Find any rules that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbRuleObjectDictionary.Any())
                {
                    foreach (DbRuleObject dbRuleObject in dbRuleObjectDictionary.Values.ToList())
                    {
                        if (dbRuleObject.DbRule.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool ruleExistsInCache = ruleCache.ContainsKey(Id.Create(dbRuleObject.DbRule.Id));
                            if (!ruleExistsInCache)
                            {
                                logger.Debug($"Rule '{dbRuleObject.DbRule.Id}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbRuleObject.DbRule.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbRuleObject.DbRule.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbRuleObject.DbRule.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbRuleObjectDictionary[Id.Create(dbRuleObject.Id)] = dbRuleObject;
                                dbRuleObjectsToUpdate.Add(dbRuleObject);
                            }
                        }
                    }
                }

                // Add any new objects from the Geotab object list to the cached Db object list or update any existing objects that have changed.
                double cachedRuleCount = (Double)ruleCache.Count;
                double cachedRulesProcessed = 0;
                foreach (Geotab.Checkmate.ObjectModel.Exceptions.Rule cachedRule in ruleCache.Values.ToList())
                {
                    // Log progress.
                    cachedRulesProcessed++;
                    double cachedRulesProcessedPercentage = (cachedRulesProcessed / cachedRuleCount);
                    logger.Debug($"Processing cached Rule objects in preparation for database update: {cachedRulesProcessed.ToString()} of {cachedRuleCount.ToString()} ({cachedRulesProcessedPercentage.ToString("P")})");

                    // Try to find the existing database record for the cached rule.
                    if (dbRuleObjectDictionary.TryGetValue(cachedRule.Id, out var existingDbRuleObject))
                    {
                        // The rule has already been added to the database - therefore update it.
                        bool dbRuleRequiresUpdate = ObjectMapper.DbRuleObjectRequiresUpdate(existingDbRuleObject, cachedRule);
                        if (dbRuleRequiresUpdate)
                        {
                            DbRuleObject updatedDbRuleObject = new DbRuleObject();
                            updatedDbRuleObject.BuildRuleObject(cachedRule, (int)Common.DatabaseRecordStatus.Active, recordChangedTimestampUtc,
                                Common.DatabaseWriteOperationType.Update);

                            dbRuleObjectDictionary[Id.Create(updatedDbRuleObject.Id)] = updatedDbRuleObject;
                            dbRuleObjectsToUpdate.Add(updatedDbRuleObject);
                        }
                    }
                    else
                    {
                        // The rule has not yet been added to the database. Create a DbRule, set its properties and add it to the cache.
                        DbRuleObject newDbRuleObject = new DbRuleObject();
                        newDbRuleObject.BuildRuleObject(cachedRule, (int)Common.DatabaseRecordStatus.Active, recordChangedTimestampUtc,
                            Common.DatabaseWriteOperationType.Insert);
                        dbRuleObjectDictionary[Id.Create(newDbRuleObject.Id)] = newDbRuleObject;
                        dbRuleObjectsToInsert.Add(newDbRuleObject);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Send any inserts to the database.
                if (dbRuleObjectsToInsert.Any())
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    int ruleObjectEntitiesInserted = await RuleHelper.InsertDbRuleObjectListAsync(dbRuleObjectsToInsert, cancellationTokenSource);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)ruleObjectEntitiesInserted / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {ruleObjectEntitiesInserted.ToString()} records into the {Globals.ConfigurationManager.DbRuleTableName} table, along with its related conditions in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                }

                // Send any updates / deletes to the database.
                if (dbRuleObjectsToUpdate.Any())
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    int ruleObjectEntitiesUpdated = RuleHelper.UpdateDbRuleObjectsToDatabase(dbRuleObjectsToUpdate, cancellationTokenSource);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)ruleObjectEntitiesUpdated / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed updating of {ruleObjectEntitiesUpdated.ToString()} records in the {Globals.ConfigurationManager.DbRuleTableName} table, along with its related conditions in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                }
                cacheManager.RuleCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
            }
            else
            {
                logger.Debug($"Rule cache in database is up-to-date.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Propagates <see cref="User"/> information received from MyGeotab to the database - inserting new records, updating existing records (if the values of any of the utilized fields have changed), and marking as deleted any database user records that no longer exist in MyGeotab (based on matching on ID). 
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task PropagateUserCacheUpdatesToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
            if (cacheManager.UserCacheContainer.LastUpdatedTimeUtc > cacheManager.UserCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbUsersToInsert = new List<DbUser>();
                var dbUsersToUpdate = new List<DbUser>();

                // Get cached users.
                var userCache = (Dictionary<Id, User>)cacheManager.UserCacheContainer.Cache;

                // Find any users that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbUsersDictionary.Any())
                {
                    foreach (DbUser dbUser in dbUsersDictionary.Values.ToList())
                    {
                        if (dbUser.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool userExistsInCache = userCache.ContainsKey(Id.Create(dbUser.Id));
                            if (!userExistsInCache)
                            {
                                logger.Debug($"User '{dbUser.Id}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbUser.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbUser.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbUser.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbUsersDictionary[Id.Create(dbUser.Id)] = dbUser;
                                dbUsersToUpdate.Add(dbUser);
                            }
                        }
                    }
                }

                // Iterate through cached users.
                foreach (User cachedUser in userCache.Values.ToList())
                {
                    // Try to find the existing database record for the cached user.
                    if (dbUsersDictionary.TryGetValue(cachedUser.Id, out var existingDbUser))
                    {
                        // The user has already been added to the database.
                        bool dbUserRequiresUpdate = ObjectMapper.DbUserRequiresUpdate(existingDbUser, cachedUser);
                        if (dbUserRequiresUpdate)
                        {
                            DbUser updatedDbUser = ObjectMapper.GetDbUser(cachedUser);
                            updatedDbUser.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                            updatedDbUser.RecordLastChangedUtc = recordChangedTimestampUtc;
                            updatedDbUser.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                            dbUsersDictionary[Id.Create(updatedDbUser.Id)] = updatedDbUser;
                            dbUsersToUpdate.Add(updatedDbUser);
                        }
                    }
                    else
                    {
                        // The user has not yet been added to the database. Create a DbUser, set its properties and add it to the cache.
                        DbUser newDbUser = ObjectMapper.GetDbUser(cachedUser);
                        newDbUser.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                        newDbUser.RecordLastChangedUtc = recordChangedTimestampUtc;
                        newDbUser.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;
                        dbUsersDictionary.Add(Id.Create(newDbUser.Id), newDbUser);
                        dbUsersToInsert.Add(newDbUser);

                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Send any inserts to the database.
                if (dbUsersToInsert.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long userEntitiesInserted = await DbUserService.InsertAsync(connectionInfo, dbUsersToInsert, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)userEntitiesInserted / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed insertion of {userEntitiesInserted.ToString()} records into {Globals.ConfigurationManager.DbUserTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }

                // Send any updates/deletes to the database.
                if (dbUsersToUpdate.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long userEntitiesUpdated = await DbUserService.UpdateAsync(connectionInfo, dbUsersToUpdate, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)userEntitiesUpdated / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed updating of {userEntitiesUpdated.ToString()} records in {Globals.ConfigurationManager.DbUserTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                cacheManager.UserCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
            }
            else
            {
                logger.Debug($"User cache in database is up-to-date.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Propagates <see cref="Zone"/> information received from MyGeotab to the database - inserting new records, updating existing records (if the values of any of the utilized fields have changed), and marking as deleted any database zone records that no longer exist in MyGeotab (based on matching on ID). 
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task PropagateZoneCacheUpdatesToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
            if (cacheManager.ZoneCacheContainer.LastUpdatedTimeUtc > cacheManager.ZoneCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbZonesToInsert = new List<DbZone>();
                var dbZonesToUpdate = new List<DbZone>();

                // Get cached zones.
                var zoneCache = (Dictionary<Id, Zone>)cacheManager.ZoneCacheContainer.Cache;

                // Find any zones that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbZonesDictionary.Any())
                {
                    foreach (DbZone dbZone in dbZonesDictionary.Values.ToList())
                    {
                        if (dbZone.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool zoneExistsInCache = zoneCache.ContainsKey(Id.Create(dbZone.Id));
                            if (!zoneExistsInCache)
                            {
                                logger.Debug($"Zone '{dbZone.Id}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbZone.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbZone.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbZone.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbZonesDictionary[Id.Create(dbZone.Id)] = dbZone;
                                dbZonesToUpdate.Add(dbZone);
                            }
                        }
                    }
                }

                // Iterate through cached zones.
                foreach (Zone cachedZone in zoneCache.Values.ToList())
                {
                    // Try to find the existing database record for the cached zone.
                    if (dbZonesDictionary.TryGetValue(cachedZone.Id, out var existingDbZone))
                    {
                        // The zone has already been added to the database.
                        bool dbZoneRequiresUpdate = ObjectMapper.DbZoneRequiresUpdate(existingDbZone, cachedZone);
                        if (dbZoneRequiresUpdate)
                        {
                            DbZone updatedDbZone = ObjectMapper.GetDbZone(cachedZone);
                            updatedDbZone.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                            updatedDbZone.RecordLastChangedUtc = recordChangedTimestampUtc;
                            updatedDbZone.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                            dbZonesDictionary[Id.Create(updatedDbZone.Id)] = updatedDbZone;
                            dbZonesToUpdate.Add(updatedDbZone);
                        }
                    }
                    else
                    {
                        // The zone has not yet been added to the database. Create a DbZone, set its properties and add it to the cache.
                        DbZone newDbZone = ObjectMapper.GetDbZone(cachedZone);
                        newDbZone.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                        newDbZone.RecordLastChangedUtc = recordChangedTimestampUtc;
                        newDbZone.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;
                        dbZonesDictionary.Add(Id.Create(newDbZone.Id), newDbZone);
                        dbZonesToInsert.Add(newDbZone);

                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Send any inserts to the database.
                if (dbZonesToInsert.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long zoneEntitiesInserted = await DbZoneService.InsertAsync(connectionInfo, dbZonesToInsert, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)zoneEntitiesInserted / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed insertion of {zoneEntitiesInserted.ToString()} records into {Globals.ConfigurationManager.DbZoneTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }

                // Send any updates/deletes to the database.
                if (dbZonesToUpdate.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long zoneEntitiesUpdated = await DbZoneService.UpdateAsync(connectionInfo, dbZonesToUpdate, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)zoneEntitiesUpdated / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed updating of {zoneEntitiesUpdated.ToString()} records in {Globals.ConfigurationManager.DbZoneTableName} table in {elapsedTime.TotalSeconds.ToString()} seconds ({recordsProcessedPerSecond.ToString()} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                cacheManager.ZoneCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
            }
            else
            {
                logger.Debug($"Zone cache in database is up-to-date.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Roll-back LastFeedVersion values in FeedManager so that any feed results that were lost will be re-acquired.
        /// </summary>
        /// <returns></returns>
        async Task RollbackFeedContainerLastFeedVersionsAsync()
        {
            try
            {
                // First, refresh the list of DbConfigFeedVersions to capture any updates that may have resulted from committed transactions.
                dbConfigFeedVersions = await InitializeDbConfigFeedVersionListAsync();
                feedManager.RollbackFeedContainerLastFeedVersions(dbConfigFeedVersions);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Worker process caught an exception: \nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
                logger.Error(errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        /// <summary>
        /// Starts the current <see cref="Worker"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Stops the current <see cref="Worker"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info("Worker stopped.");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Builds a list of <see cref="DefectListPartDefect"/> objects by retrieving all defect lists in the database and flattening the trees - capturing the defect list, part and part defect information required when processing <see cref="DVIRDefect"/> information associated with <see cref="DVIRLog"/>s. 
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateDefectListPartDefectCacheAsync(CancellationTokenSource cancellationTokenSource)
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
            defectListPartDefectCacheExpiryTime = currentTime.AddMinutes(Globals.ConfigurationManager.DVIRDefectListCacheRefreshIntervalMinutes);

            // Get all defect lists.
            DefectSearch defectSearch = new DefectSearch
            {
                IncludeAllTrees = true
            };

            List<Defect> defectLists;
            try
            {
                CancellationTokenSource timeoutcancellationTokenSource = new CancellationTokenSource();
                timeoutcancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(Globals.ConfigurationManager.TimeoutSecondsForMyGeotabTasks));

                Task<List<Defect>> defectListsTask = Task.Run(() => Globals.MyGeotabAPI.CallAsync<List<Defect>>("Get", typeof(Defect), new { search = defectSearch }));

                defectLists = await defectListsTask;
            }
            catch (OperationCanceledException exception)
            {
                cancellationTokenSource.Cancel();
                throw new MyGeotabConnectionException($"MyGeotab API GetAsync call for type '{typeof(Defect).Name}' did not return within the allowed time of {Globals.ConfigurationManager.TimeoutSecondsForMyGeotabTasks.ToString()} seconds. This may be due to a loss of connectivity with the MyGeotab server.", exception);
            }

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
                            logger.Debug($"partDefect '{partDefect.Id.ToString()} ({partDefect.Name})' is not a Defect.");
                        }
                    }
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Obtains the <see cref="VersionInformation"/> for the current MyGeotab database, logs this information and performs validation to prevent mixing if data from multiple MyGeotab database in a single MyGeotab API Adapter database.
        /// </summary>
        /// <returns></returns>
        async Task ValidateMyGeotabVersionInformationAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            VersionInformation versionInformation;
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(Globals.ConfigurationManager.TimeoutSecondsForMyGeotabTasks));

                Task<VersionInformation> versionInformationTask = Task.Run(() => Globals.MyGeotabAPI.CallAsync<VersionInformation>("GetVersionInformation"));

                versionInformation = await versionInformationTask;
            }
            catch (OperationCanceledException exception)
            {
                throw new MyGeotabConnectionException($"MyGeotab API GetVersionInformationAsync call did not return within the allowed time of {Globals.ConfigurationManager.TimeoutSecondsForMyGeotabTasks.ToString()} seconds. This may be due to a loss of connectivity with the MyGeotab server.", exception);
            }
            catch (Exception exception)
            {
                // If the exception is related to connectivity, wrap it in a MyGeotabConnectionException. Otherwise, just pass it along.
                if (Globals.ExceptionIsRelatedToMyGeotabConnectivityLoss(exception))
                {
                    throw new MyGeotabConnectionException("An exception occurred while attempting to authenticate the MyGeotab API.", exception);
                }
                else
                {
                    throw;
                }
            }

            logger.Info($"Version information for MyGeotab database '{Globals.MyGeotabAPI.Database}' on server '{Globals.MyGeotabAPI.Server}': Server='{versionInformation.Application.Build}-{versionInformation.Application.Branch}-{versionInformation.Application.Commit}', Database='{versionInformation.Database}', GoTalk='{versionInformation.GoTalk}'");

            try
            {
                // Create a new DbMyGeotabVersionInfo entity with info from the VersionInformation of the current MyGeotab database.
                var newDbMyGeotabVersionInfo = new DbMyGeotabVersionInfo
                {
                    ApplicationBranch = versionInformation.Application.Branch,
                    ApplicationBuild = versionInformation.Application.Build,
                    ApplicationCommit = versionInformation.Application.Commit,
                    DatabaseName = Globals.MyGeotabAPI.Database,
                    DatabaseVersion = versionInformation.Database,
                    GoTalkVersion = versionInformation.GoTalk,
                    RecordCreationTimeUtc = DateTime.UtcNow,
                    Server = Globals.MyGeotabAPI.Server
                };
                var dbMyGeotabVersionInfos = await DbMyGeotabVersionInfoService.GetAllAsync(connectionInfo, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);


                if (!dbMyGeotabVersionInfos.Any())
                {
                    // Insert the new DbMyGeotabVersionInfo entity into the database.
                    PropagateDbMyGeotabVersionInfoToDatabase(newDbMyGeotabVersionInfo);
                }
                else
                {
                    // Get the latest DbMyGeotabVersionInfo from the database.
                    var latestDbMyGeotabVersionInfo = dbMyGeotabVersionInfos.Last();

                    // Throw an exception if name of the current MyGeotab database doesn't match that for which data has been written to the database.
                    if (newDbMyGeotabVersionInfo.DatabaseName != latestDbMyGeotabVersionInfo.DatabaseName)
                    {
                        string errorMessage = $"The MyGeotab database to which this application is currently connected ('{newDbMyGeotabVersionInfo.DatabaseName}') does not match that for which data has been stored in the MyGeotab API Adapter database ('{latestDbMyGeotabVersionInfo.DatabaseName}'). Data from multiple MyGeotab databases cannot be stored in a single MyGeotab API Adapter database.";
                        logger.Error(errorMessage);
                        throw new Exception(errorMessage);
                    }

                    // If any of the properties other than DatabaseName have changed, insert the new DbMyGeotabVersionInfo entity into the database.
                    if (newDbMyGeotabVersionInfo.ApplicationBranch != latestDbMyGeotabVersionInfo.ApplicationBranch || newDbMyGeotabVersionInfo.ApplicationBuild != latestDbMyGeotabVersionInfo.ApplicationBuild || newDbMyGeotabVersionInfo.ApplicationCommit != latestDbMyGeotabVersionInfo.ApplicationCommit || newDbMyGeotabVersionInfo.DatabaseVersion != latestDbMyGeotabVersionInfo.DatabaseVersion || newDbMyGeotabVersionInfo.GoTalkVersion != latestDbMyGeotabVersionInfo.GoTalkVersion || newDbMyGeotabVersionInfo.Server != latestDbMyGeotabVersionInfo.Server)
                    {
                        PropagateDbMyGeotabVersionInfoToDatabase(newDbMyGeotabVersionInfo);
                    }
                }
            }
            catch (DatabaseConnectionException databaseConnectionException)
            {
                HandleException(databaseConnectionException, NLog.LogLevel.Error, "Worker process caught an exception");
            }
            catch (Exception)
            {
                throw;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Inserts the supplied <see cref="DbMyGeotabVersionInfo"/> entity into the database.
        /// </summary>
        /// <param name="dbMyGeotabVersionInfo">The <see cref="DbMyGeotabVersionInfo"/> to be inserted.</param>
        /// <returns></returns>
        void PropagateDbMyGeotabVersionInfoToDatabase(DbMyGeotabVersionInfo dbMyGeotabVersionInfo)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var timeout = TimeSpan.FromSeconds(Globals.ConfigurationManager.TimeoutSecondsForMyGeotabTasks);
            using (var cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                try
                {
                    var insertDbMyGeotabVersionInfoTask = DbMyGeotabVersionInfoService.InsertAsync(connectionInfo, dbMyGeotabVersionInfo, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks); ;

                    Task[] tasks = { insertDbMyGeotabVersionInfoTask };

                    try
                    {
                        if (!Task.WaitAll(tasks, Globals.ConfigurationManager.TimeoutMillisecondsForDatabaseTasks))
                        {
                            throw new DatabaseConnectionException($"The MyGeotab database version information was not propagated to the database within the allowed time of {Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks.ToString()} seconds. This may be due to a database connectivity issue.");
                        }
                    }
                    catch (AggregateException aggregateException)
                    {
                        cancellationTokenSource.Cancel();
                        Globals.HandleConnectivityRelatedAggregateException(aggregateException, Globals.ConnectivityIssueType.Database, "One or more exceptions were encountered during propagation of MyGeotab database version information to database due to an apparent loss of connectivity with the database.");
                    }
                }
                catch (TaskCanceledException taskCanceledException)
                {
                    string errorMessage = $"Task was cancelled. TaskCanceledException: \nMESSAGE [{taskCanceledException.Message}]; \nSOURCE [{taskCanceledException.Source}]; \nSTACK TRACE [{taskCanceledException.StackTrace}]";
                    logger.Warn(errorMessage);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Iteratively tests MyGeotab or database connectivity, depending on which was lost, until connectivity is restored.
        /// </summary>
        /// <param name="reasonForConnectivityLoss">The reason for loss of connectivity.</param>
        /// <returns></returns>        
        async Task WaitForConnectivityRestorationAsync(StateReason reasonForConnectivityLoss)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            StateMachine.CurrentState = State.Waiting;
            StateMachine.Reason = reasonForConnectivityLoss;
            logger.Warn($"******** CONNECTIVITY LOST. REASON: '{StateMachine.Reason.ToString()}'. WAITING FOR RESTORATION OF CONNECTIVITY...");
            while (StateMachine.CurrentState == State.Waiting)
            {
                // Wait for the prescribed interval between connectivity checks.
                await Task.Delay(ConnectivityRestorationCheckIntervalMilliseconds);

                logger.Warn($"{StateMachine.Reason.ToString()}; continuing to wait for restoration of connectivity...");
                switch (StateMachine.Reason)
                {
                    case StateReason.DatabaseNotAvailable:
                        if (await StateMachine.IsDatabaseAccessibleAsync(connectionInfo) == true)
                        {
                            logger.Warn("******** CONNECTIVITY RESTORED.");
                            if (initializationCompleted == true)
                            {
                                await RollbackFeedContainerLastFeedVersionsAsync();
                            }
                            StateMachine.CurrentState = State.Normal;
                            continue;
                        }
                        break;
                    case StateReason.MyGeotabNotAvailable:
                        if (await StateMachine.IsMyGeotabAccessibleAsync() == true)
                        {
                            logger.Warn("******** CONNECTIVITY RESTORED.");
                            if (initializationCompleted == true)
                            {
                                await RollbackFeedContainerLastFeedVersionsAsync();
                            }
                            StateMachine.CurrentState = State.Normal;
                            continue;
                        }
                        break;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
