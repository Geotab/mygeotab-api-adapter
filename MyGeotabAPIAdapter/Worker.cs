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
        IDictionary<Id, DbZoneType> dbZoneTypesDictionary;
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

            var ruleCache = CacheManager.RuleCacheContainer.Cache;
            List<ExceptionEvent> filteredList = new();
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

            List<T> filteredList = new();
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
                        case nameof(DVIRLog):
                            var dvirLogToBeEvaluated = itemToBeEvaluated as DVIRLog;
                            itemToBeEvaluatedDevice = dvirLogToBeEvaluated.Device;
                            break;
                        case nameof(ExceptionEvent):
                            var exceptionEventToBeEvaluated = itemToBeEvaluated as ExceptionEvent;
                            itemToBeEvaluatedDevice = exceptionEventToBeEvaluated.Device;
                            break;
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

            List<T> filteredList = new();
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
                    Dictionary<Id, Device> deviceCache = (Dictionary<Id, Device>)CacheManager.DeviceCacheContainer.Cache;
                    string[] deviceList = Globals.ConfigurationManager.DevicesToTrackList.Split(",");
                    for (int deviceListIndex = 0; deviceListIndex < deviceList.Length; deviceListIndex++)
                    {
                        var deviceId = Id.Create(deviceList[deviceListIndex]);
                        if (deviceCache.ContainsKey(deviceId))
                        {
                            var checkedDevice = deviceCache[deviceId];
                            if (!trackedDevicesDictionary.ContainsKey(checkedDevice.Id))
                            {
                                trackedDevicesDictionary.Add(checkedDevice.Id, checkedDevice);
                            }
                            else
                            {
                                logger.Warn($"The value '{deviceId}' is contained multiple times in the '{Globals.ConfigurationManager.ArgNameDevicesToTrack}' setting in the appsettngs.json file. This instance will be ignored.");
                            }
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
                    Dictionary<Id, Diagnostic> diagnosticCache = (Dictionary<Id, Diagnostic>)CacheManager.DiagnosticCacheContainer.Cache;
                    string[] diagnosticList = Globals.ConfigurationManager.DiagnosticsToTrackList.Split(",");
                    for (int diagnosticListIndex = 0; diagnosticListIndex < diagnosticList.Length; diagnosticListIndex++)
                    {
                        var diagnosticId = Id.Create(diagnosticList[diagnosticListIndex]);
                        if (diagnosticCache.ContainsKey(diagnosticId))
                        {
                            var checkedDiagnostic = diagnosticCache[diagnosticId];
                            if (!trackedDiagnosticsDictionary.ContainsKey(checkedDiagnostic.Id))
                            {
                                trackedDiagnosticsDictionary.Add(checkedDiagnostic.Id, checkedDiagnostic);
                            }
                            else
                            {
                                logger.Warn($"The value '{diagnosticId}' is contained multiple times in the '{Globals.ConfigurationManager.ArgNameDiagnosticsToTrack}' setting in the appsettngs.json file. This instance will be ignored.");
                            }
                        }
                        else
                        {
                            logger.Warn($"'{diagnosticId}' is not a valid diagnostic Id; as such the intended diagnostic will not be tracked.");
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

                    await UpdateAllCachesAndPersistToDatabaseAsync();
                    BuildTrackedDevicesDictionary();
                    BuildTrackedDiagnosticsDictionary();
                    await GetAllFeedDataAndPersistToDatabaseAsync();

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
        /// Retrieves data from all feeds and persists the data to the database.
        /// </summary>
        /// <returns></returns>
        async Task GetAllFeedDataAndPersistToDatabaseAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var getLogRecordFeedDataAndPersistToDatabaseAsyncTask = GetLogRecordFeedDataAndPersistToDatabaseAsync(cancellationTokenSource);
                    var getStatusDataFeedDataAndPersistToDatabaseAsyncTask = GetStatusDataFeedDataAndPersistToDatabaseAsync(cancellationTokenSource);
                    var getFaultDataFeedDataAndPersistToDatabaseAsyncTask = GetFaultDataFeedDataAndPersistToDatabaseAsync(cancellationTokenSource);
                    var getDVIRLogFeedDataAndPersistToDatabaseAsyncTask = GetDVIRLogFeedDataAndPersistToDatabaseAsync(cancellationTokenSource);
                    var getTripFeedDataAndPersistToDatabaseAsyncTask = GetTripFeedDataAndPersistToDatabaseAsync(cancellationTokenSource);
                    var getExceptionEventFeedDataAndPersistToDatabaseAsyncTask = GetExceptionEventFeedDataAndPersistToDatabaseAsync(cancellationTokenSource);

                    Task[] tasks = { getLogRecordFeedDataAndPersistToDatabaseAsyncTask, getStatusDataFeedDataAndPersistToDatabaseAsyncTask, getFaultDataFeedDataAndPersistToDatabaseAsyncTask, getDVIRLogFeedDataAndPersistToDatabaseAsyncTask, getTripFeedDataAndPersistToDatabaseAsyncTask, getExceptionEventFeedDataAndPersistToDatabaseAsyncTask };

                    try
                    {
                        await Task.WhenAll(tasks);
                    }
                    catch (MyGeotabConnectionException myGeotabConnectionException)
                    {
                        throw new MyGeotabConnectionException($"One or more exceptions were encountered during data feed processing due to an apparent loss of connectivity with the MyGeotab server.", myGeotabConnectionException);
                    }
                    catch (DatabaseConnectionException databaseConnectionException)
                    {
                        throw new DatabaseConnectionException($"One or more exceptions were encountered during data feed processing due to an apparent loss of connectivity with the adapter database.", databaseConnectionException);
                    }
                    catch (AggregateException aggregateException)
                    {
                        Globals.HandleConnectivityRelatedAggregateException(aggregateException, Globals.ConnectivityIssueType.MyGeotabOrDatabase, "One or more exceptions were encountered during retrieval of data via feeds and persistance of data to database due to an apparent loss of connectivity with the MyGeotab server or the database.");
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
        /// Retrieves data from the <see cref="DVIRLog"/> feed and persists the data to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task GetDVIRLogFeedDataAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await FeedManager.GetFeedDataAsync<DVIRLog>(FeedManager.DVIRLogFeedContainer, cancellationTokenSource);
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessDVIRLogFeedResultsAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Retrieves data from the <see cref="ExceptionEvent"/> feed and persists the data to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task GetExceptionEventFeedDataAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await FeedManager.GetFeedDataAsync<ExceptionEvent>(FeedManager.ExceptionEventFeedContainer, cancellationTokenSource);
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessExceptionEventFeedResultsAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Retrieves data from the <see cref="FaultData"/> feed and persists the data to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task GetFaultDataFeedDataAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await FeedManager.GetFeedDataAsync<FaultData>(FeedManager.FaultDataFeedContainer, cancellationTokenSource);
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessFaultDataFeedResultsAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Retrieves data from the <see cref="LogRecord"/> feed and persists the data to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task GetLogRecordFeedDataAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await FeedManager.GetFeedDataAsync<LogRecord>(FeedManager.LogRecordFeedContainer, cancellationTokenSource);
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessLogRecordFeedResultsAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Retrieves data from the <see cref="StatusData"/> feed and persists the data to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task GetStatusDataFeedDataAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await FeedManager.GetFeedDataAsync<StatusData>(FeedManager.StatusDataFeedContainer, cancellationTokenSource);
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessStatusDataFeedResultsAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Retrieves data from the <see cref="Trip"/> feed and persists the data to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task GetTripFeedDataAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await FeedManager.GetFeedDataAsync<Trip>(FeedManager.TripFeedContainer, cancellationTokenSource);
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessTripFeedResultsAsync(cancellationTokenSource);

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
                    DbConfigFeedVersion newDbConfigFeedVersion = new()
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
                    var getAllDbZoneTypesTask = DbZoneTypeService.GetAllAsync(connectionInfo, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);

                    Task[] tasks = { getAllDbDevicesTask, getAllDbDiagnosticsTask, getAllDbUsersTask, getAllDbDVIRDefectsTask, getAllDbDVIRDefectRemarksTask, getAllDbRuleObjectsTask, getAllDbZonesTask, getAllDbZoneTypesTask };

                    Task.WaitAll(tasks);

                    // Sort lists on Id.
                    dbDevicesDictionary = getAllDbDevicesTask.Result.ToDictionary(device => Id.Create(device.GeotabId));
                    dbDiagnosticsDictionary = getAllDbDiagnosticsTask.Result.ToDictionary(diagnostic => Id.Create(diagnostic.GeotabId));
                    dbUsersDictionary = getAllDbUsersTask.Result.ToDictionary(user => Id.Create(user.GeotabId));
                    dbDVIRDefectsDictionary = getAllDbDVIRDefectsTask.Result.ToDictionary(dvirDefect => Id.Create(dvirDefect.GeotabId));
                    dbDVIRDefectRemarksDictionary = getAllDbDVIRDefectRemarksTask.Result.ToDictionary(dvirDefectRemark => Id.Create(dvirDefectRemark.GeotabId));
                    dbRuleObjectDictionary = getAllDbRuleObjectsTask.Result.ToDictionary(dbRuleObject => Id.Create(dbRuleObject.GeotabId));
                    dbZonesDictionary = getAllDbZonesTask.Result.ToDictionary(dbZone => Id.Create(dbZone.GeotabId));
                    dbZoneTypesDictionary = getAllDbZoneTypesTask.Result.ToDictionary(dbZoneType => Id.Create(dbZoneType.GeotabId));
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
                using (ConfigurationManager configurationManager = new(configuration))
                {
                    Globals.ConfigurationManager = configurationManager;
                }
                var assemblyName = GetType().Assembly.GetName().Name;
                var assemblyVersion = GetType().Assembly.GetName().Version.ToString();
                logger.Info($"******** INITIALIZING APPLICATION - {assemblyName} (v{assemblyVersion}) ********");
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
            if (FeedManager.DVIRLogFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultDVIRLogs = FeedManager.DVIRLogFeedContainer.GetFeedResultDataValuesList<DVIRLog>();
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
                                        updatedDbDVIRDefect.id = existingDbDVIRDefect.id;
                                        updatedDbDVIRDefect.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                                        updatedDbDVIRDefect.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        updatedDbDVIRDefect.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

                                        dbDVIRDefectsDictionary[Id.Create(updatedDbDVIRDefect.GeotabId)] = updatedDbDVIRDefect;
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

                                    dbDVIRDefectsDictionary[Id.Create(newDbDVIRDefect.GeotabId)] = newDbDVIRDefect;
                                    dbDVIRDefectsToInsert.Add(newDbDVIRDefect);
                                }
                            }
                            else
                            {
                                logger.Debug($"No DefectListPartDefect could be found for the Defect '{defect.Id} ({defect.Name})' associated with the DVIRDefect '{dvirDefect.Id}' of DVIRLog '{filteredDVIRLog.Id}'. This DVIRDefect will not be processed.");
                            }

                            // Process any DefectRemarks associated with the subject DVIRDefect.
                            if (dvirDefect.DefectRemarks != null)
                            {
                                foreach (var defectRemark in dvirDefect.DefectRemarks)
                                {
                                    // If the DefectRemark has not yet been added to the database, create a DbDVIRDefectRemark, set its properties and add it to the list for later write to the database.
                                    #region Temporary - Use DefectRemark DateTime as the Id
                                    // Pre-5.7.2004, the DefectRemark Ids change whenever a new DefectRemark is added to a DVIRLog, resulting in all existing DefectRemarks being duplicated in the adapter database each time a new one is added. Once all databases are at 5.7.2004+, the code can revert to the original commented-out version below which uses DefectRemark.Id.
                                    var dbDVIRDefectRemarksList = dbDVIRDefectRemarksDictionary.Values.ToList();
                                    if (!dbDVIRDefectRemarksList.Where(dbDVIRDefectRemark => dbDVIRDefectRemark.DateTime == defectRemark.DateTime && dbDVIRDefectRemark.Remark == defectRemark.Remark).Any())
                                    {
                                        DbDVIRDefectRemark newDbDVIRDefectRemark = ObjectMapper.GetDbDVIRDefectRemark(defectRemark);
                                        newDbDVIRDefectRemark.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                                        newDbDVIRDefectRemark.RecordLastChangedUtc = recordChangedTimestampUtc;
                                        newDbDVIRDefectRemark.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;

                                        dbDVIRDefectRemarksDictionary.Add(Id.Create(newDbDVIRDefectRemark.GeotabId), newDbDVIRDefectRemark);
                                        dbDVIRDefectRemarksToInsert.Add(newDbDVIRDefectRemark);
                                    }
                                    #endregion
                                    //if (!dbDVIRDefectRemarksDictionary.TryGetValue(defectRemark.Id, out var existingDbDVIRDefectRemark))
                                    //{
                                    //    DbDVIRDefectRemark newDbDVIRDefectRemark = ObjectMapper.GetDbDVIRDefectRemark(defectRemark);
                                    //    newDbDVIRDefectRemark.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                                    //    newDbDVIRDefectRemark.RecordLastChangedUtc = recordChangedTimestampUtc;
                                    //    newDbDVIRDefectRemark.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;

                                    //    dbDVIRDefectRemarksDictionary.Add(Id.Create(newDbDVIRDefectRemark.GeotabId), newDbDVIRDefectRemark);
                                    //    dbDVIRDefectRemarksToInsert.Add(newDbDVIRDefectRemark);
                                    //}
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
                    dvirLogDbConfigFeedVersion.LastProcessedFeedVersion = (long)FeedManager.DVIRLogFeedContainer.LastFeedVersion;
                    dvirLogDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                    dvirLogDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        string resultCounts = await DbDVIRLogService.PersistAllDVIRLogChangesToDatabase(connectionInfo, dbDVIRLogsForDatabaseWrite, dbDVIRDefectsToInsert, dbDVIRDefectsToUpdate, dbDVIRDefectRemarksToInsert, dvirLogDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);

                        string[] individualResultCounts = resultCounts.Split(",");
                        logger.Info($"Completed insertion of {individualResultCounts[0]} records into {ConfigurationManager.DbDVIRLogTableName} table, insertion of {individualResultCounts[1]} records into {ConfigurationManager.DbDVIRDefectTableName} table, update of {individualResultCounts[2]} records in {ConfigurationManager.DbDVIRDefectTableName} table and insertion of {individualResultCounts[3]} records into {ConfigurationManager.DbDVIRDefectRemarkTableName} table in {elapsedTime.TotalSeconds} seconds.");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }

                // Clear FeedResultData.
                FeedManager.DVIRLogFeedContainer.FeedResultData.Clear();
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
            if (FeedManager.ExceptionEventFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultExceptionEvents = FeedManager.ExceptionEventFeedContainer.GetFeedResultDataValuesList<ExceptionEvent>();
                var filteredExceptionEvents = ApplyRuleFilterToExceptionEventList(feedResultExceptionEvents);
                filteredExceptionEvents = ApplyTrackedDevicesFilterToList<ExceptionEvent>(filteredExceptionEvents);

                // Map ExceptionEvents to DbExceptionEvents.
                var dbExpectionEvents = ObjectMapper.GetDbExceptionEvents(filteredExceptionEvents);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var exceptionEventDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.ExceptionEvent.ToString()).First();
                exceptionEventDbConfigFeedVersion.LastProcessedFeedVersion = (long)FeedManager.ExceptionEventFeedContainer.LastFeedVersion;
                exceptionEventDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                exceptionEventDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbExceptionEvents into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long noOfInserts = await DbExceptionEventService.InsertAsync(connectionInfo, dbExpectionEvents, exceptionEventDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)FeedManager.ExceptionEventFeedContainer.FeedResultData.Count / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {FeedManager.ExceptionEventFeedContainer.FeedResultData.Count} records into " +
                        $"{ConfigurationManager.DbExceptionEventTableName} table in {elapsedTime.TotalSeconds} seconds " +
                        $"({recordsProcessedPerSecond} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                FeedManager.ExceptionEventFeedContainer.FeedResultData.Clear();
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
            if (FeedManager.FaultDataFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultFaultDatas = FeedManager.FaultDataFeedContainer.GetFeedResultDataValuesList<FaultData>();
                var filteredFaultDatas = ApplyTrackedDevicesFilterToList<FaultData>(feedResultFaultDatas);
                filteredFaultDatas = ApplyTrackedDiagnosticsFilterToList<FaultData>(filteredFaultDatas);

                // Hydrate child objects of filtered FaultData entities.
                foreach (var filteredFaultData in filteredFaultDatas)
                {
                    Controller hydratedController = CacheManager.HydrateController(filteredFaultData.Controller);
                    filteredFaultData.Controller = hydratedController;
                    FailureMode hydratedFailureMode = CacheManager.HydrateFailureMode(filteredFaultData.FailureMode);
                    filteredFaultData.FailureMode = hydratedFailureMode;
                }

                // Map FaultData entities to DbFaultData entities.
                var dbFaultDatas = ObjectMapper.GetDbFaultDatas(filteredFaultDatas);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var faultDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.FaultData.ToString()).First();
                faultDataDbConfigFeedVersion.LastProcessedFeedVersion = (long)FeedManager.FaultDataFeedContainer.LastFeedVersion;
                faultDataDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                faultDataDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbFaultData entities into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long faultDataEntitiesInserted = await DbFaultDataService.InsertAsync(connectionInfo, dbFaultDatas, faultDataDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)faultDataEntitiesInserted / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {faultDataEntitiesInserted} records into {ConfigurationManager.DbFaultDataTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                FeedManager.FaultDataFeedContainer.FeedResultData.Clear();
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
            if (FeedManager.LogRecordFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultLogRecords = FeedManager.LogRecordFeedContainer.GetFeedResultDataValuesList<LogRecord>();
                var filteredLogRecords = ApplyTrackedDevicesFilterToList<LogRecord>(feedResultLogRecords);

                // Map LogRecords to DbLogRecords.
                var dbLogRecords = ObjectMapper.GetDbLogRecords(filteredLogRecords);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var logRecordDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.LogRecord.ToString()).First();
                logRecordDbConfigFeedVersion.LastProcessedFeedVersion = (long)FeedManager.LogRecordFeedContainer.LastFeedVersion;
                logRecordDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                logRecordDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbLogRecords into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long logRecordsInserted = await DbLogRecordService.InsertAsync(connectionInfo, dbLogRecords, logRecordDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)FeedManager.LogRecordFeedContainer.FeedResultData.Count / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {FeedManager.LogRecordFeedContainer.FeedResultData.Count} records into {ConfigurationManager.DbLogRecordTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                FeedManager.LogRecordFeedContainer.FeedResultData.Clear();
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
            if (FeedManager.StatusDataFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultStatusDatas = FeedManager.StatusDataFeedContainer.GetFeedResultDataValuesList<StatusData>();
                var filteredStatusDatas = ApplyTrackedDevicesFilterToList<StatusData>(feedResultStatusDatas);
                filteredStatusDatas = ApplyTrackedDiagnosticsFilterToList<StatusData>(filteredStatusDatas);

                // Map StatusData entities to DbStatusData entities.
                var dbStatusDatas = ObjectMapper.GetDbStatusDatas(filteredStatusDatas);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var statusDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.StatusData.ToString()).First();
                statusDataDbConfigFeedVersion.LastProcessedFeedVersion = (long)FeedManager.StatusDataFeedContainer.LastFeedVersion;
                statusDataDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                statusDataDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbStatusData entities into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long statusDataEntitiesInserted = await DbStatusDataService.InsertAsync(connectionInfo, dbStatusDatas, statusDataDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)statusDataEntitiesInserted / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {statusDataEntitiesInserted} records into {ConfigurationManager.DbStatusDataTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                FeedManager.StatusDataFeedContainer.FeedResultData.Clear();
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
            if (FeedManager.TripFeedContainer.FeedResultData.Count > 0)
            {
                var feedResultTrips = FeedManager.TripFeedContainer.GetFeedResultDataValuesList<Trip>();
                var filteredTrips = ApplyTrackedDevicesFilterToList<Trip>(feedResultTrips);

                // Map Trip entities to DbTrip entities.
                List<DbTrip> dbTripList = ObjectMapper.GetDbTrips(filteredTrips);

                cancellationToken.ThrowIfCancellationRequested();

                // Get the feed version information.
                var tripDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.Trip.ToString()).First();
                tripDbConfigFeedVersion.LastProcessedFeedVersion = (long)FeedManager.TripFeedContainer.LastFeedVersion;
                tripDbConfigFeedVersion.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                tripDbConfigFeedVersion.RecordLastChangedUtc = DateTime.UtcNow;

                // Insert DbTrip entities into database.
                try
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    long tripsNoInserted = await DbTripService.InsertAsync(connectionInfo, dbTripList, tripDbConfigFeedVersion, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)tripsNoInserted / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed insertion of {tripsNoInserted} records into {ConfigurationManager.DbTripTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                }
                catch (Exception)
                {
                    cancellationTokenSource.Cancel();
                    throw;
                }

                // Clear FeedResultData.
                FeedManager.TripFeedContainer.FeedResultData.Clear();
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
                            throw new DatabaseConnectionException($"The MyGeotab database version information was not propagated to the database within the allowed time of {Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks} seconds. This may be due to a database connectivity issue.");
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
            if (CacheManager.DeviceCacheContainer.LastUpdatedTimeUtc > CacheManager.DeviceCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbDevicesToInsert = new List<DbDevice>();
                var dbDevicesToUpdate = new List<DbDevice>();

                // Get cached devices.
                var deviceCache = (Dictionary<Id, Device>)CacheManager.DeviceCacheContainer.Cache;

                // Find any devices that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbDevicesDictionary.Any())
                {
                    foreach (DbDevice dbDevice in dbDevicesDictionary.Values.ToList())
                    {
                        if (dbDevice.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool deviceExistsInCache = deviceCache.ContainsKey(Id.Create(dbDevice.GeotabId));
                            if (!deviceExistsInCache)
                            {
                                logger.Debug($"Device '{dbDevice.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbDevice.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbDevice.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbDevice.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbDevicesDictionary[Id.Create(dbDevice.GeotabId)] = dbDevice;
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
                            updatedDbDevice.id = existingDbDevice.id;
                            updatedDbDevice.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                            updatedDbDevice.RecordLastChangedUtc = recordChangedTimestampUtc;
                            updatedDbDevice.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

                            dbDevicesDictionary[Id.Create(updatedDbDevice.GeotabId)] = updatedDbDevice;
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
                        dbDevicesDictionary.Add(Id.Create(newDbDevice.GeotabId), newDbDevice);
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
                        logger.Info($"Completed insertion of {deviceEntitiesInserted} records into {ConfigurationManager.DbDeviceTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
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
                        logger.Info($"Completed updating of {deviceEntitiesUpdated} records in {ConfigurationManager.DbDeviceTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                CacheManager.DeviceCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
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
            if (CacheManager.DiagnosticCacheContainer.LastUpdatedTimeUtc > CacheManager.DiagnosticCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbDiagnosticsToInsert = new List<DbDiagnostic>();
                var dbDiagnosticsToUpdate = new List<DbDiagnostic>();

                // Get cached diagnostics.
                var diagnosticCache = (Dictionary<Id, Diagnostic>)CacheManager.DiagnosticCacheContainer.Cache;

                // Find any diagnostics that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbDiagnosticsDictionary.Any())
                {
                    foreach (DbDiagnostic dbDiagnostic in dbDiagnosticsDictionary.Values.ToList())
                    {
                        if (dbDiagnostic.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool diagnosticExistsInCache = diagnosticCache.ContainsKey(Id.Create(dbDiagnostic.GeotabId));
                            if (!diagnosticExistsInCache)
                            {
                                logger.Debug($"Diagnostic '{dbDiagnostic.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbDiagnostic.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbDiagnostic.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbDiagnostic.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbDiagnosticsDictionary[Id.Create(dbDiagnostic.GeotabId)] = dbDiagnostic;
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
                            updatedDbDiagnostic.id = existingDbDiagnostic.id;
                            updatedDbDiagnostic.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                            updatedDbDiagnostic.RecordLastChangedUtc = recordChangedTimestampUtc;
                            updatedDbDiagnostic.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

                            dbDiagnosticsDictionary[Id.Create(updatedDbDiagnostic.GeotabId)] = updatedDbDiagnostic;
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
                        dbDiagnosticsDictionary.Add(Id.Create(newDbDiagnostic.GeotabId), newDbDiagnostic);
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
                        logger.Info($"Completed insertion of {diagnosticEntitiesInserted} records into {ConfigurationManager.DbDiagnosticTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
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
                        logger.Info($"Completed updating of {diagnosticEntitiesUpdated} records in {ConfigurationManager.DbDiagnosticTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                CacheManager.DiagnosticCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
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
            if (CacheManager.RuleCacheContainer.LastUpdatedTimeUtc > CacheManager.RuleCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbRuleObjectsToInsert = new List<DbRuleObject>();
                var dbRuleObjectsToUpdate = new List<DbRuleObject>();
                var dbRuleObjectsToDelete = new List<DbRuleObject>();

                // Get cached rules (Geotab object).
                var ruleCache = (Dictionary<Id, Geotab.Checkmate.ObjectModel.Exceptions.Rule>)CacheManager.RuleCacheContainer.Cache;

                // Find any rules that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbRuleObjectDictionary.Any())
                {
                    foreach (DbRuleObject dbRuleObject in dbRuleObjectDictionary.Values.ToList())
                    {
                        if (dbRuleObject.DbRule.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool ruleExistsInCache = ruleCache.ContainsKey(Id.Create(dbRuleObject.DbRule.GeotabId));
                            if (!ruleExistsInCache)
                            {
                                logger.Debug($"Rule '{dbRuleObject.DbRule.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbRuleObject.DbRule.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbRuleObject.DbRule.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbRuleObject.DbRule.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbRuleObjectDictionary[Id.Create(dbRuleObject.GeotabId)] = dbRuleObject;
                                dbRuleObjectsToDelete.Add(dbRuleObject);
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
                    logger.Debug($"Processing cached Rule objects in preparation for database update: {cachedRulesProcessed} of {cachedRuleCount} ({cachedRulesProcessedPercentage:P})");

                    // Try to find the existing database record for the cached rule.
                    if (dbRuleObjectDictionary.TryGetValue(cachedRule.Id, out var existingDbRuleObject))
                    {
                        // The rule has already been added to the database - therefore update it.
                        bool dbRuleRequiresUpdate = ObjectMapper.DbRuleObjectRequiresUpdate(existingDbRuleObject, cachedRule);
                        if (dbRuleRequiresUpdate)
                        {
                            DbRuleObject updatedDbRuleObject = new();
                            updatedDbRuleObject.BuildRuleObject(cachedRule, (int)Common.DatabaseRecordStatus.Active, recordChangedTimestampUtc,
                                Common.DatabaseWriteOperationType.Update);
                            updatedDbRuleObject.DbRule.id = existingDbRuleObject.DbRule.id;

                            // Get existing conditions associated with the rule from the adapter database and flag these for deletion/replacement (since conditon Ids coming through the API may differ from those in the adapter database if a Rule's conditions have changed).
                            updatedDbRuleObject.DbConditionsToBeDeleted = existingDbRuleObject.DbConditions;

                            dbRuleObjectDictionary[Id.Create(updatedDbRuleObject.GeotabId)] = updatedDbRuleObject;
                            dbRuleObjectsToUpdate.Add(updatedDbRuleObject);
                        }
                    }
                    else
                    {
                        // The rule has not yet been added to the database. Create a DbRule, set its properties and add it to the cache.
                        DbRuleObject newDbRuleObject = new();
                        newDbRuleObject.BuildRuleObject(cachedRule, (int)Common.DatabaseRecordStatus.Active, recordChangedTimestampUtc,
                            Common.DatabaseWriteOperationType.Insert);
                        dbRuleObjectDictionary[Id.Create(newDbRuleObject.GeotabId)] = newDbRuleObject;
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
                    logger.Info($"Completed insertion of {ruleObjectEntitiesInserted} records into the {ConfigurationManager.DbRuleTableName} table, along with its related conditions in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                }

                // Send any updates to the database. When a rule is updated, delete all of the associated conditions and then re-create them rather than evaluating individual conditions for differences/additions/deletions (as this would be very time-consuming).
                if (dbRuleObjectsToUpdate.Any())
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    int ruleObjectEntitiesUpdated = RuleHelper.UpdateDbRuleObjectsToDatabase(dbRuleObjectsToUpdate, cancellationTokenSource);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)ruleObjectEntitiesUpdated / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed updating of {ruleObjectEntitiesUpdated} records in the {ConfigurationManager.DbRuleTableName} table, along with its related conditions in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                }

                // Send any deletes to the database. Rules that have been deleted in MyGeotab will have their EntityStatus changed, but the associated conditions will remain in the adapter database.
                if (dbRuleObjectsToDelete.Any())
                {
                    DateTime startTimeUTC = DateTime.UtcNow;
                    int ruleObjectEntitiesDeleted = RuleHelper.UpdateDbRuleObjectsToDatabase(dbRuleObjectsToDelete, cancellationTokenSource);
                    TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                    double recordsProcessedPerSecond = (double)ruleObjectEntitiesDeleted / (double)elapsedTime.TotalSeconds;
                    logger.Info($"Completed flagging as deleted {ruleObjectEntitiesDeleted} records in the {ConfigurationManager.DbRuleTableName} table, along with its related conditions in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                }

                CacheManager.RuleCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
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
            if (CacheManager.UserCacheContainer.LastUpdatedTimeUtc > CacheManager.UserCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbUsersToInsert = new List<DbUser>();
                var dbUsersToUpdate = new List<DbUser>();

                // Get cached users.
                var userCache = (Dictionary<Id, User>)CacheManager.UserCacheContainer.Cache;

                // Find any users that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbUsersDictionary.Any())
                {
                    foreach (DbUser dbUser in dbUsersDictionary.Values.ToList())
                    {
                        if (dbUser.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool userExistsInCache = userCache.ContainsKey(Id.Create(dbUser.GeotabId));
                            if (!userExistsInCache)
                            {
                                logger.Debug($"User '{dbUser.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbUser.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbUser.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbUser.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbUsersDictionary[Id.Create(dbUser.GeotabId)] = dbUser;
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
                            updatedDbUser.id = existingDbUser.id;
                            updatedDbUser.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                            updatedDbUser.RecordLastChangedUtc = recordChangedTimestampUtc;
                            updatedDbUser.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                            dbUsersDictionary[Id.Create(updatedDbUser.GeotabId)] = updatedDbUser;
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
                        dbUsersDictionary.Add(Id.Create(newDbUser.GeotabId), newDbUser);
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
                        logger.Info($"Completed insertion of {userEntitiesInserted} records into {ConfigurationManager.DbUserTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
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
                        logger.Info($"Completed updating of {userEntitiesUpdated} records in {ConfigurationManager.DbUserTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                CacheManager.UserCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
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
            if (CacheManager.ZoneCacheContainer.LastUpdatedTimeUtc > CacheManager.ZoneCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbZonesToInsert = new List<DbZone>();
                var dbZonesToUpdate = new List<DbZone>();

                // Get cached zones.
                var zoneCache = (Dictionary<Id, Zone>)CacheManager.ZoneCacheContainer.Cache;

                // Find any zones that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbZonesDictionary.Any())
                {
                    foreach (DbZone dbZone in dbZonesDictionary.Values.ToList())
                    {
                        if (dbZone.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool zoneExistsInCache = zoneCache.ContainsKey(Id.Create(dbZone.GeotabId));
                            if (!zoneExistsInCache)
                            {
                                logger.Debug($"Zone '{dbZone.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbZone.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbZone.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbZone.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbZonesDictionary[Id.Create(dbZone.GeotabId)] = dbZone;
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
                            updatedDbZone.id = existingDbZone.id;
                            updatedDbZone.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                            updatedDbZone.RecordLastChangedUtc = recordChangedTimestampUtc;
                            updatedDbZone.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                            dbZonesDictionary[Id.Create(updatedDbZone.GeotabId)] = updatedDbZone;
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
                        dbZonesDictionary.Add(Id.Create(newDbZone.GeotabId), newDbZone);
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
                        logger.Info($"Completed insertion of {zoneEntitiesInserted} records into {ConfigurationManager.DbZoneTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
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
                        logger.Info($"Completed updating of {zoneEntitiesUpdated} records in {ConfigurationManager.DbZoneTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                CacheManager.ZoneCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
            }
            else
            {
                logger.Debug($"Zone cache in database is up-to-date.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Propagates <see cref="ZoneType"/> information received from MyGeotab to the database - inserting new records, updating existing records (if the values of any of the utilized fields have changed), and marking as deleted any database zoneType records that no longer exist in MyGeotab (based on matching on ID). 
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task PropagateZoneTypeCacheUpdatesToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Only propagate the cache to database if the cache has been updated since the last time it was propagated to database.
            if (CacheManager.ZoneTypeCacheContainer.LastUpdatedTimeUtc > CacheManager.ZoneTypeCacheContainer.LastPropagatedToDatabaseTimeUtc)
            {
                DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                var dbZoneTypesToInsert = new List<DbZoneType>();
                var dbZoneTypesToUpdate = new List<DbZoneType>();

                // Get cached zoneTypes.
                var zoneTypeCache = (Dictionary<Id, ZoneType>)CacheManager.ZoneTypeCacheContainer.Cache;

                // Find any zoneTypes that have been deleted in MyGeotab but exist in the database and have not yet been flagged as deleted. Update them so that they will be flagged as deleted in the database.
                if (dbZoneTypesDictionary.Any())
                {
                    foreach (DbZoneType dbZoneType in dbZoneTypesDictionary.Values.ToList())
                    {
                        if (dbZoneType.EntityStatus == (int)Common.DatabaseRecordStatus.Active)
                        {
                            bool zoneTypeExistsInCache = zoneTypeCache.ContainsKey(Id.Create(dbZoneType.GeotabId));
                            if (!zoneTypeExistsInCache)
                            {
                                logger.Debug($"ZoneType '{dbZoneType.GeotabId}' no longer exists in MyGeotab and is being marked as deleted.");
                                dbZoneType.EntityStatus = (int)Common.DatabaseRecordStatus.Deleted;
                                dbZoneType.RecordLastChangedUtc = recordChangedTimestampUtc;
                                dbZoneType.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                dbZoneTypesDictionary[Id.Create(dbZoneType.GeotabId)] = dbZoneType;
                                dbZoneTypesToUpdate.Add(dbZoneType);
                            }
                        }
                    }
                }

                // Iterate through cached zoneTypes.
                foreach (ZoneType cachedZoneType in zoneTypeCache.Values.ToList())
                {
                    // Try to find the existing database record for the cached zoneType.
                    if (dbZoneTypesDictionary.TryGetValue(cachedZoneType.Id, out var existingDbZoneType))
                    {
                        // The zoneType has already been added to the database.
                        bool dbZoneTypeRequiresUpdate = ObjectMapper.DbZoneTypeRequiresUpdate(existingDbZoneType, cachedZoneType);
                        if (dbZoneTypeRequiresUpdate)
                        {
                            DbZoneType updatedDbZoneType = ObjectMapper.GetDbZoneType(cachedZoneType);
                            updatedDbZoneType.id = existingDbZoneType.id;
                            updatedDbZoneType.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                            updatedDbZoneType.RecordLastChangedUtc = recordChangedTimestampUtc;
                            updatedDbZoneType.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                            dbZoneTypesDictionary[Id.Create(updatedDbZoneType.GeotabId)] = updatedDbZoneType;
                            dbZoneTypesToUpdate.Add(updatedDbZoneType);
                        }
                    }
                    else
                    {
                        // The zoneType has not yet been added to the database. Create a DbZoneType, set its properties and add it to the cache.
                        DbZoneType newDbZoneType = ObjectMapper.GetDbZoneType(cachedZoneType);
                        newDbZoneType.EntityStatus = (int)Common.DatabaseRecordStatus.Active;
                        newDbZoneType.RecordLastChangedUtc = recordChangedTimestampUtc;
                        newDbZoneType.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;
                        dbZoneTypesDictionary.Add(Id.Create(newDbZoneType.GeotabId), newDbZoneType);
                        dbZoneTypesToInsert.Add(newDbZoneType);

                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Send any inserts to the database.
                if (dbZoneTypesToInsert.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long zoneTypeEntitiesInserted = await DbZoneTypeService.InsertAsync(connectionInfo, dbZoneTypesToInsert, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)zoneTypeEntitiesInserted / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed insertion of {zoneTypeEntitiesInserted} records into {ConfigurationManager.DbZoneTypeTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }

                // Send any updates/deletes to the database.
                if (dbZoneTypesToUpdate.Any())
                {
                    try
                    {
                        DateTime startTimeUTC = DateTime.UtcNow;
                        long zoneTypeEntitiesUpdated = await DbZoneTypeService.UpdateAsync(connectionInfo, dbZoneTypesToUpdate, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                        TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                        double recordsProcessedPerSecond = (double)zoneTypeEntitiesUpdated / (double)elapsedTime.TotalSeconds;
                        logger.Info($"Completed updating of {zoneTypeEntitiesUpdated} records in {ConfigurationManager.DbZoneTypeTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                CacheManager.ZoneTypeCacheContainer.LastPropagatedToDatabaseTimeUtc = DateTime.UtcNow;
            }
            else
            {
                logger.Debug($"ZoneType cache in database is up-to-date.");
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
                FeedManager.RollbackFeedContainerLastFeedVersions(dbConfigFeedVersions);
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
            DefectSearch defectSearch = new()
            {
                IncludeAllTrees = true
            };

            List<Defect> defectLists;
            try
            {
                CancellationTokenSource timeoutcancellationTokenSource = new();
                timeoutcancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(Globals.ConfigurationManager.TimeoutSecondsForMyGeotabTasks));

                Task<List<Defect>> defectListsTask = Task.Run(() => Globals.MyGeotabAPI.CallAsync<List<Defect>>("Get", typeof(Defect), new { search = defectSearch }));

                defectLists = await defectListsTask;
            }
            catch (OperationCanceledException exception)
            {
                cancellationTokenSource.Cancel();
                throw new MyGeotabConnectionException($"MyGeotab API GetAsync call for type '{typeof(Defect).Name}' did not return within the allowed time of {Globals.ConfigurationManager.TimeoutSecondsForMyGeotabTasks} seconds. This may be due to a loss of connectivity with the MyGeotab server.", exception);
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
                            logger.Debug($"partDefect '{partDefect.Id} ({partDefect.Name})' is not a Defect.");
                        }
                    }
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates all caches and persists cache updates to the database.
        /// </summary>
        /// <returns></returns>
        async Task UpdateAllCachesAndPersistToDatabaseAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var updateUserCacheAndPersistToDatabaseAsyncTask = UpdateUserCacheAndPersistToDatabaseAsync(cancellationTokenSource);
                    var updateDeviceCacheAndPersistToDatabaseAsyncTask = UpdateDeviceCacheAndPersistToDatabaseAsync(cancellationTokenSource);
                    var updateZoneTypeCacheAndPersistToDatabaseAsyncTask = UpdateZoneTypeCacheAndPersistToDatabaseAsync(cancellationTokenSource);
                    var updateZoneCacheAndPersistToDatabaseAsyncTask = UpdateZoneCacheAndPersistToDatabaseAsync(cancellationTokenSource);
                    var updateDiagnosticCacheAndPersistToDatabaseAsyncTask = UpdateDiagnosticCacheAndPersistToDatabaseAsync(cancellationTokenSource);
                    var updateControllerCacheAsyncTask = UpdateControllerCacheAsync(cancellationTokenSource);
                    var updateFailureModeCacheAsyncTask = UpdateFailureModeCacheAsync(cancellationTokenSource);
                    var updateUnitOfMeasureCacheAsyncTask = UpdateUnitOfMeasureCacheAsync(cancellationTokenSource);
                    var updateRuleCacheAndPersistToDatabaseAsyncTask = UpdateRuleCacheAndPersistToDatabaseAsync(cancellationTokenSource);
                    var updateGroupCacheAsyncTask = UpdateGroupCacheAsync(cancellationTokenSource);

                    Task[] tasks = { updateUserCacheAndPersistToDatabaseAsyncTask, updateDeviceCacheAndPersistToDatabaseAsyncTask, updateZoneTypeCacheAndPersistToDatabaseAsyncTask, updateZoneCacheAndPersistToDatabaseAsyncTask, updateDiagnosticCacheAndPersistToDatabaseAsyncTask, updateControllerCacheAsyncTask, updateFailureModeCacheAsyncTask, updateUnitOfMeasureCacheAsyncTask, updateRuleCacheAndPersistToDatabaseAsyncTask, updateGroupCacheAsyncTask };

                    try
                    {
                        await Task.WhenAll(tasks);
                    }
                    catch (MyGeotabConnectionException myGeotabConnectionException)
                    {
                        throw new MyGeotabConnectionException($"One or more exceptions were encountered during cache processing due to an apparent loss of connectivity with the MyGeotab server.", myGeotabConnectionException);
                    }
                    catch (DatabaseConnectionException databaseConnectionException)
                    {
                        throw new DatabaseConnectionException($"One or more exceptions were encountered during cache processing due to an apparent loss of connectivity with the adapter database.", databaseConnectionException);
                    }
                    catch (AggregateException aggregateException)
                    {
                        Globals.HandleConnectivityRelatedAggregateException(aggregateException, Globals.ConnectivityIssueType.MyGeotabOrDatabase, "One or more exceptions were encountered during retrieval of cache data and persistance of data to database due to an apparent loss of connectivity with the MyGeotab server or the database.");
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
        /// Updates the <see cref="Controller"/> cache.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateControllerCacheAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await CacheManager.UpdateCacheAsync<Controller>(cancellationTokenSource, CacheManager.ControllerCacheContainer, Globals.ConfigurationManager.ControllerCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.ControllerCacheUpdateIntervalMinutes, Globals.ConfigurationManager.ControllerCacheRefreshIntervalMinutes, false);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates the <see cref="Device"/> cache and persists cache updates to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateDeviceCacheAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await CacheManager.UpdateCacheAsync<Device>(cancellationTokenSource, CacheManager.DeviceCacheContainer, Globals.ConfigurationManager.DeviceCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.DeviceCacheUpdateIntervalMinutes, Globals.ConfigurationManager.DeviceCacheRefreshIntervalMinutes);
            cancellationToken.ThrowIfCancellationRequested();
            await PropagateDeviceCacheUpdatesToDatabaseAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates the <see cref="Diagnostic"/> cache and persists cache updates to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateDiagnosticCacheAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await CacheManager.UpdateCacheAsync<Diagnostic>(cancellationTokenSource, CacheManager.DiagnosticCacheContainer, Globals.ConfigurationManager.DiagnosticCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.DiagnosticCacheUpdateIntervalMinutes, Globals.ConfigurationManager.DiagnosticCacheRefreshIntervalMinutes);
            cancellationToken.ThrowIfCancellationRequested();
            await PropagateDiagnosticCacheUpdatesToDatabaseAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates the <see cref="FailureMode"/> cache.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateFailureModeCacheAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await CacheManager.UpdateCacheAsync<FailureMode>(cancellationTokenSource, CacheManager.FailureModeCacheContainer, Globals.ConfigurationManager.FailureModeCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.FailureModeCacheUpdateIntervalMinutes, Globals.ConfigurationManager.FailureModeCacheRefreshIntervalMinutes, false);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates the <see cref="Group"/> cache.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateGroupCacheAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await CacheManager.UpdateCacheAsync<Group>(cancellationTokenSource, CacheManager.GroupCacheContainer, Globals.ConfigurationManager.GroupCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.GroupCacheUpdateIntervalMinutes, Globals.ConfigurationManager.GroupCacheRefreshIntervalMinutes, false);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates the <see cref="Geotab.Checkmate.ObjectModel.Exceptions.Rule"/> cache and persists cache updates to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateRuleCacheAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await CacheManager.UpdateCacheAsync<Geotab.Checkmate.ObjectModel.Exceptions.Rule>(cancellationTokenSource, CacheManager.RuleCacheContainer, Globals.ConfigurationManager.RuleCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.RuleCacheUpdateIntervalMinutes, Globals.ConfigurationManager.RuleCacheRefreshIntervalMinutes);
            cancellationToken.ThrowIfCancellationRequested();
            await PropagateRuleCacheUpdatesToDatabaseAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates the <see cref="UnitOfMeasure"/> cache.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateUnitOfMeasureCacheAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await CacheManager.UpdateCacheAsync<UnitOfMeasure>(cancellationTokenSource, CacheManager.UnitOfMeasureCacheContainer, Globals.ConfigurationManager.UnitOfMeasureCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.UnitOfMeasureCacheUpdateIntervalMinutes, Globals.ConfigurationManager.UnitOfMeasureCacheRefreshIntervalMinutes, false);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates the <see cref="User"/> cache and persists cache updates to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateUserCacheAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await CacheManager.UpdateCacheAsync<User>(cancellationTokenSource, CacheManager.UserCacheContainer, Globals.ConfigurationManager.UserCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.UserCacheUpdateIntervalMinutes, Globals.ConfigurationManager.UserCacheRefreshIntervalMinutes);
            cancellationToken.ThrowIfCancellationRequested();
            await PropagateUserCacheUpdatesToDatabaseAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates the <see cref="Zone"/> cache and persists cache updates to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateZoneCacheAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await CacheManager.UpdateCacheAsync<Zone>(cancellationTokenSource, CacheManager.ZoneCacheContainer, Globals.ConfigurationManager.ZoneCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.ZoneCacheUpdateIntervalMinutes, Globals.ConfigurationManager.ZoneCacheRefreshIntervalMinutes);
            cancellationToken.ThrowIfCancellationRequested();
            await PropagateZoneCacheUpdatesToDatabaseAsync(cancellationTokenSource);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Updates the <see cref="ZoneType"/> cache and persists cache updates to the database.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateZoneTypeCacheAndPersistToDatabaseAsync(CancellationTokenSource cancellationTokenSource)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await CacheManager.UpdateCacheAsync<ZoneType>(cancellationTokenSource, CacheManager.ZoneTypeCacheContainer, Globals.ConfigurationManager.ZoneTypeCacheIntervalDailyReferenceStartTimeUTC, Globals.ConfigurationManager.ZoneTypeCacheUpdateIntervalMinutes, Globals.ConfigurationManager.ZoneTypeCacheRefreshIntervalMinutes, false);
            cancellationToken.ThrowIfCancellationRequested();
            await PropagateZoneTypeCacheUpdatesToDatabaseAsync(cancellationTokenSource);

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
                CancellationTokenSource cancellationTokenSource = new();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(Globals.ConfigurationManager.TimeoutSecondsForMyGeotabTasks));

                Task<VersionInformation> versionInformationTask = Task.Run(() => Globals.MyGeotabAPI.CallAsync<VersionInformation>("GetVersionInformation"));

                versionInformation = await versionInformationTask;
            }
            catch (OperationCanceledException exception)
            {
                throw new MyGeotabConnectionException($"MyGeotab API GetVersionInformationAsync call did not return within the allowed time of {Globals.ConfigurationManager.TimeoutSecondsForMyGeotabTasks} seconds. This may be due to a loss of connectivity with the MyGeotab server.", exception);
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
            logger.Warn($"******** CONNECTIVITY LOST. REASON: '{StateMachine.Reason}'. WAITING FOR RESTORATION OF CONNECTIVITY...");
            while (StateMachine.CurrentState == State.Waiting)
            {
                // Wait for the prescribed interval between connectivity checks.
                await Task.Delay(ConnectivityRestorationCheckIntervalMilliseconds);

                logger.Warn($"{StateMachine.Reason}; continuing to wait for restoration of connectivity...");
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
