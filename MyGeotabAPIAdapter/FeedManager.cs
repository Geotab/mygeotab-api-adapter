using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Utilities;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Manages the retrieval of MyGeotab data via multiple data feeds for various <see cref="Entity"/> types.
    /// </summary>
    class FeedManager
    {
        // FeedCurrentThresholdRecordCount is used to set FeedContainer.FeedCurrent; if the FeedResult returned in a GetFeed call contains less than this number of entities, the feed will be considered up-to-date.
        const int FeedCurrentThresholdRecordCount = 1000;
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        static readonly FeedContainer dvirLogFeedContainer = new FeedContainer();
        static readonly FeedContainer exceptionEventFeedContainer = new FeedContainer();
        static readonly FeedContainer faultDataFeedContainer = new FeedContainer();
        static readonly FeedContainer logRecordFeedContainer = new FeedContainer();
        static readonly FeedContainer statusDataFeedContainer = new FeedContainer();
        static readonly FeedContainer tripFeedContainer = new FeedContainer();

        /// <summary>
        /// Constructor is private. Use CreateAsync() method to instantiate. This is to facilitate use of MyGeotab async methods, since the 'await' operator can only be used within an async method.
        /// </summary>
        private FeedManager()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FeedManager"/> class.
        /// </summary>
        /// <param name="dbConfigFeedVersions">The list of <see cref="DbConfigFeedVersion"/> objects conatining the latest ToVersion for each of the supported data feeds.</param>
        /// <returns></returns>
        public static FeedManager Create(List<DbConfigFeedVersion> dbConfigFeedVersions)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var feedManager = new FeedManager();

            // If the FeedStartOption is not set to FeedVersion and the ConfigFeedVersions table has data for any of the feeds, switch the FeedStartOption to FeedVersion to avoid sending duplicate records to the database.
            var currentFeedStartOption = Globals.ConfigurationManager.FeedStartOption;
            if (currentFeedStartOption != Globals.FeedStartOption.FeedVersion && dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.LastProcessedFeedVersion > 0).Any())
            {
                logger.Info($"Switching FeedStartOption from '{currentFeedStartOption.ToString()}' to '{Globals.FeedStartOption.FeedVersion.ToString()}' to prevent data duplication because the '{Globals.ConfigurationManager.DbConfigFeedVersionsTableName}' table contains data for one or more feeds.");
                Globals.ConfigurationManager.FeedStartOption = Globals.FeedStartOption.FeedVersion;
            }

            // Setup a data feed for LogRecords.
            DbConfigFeedVersion logRecordDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.LogRecord.ToString()).First();
            SetupFeedContainer<LogRecord>(logRecordFeedContainer, false, Globals.ConfigurationManager.EnableLogRecordFeed,
                Globals.ConfigurationManager.LogRecordFeedIntervalSeconds, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, logRecordDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for StatusData.
            DbConfigFeedVersion statusDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.StatusData.ToString()).First();
            SetupFeedContainer<StatusData>(statusDataFeedContainer, false, Globals.ConfigurationManager.EnableStatusDataFeed,
                Globals.ConfigurationManager.StatusDataFeedIntervalSeconds, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, statusDataDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for FaultData.
            DbConfigFeedVersion faultDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.FaultData.ToString()).First();
            SetupFeedContainer<FaultData>(faultDataFeedContainer, false, Globals.ConfigurationManager.EnableFaultDataFeed,
                Globals.ConfigurationManager.FaultDataFeedIntervalSeconds, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, faultDataDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for DVIRLogs.
            DbConfigFeedVersion dvirLogDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.DVIRLog.ToString()).First();
            SetupFeedContainer<DVIRLog>(dvirLogFeedContainer, false, Globals.ConfigurationManager.EnableDVIRLogDataFeed,
                Globals.ConfigurationManager.DVIRLogDataFeedIntervalSeconds, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, dvirLogDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for Trips.
            DbConfigFeedVersion tripDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.Trip.ToString()).First();
            SetupFeedContainer<Trip>(tripFeedContainer, false, Globals.ConfigurationManager.EnableTripFeed,
                Globals.ConfigurationManager.TripFeedIntervalSeconds, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, tripDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for ExceptionEvents.
            DbConfigFeedVersion exceptionEventDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.ExceptionEvent.ToString()).First();
            SetupFeedContainer<ExceptionEvent>(exceptionEventFeedContainer, false, Globals.ConfigurationManager.EnableExceptionEventFeed,
                Globals.ConfigurationManager.ExceptionEventFeedIntervalSeconds, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, exceptionEventDbConfigFeedVersion.LastProcessedFeedVersion);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return feedManager;
        }

        /// <summary>
        /// Holds <see cref="DVIRLog"/> information obtained via data feed.
        /// </summary>
        public FeedContainer DVIRLogFeedContainer
        {
            get => dvirLogFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="ExceptionEvent"/> information obtained via data feed.
        /// </summary>
        public FeedContainer ExceptionEventFeedContainer
        {
            get => exceptionEventFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="FaultData"/> information obtained via data feed.
        /// </summary>
        public FeedContainer FaultDataFeedContainer
        {
            get => faultDataFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="LogRecord"/> information obtained via data feed.
        /// </summary>
        public FeedContainer LogRecordFeedContainer
        {
            get => logRecordFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="StatusData"/> information obtained via data feed.
        /// </summary>
        public FeedContainer StatusDataFeedContainer
        {
            get => statusDataFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="Trip"/> information obtained via data feed.
        /// </summary>
        public FeedContainer TripFeedContainer
        {
            get => tripFeedContainer;
        }

        /// <summary>
        /// Triggers concurrent calls to update the <see cref="FeedContainer"/> objects managed by this <see cref="FeedManager"/>.
        /// </summary>
        /// <returns></returns>
        public async Task GetDataFromFeedsAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var pollLogRecordFeedTask = GetFeedDataAsync<LogRecord>(logRecordFeedContainer, cancellationTokenSource);
                    var pollStatusDataFeedTask = GetFeedDataAsync<StatusData>(statusDataFeedContainer, cancellationTokenSource);
                    var pollFaultDataFeedTask = GetFeedDataAsync<FaultData>(faultDataFeedContainer, cancellationTokenSource);
                    var pollDVIRLogFeedTask = GetFeedDataAsync<DVIRLog>(dvirLogFeedContainer, cancellationTokenSource);
                    var pollTripFeedTask = GetFeedDataAsync<Trip>(tripFeedContainer, cancellationTokenSource);
                    var pollExceptionEventTask = GetFeedDataAsync<ExceptionEvent>(exceptionEventFeedContainer, cancellationTokenSource);

                    Task[] tasks = { pollLogRecordFeedTask, pollStatusDataFeedTask, pollFaultDataFeedTask, pollDVIRLogFeedTask, pollTripFeedTask, pollExceptionEventTask };

                    try
                    {
                        await Task.WhenAll(tasks);
                    }
                    catch (MyGeotabConnectionException myGeotabConnectionException)
                    {
                        throw new MyGeotabConnectionException($"One or more exceptions were encountered during cache update due to an apparent loss of connectivity with the MyGeotab server.", myGeotabConnectionException);
                    }
                    catch (AggregateException aggregateException)
                    {
                        Globals.HandleConnectivityRelatedAggregateException(aggregateException, Globals.ConnectivityIssueType.MyGeotab, "One or more exceptions were encountered during retrieval of data via feeds due to an apparent loss of connectivity with the MyGeotab server.");
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
        /// Updates the <see cref="FeedContainer.FeedResultData"/> of a <see cref="FeedContainer"/> using GetFeed() calls.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of <see cref="Entity"/> for which data is to be retrieved.</typeparam>
        /// <param name="feedContainer">The <see cref="FeedContainer"/> representing the <see cref="Type"/> of <see cref="Entity"/> for which data is to be retrieved.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task GetFeedDataAsync<T>(FeedContainer feedContainer, CancellationTokenSource cancellationTokenSource) where T : Entity
        {
            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{typeParameterType.Name}'");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Only poll the data feed if it is enabled for the subject entity type.
            if (feedContainer.FeedEnabled == false)
            {
                logger.Debug($"{typeParameterType.Name} data feed is disabled.");
            }
            else
            {
                // Only poll the data feed if the configured polling interval has elapsed or if the feed is not current (e.g. when starting at some point in the past and numerous GetFeed calls are required to pull all of the historic data).
                if (Globals.TimeIntervalHasElapsed(feedContainer.LastFeedRetrievalTimeUtc, Globals.DateTimeIntervalType.Seconds, feedContainer.FeedPollingIntervalSeconds) || !feedContainer.FeedCurrent)
                {
                    logger.Debug($"{typeParameterType.Name} data feed poll required.");

                    // Clear any previous FeedResult data.
                    feedContainer.FeedResultData.Clear();

                    cancellationToken.ThrowIfCancellationRequested();

                    // Execute the GetFeedAsync method according to the specifed FeedStartOption. Note that CurrentTime and SpecificTime are only for use in an initial GetFeedAsync call and all subsequent GetFeedAsync calls should use the FeedVersion option.
                    logger.Debug($"Calling GetFeedAsync<{typeParameterType.Name}>.");
                    FeedResult<T> feedResult = null;
                    try
                    {
                        switch (feedContainer.FeedStartOption)
                        {
                            case Globals.FeedStartOption.CurrentTime:
                                feedContainer.FeedStartTimeUtc = DateTime.UtcNow;
                                feedResult = await MyGeotabApiUtility.GetFeedAsync<T>(Globals.MyGeotabAPI, feedContainer.FeedStartTimeUtc, feedContainer.FeedResultsLimit);

                                // Switch to FeedVersion for subsequent calls.
                                feedContainer.FeedStartOption = Globals.FeedStartOption.FeedVersion;
                                break;
                            case Globals.FeedStartOption.SpecificTime:
                                feedResult = await MyGeotabApiUtility.GetFeedAsync<T>(Globals.MyGeotabAPI, feedContainer.FeedStartTimeUtc, feedContainer.FeedResultsLimit);

                                // Switch to FeedVersion for subsequent calls.
                                feedContainer.FeedStartOption = Globals.FeedStartOption.FeedVersion;
                                break;
                            case Globals.FeedStartOption.FeedVersion:
                                feedResult = await MyGeotabApiUtility.GetFeedAsync<T>(Globals.MyGeotabAPI, feedContainer.LastFeedVersion, feedContainer.FeedResultsLimit);
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    logger.Debug($"GetFeedAsync<{typeParameterType.Name}> returned with {feedResult.Data.Count.ToString()} records.");
                    feedContainer.LastFeedVersion = feedResult.ToVersion;

                    // Add FeedResult data to the FeedContainer.
                    foreach (Entity feedResultItem in feedResult.Data)
                    {
                        feedContainer.FeedResultData.Add(feedResultItem.Id, feedResultItem);
                    }

                    // Determine whether the feed is up-to-date.
                    if (feedResult.Data.Count < FeedCurrentThresholdRecordCount)
                    {
                        feedContainer.FeedCurrent = true;
                    }
                    else
                    {
                        feedContainer.FeedCurrent = false;
                    }

                    feedContainer.LastFeedRetrievalTimeUtc = DateTime.UtcNow;
                    logger.Info($"{typeParameterType.Name} feed polled with {feedContainer.FeedResultData.Count.ToString()} records returned.");
                }
                else
                {
                    logger.Debug($"{typeParameterType.Name} data feed not polled; {feedContainer.FeedPollingIntervalSeconds.ToString()} seconds have not passed since last poll.");
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{typeParameterType.Name}'");
        }

        /// <summary>
        /// Rolls-back the LastFeedVersion of each <see cref="FeedContainer"/> to the last processed feed version that was committed to the database. Also resets the LastFeedRetrievalTimeUtc to <see cref="DateTime.MinValue"/>. For use in the event that not all feed results for a given iteration are successfully processed due to a connectivity issue.
        /// </summary>
        /// <param name="dbConfigFeedVersions">The list of <see cref="DbConfigFeedVersion"/> objects conatining the latest ToVersion for each of the supported data feeds.</param>
        /// <returns></returns>
        public void RollbackFeedContainerLastFeedVersions(List<DbConfigFeedVersion> dbConfigFeedVersions)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // LogRecords:
            DbConfigFeedVersion logRecordDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.LogRecord.ToString()).First();
            logRecordFeedContainer.LastFeedVersion = logRecordDbConfigFeedVersion.LastProcessedFeedVersion;
            logRecordFeedContainer.LastFeedRetrievalTimeUtc = DateTime.MinValue;

            // StatusData:
            DbConfigFeedVersion statusDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.StatusData.ToString()).First();
            statusDataFeedContainer.LastFeedVersion = statusDataDbConfigFeedVersion.LastProcessedFeedVersion;
            statusDataFeedContainer.LastFeedRetrievalTimeUtc = DateTime.MinValue;

            // FaultData:
            DbConfigFeedVersion faultDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.FaultData.ToString()).First();
            faultDataFeedContainer.LastFeedVersion = faultDataDbConfigFeedVersion.LastProcessedFeedVersion;
            faultDataFeedContainer.LastFeedRetrievalTimeUtc = DateTime.MinValue;

            // DVIRLogs:
            DbConfigFeedVersion dvirLogDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.DVIRLog.ToString()).First();
            dvirLogFeedContainer.LastFeedVersion = dvirLogDbConfigFeedVersion.LastProcessedFeedVersion;
            dvirLogFeedContainer.LastFeedRetrievalTimeUtc = DateTime.MinValue;

            // Trips:
            DbConfigFeedVersion tripDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.Trip.ToString()).First();
            tripFeedContainer.LastFeedVersion = tripDbConfigFeedVersion.LastProcessedFeedVersion;
            tripFeedContainer.LastFeedRetrievalTimeUtc = DateTime.MinValue;

            // ExceptionEvents:
            DbConfigFeedVersion exceptionEventDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.ExceptionEvent.ToString()).First();
            exceptionEventFeedContainer.LastFeedVersion = exceptionEventDbConfigFeedVersion.LastProcessedFeedVersion;
            exceptionEventFeedContainer.LastFeedRetrievalTimeUtc = DateTime.MinValue;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Configures a <see cref="FeedContainer"/> for the Geotab <see cref="Entity"/> object with all the necessary attributes
        /// </summary>
        /// <typeparam name="T">The Geotab <see cref="Entity"/> concerned</typeparam>
        /// <param name="feedContainer">The <see cref="FeedContainer"/> concerned</param>
        /// <param name="current">Indicates whether the data feed for the subject <see cref="Entity"/> is up-to-date.</param>
        /// <param name="enabled">Indicates whether data feed polling for the subject <see cref="Entity"/> type is enabled.</param>
        /// <param name="pollingIntervalSeonds">The minimum number of seconds to wait between GetFeed() calls for the subject <see cref="Entity"/> type.</param>
        /// <param name="startOption">Determines whether the next GetFeed() call for the subject <see cref="Entity"/> type will use FromDate or FromVersion.</param>
        /// <param name="startTime">The <see cref="DateTime"/> to use as the FromDate when making the next GetFeed() call for the subject <see cref="Entity"/> type if <see cref="FeedStartOption"/> is set to <see cref="Common.FeedStartOption.CurrentTime"/> or <see cref="Common.FeedStartOption.SpecificTime"/>.</param>
        /// <param name="resultsLimit">The results limit to be supplied to the GetFeed() method for the subject <see cref="Entity"/> type.</param>
        /// <param name="lastFeedVersion">The <see cref="FeedResult{T}.ToVersion"/> returned by the latest GetFeed() call.</param>
        static void SetupFeedContainer<T>(FeedContainer feedContainer, bool current, bool enabled, int pollingIntervalSeonds,
            Globals.FeedStartOption startOption, DateTime startTime, int resultsLimit, long lastFeedVersion)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Setup a data feed for LogRecords.
            feedContainer.FeedCurrent = current;
            feedContainer.FeedEnabled = enabled;
            feedContainer.FeedPollingIntervalSeconds = pollingIntervalSeonds;
            feedContainer.FeedStartOption = startOption;
            feedContainer.FeedStartTimeUtc = startTime;
            feedContainer.FeedResultData = new Dictionary<Id, T>();
            feedContainer.FeedResultsLimit = resultsLimit;
            feedContainer.LastFeedVersion = lastFeedVersion;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
