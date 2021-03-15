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
        static readonly FeedContainer dvirLogFeedContainer = new();
        static readonly FeedContainer exceptionEventFeedContainer = new();
        static readonly FeedContainer faultDataFeedContainer = new();
        static readonly FeedContainer logRecordFeedContainer = new();
        static readonly FeedContainer statusDataFeedContainer = new();
        static readonly FeedContainer tripFeedContainer = new();

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

            // If the FeedStartOption is not set to FeedVersion and the ConfigFeedVersions table has data for any of the feeds, switch the FeedStartOption to FeedVersion to avoid sending duplicate records to the database. Note that the FeedStartOption may be adjusted on a per-feed basis to account for the scenario in which the adapter was started and stopped before data was obtained for specific feeds. This is to avoid collecting ALL data for a feed when the CurrentTime or SpecificTime option was specified.
            var currentFeedStartOption = Globals.ConfigurationManager.FeedStartOption;
            if (currentFeedStartOption != Globals.FeedStartOption.FeedVersion && dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.LastProcessedFeedVersion > 0).Any())
            {
                logger.Info($"Switching FeedStartOption from '{currentFeedStartOption}' to '{Globals.FeedStartOption.FeedVersion}' to prevent data duplication because the '{ConfigurationManager.DbConfigFeedVersionsTableName}' table contains data for one or more feeds.");
                Globals.ConfigurationManager.FeedStartOption = Globals.FeedStartOption.FeedVersion;
            }

            // Setup a data feed for LogRecords.
            DbConfigFeedVersion logRecordDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.LogRecord.ToString()).First();
            SetupFeedContainer<LogRecord>(logRecordFeedContainer, false, Globals.ConfigurationManager.EnableLogRecordFeed,
                Globals.ConfigurationManager.LogRecordFeedIntervalSeconds, currentFeedStartOption, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, logRecordDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for StatusData.
            DbConfigFeedVersion statusDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.StatusData.ToString()).First();
            SetupFeedContainer<StatusData>(statusDataFeedContainer, false, Globals.ConfigurationManager.EnableStatusDataFeed,
                Globals.ConfigurationManager.StatusDataFeedIntervalSeconds, currentFeedStartOption, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, statusDataDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for FaultData.
            DbConfigFeedVersion faultDataDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.FaultData.ToString()).First();
            SetupFeedContainer<FaultData>(faultDataFeedContainer, false, Globals.ConfigurationManager.EnableFaultDataFeed,
                Globals.ConfigurationManager.FaultDataFeedIntervalSeconds, currentFeedStartOption, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, faultDataDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for DVIRLogs.
            DbConfigFeedVersion dvirLogDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.DVIRLog.ToString()).First();
            SetupFeedContainer<DVIRLog>(dvirLogFeedContainer, false, Globals.ConfigurationManager.EnableDVIRLogDataFeed,
                Globals.ConfigurationManager.DVIRLogDataFeedIntervalSeconds, currentFeedStartOption, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, dvirLogDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for Trips.
            DbConfigFeedVersion tripDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.Trip.ToString()).First();
            SetupFeedContainer<Trip>(tripFeedContainer, false, Globals.ConfigurationManager.EnableTripFeed,
                Globals.ConfigurationManager.TripFeedIntervalSeconds, currentFeedStartOption, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, tripDbConfigFeedVersion.LastProcessedFeedVersion);

            // Setup a data feed for ExceptionEvents.
            DbConfigFeedVersion exceptionEventDbConfigFeedVersion = dbConfigFeedVersions.Where(dbConfigFeedVersion => dbConfigFeedVersion.FeedTypeId == Globals.SupportedFeedTypes.ExceptionEvent.ToString()).First();
            SetupFeedContainer<ExceptionEvent>(exceptionEventFeedContainer, false, Globals.ConfigurationManager.EnableExceptionEventFeed,
                Globals.ConfigurationManager.ExceptionEventFeedIntervalSeconds, currentFeedStartOption, Globals.ConfigurationManager.FeedStartOption,
                Globals.ConfigurationManager.FeedStartSpecificTimeUTC, Globals.GetFeedResultLimitDefault, exceptionEventDbConfigFeedVersion.LastProcessedFeedVersion);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return feedManager;
        }

        /// <summary>
        /// Holds <see cref="DVIRLog"/> information obtained via data feed.
        /// </summary>
        public static FeedContainer DVIRLogFeedContainer
        {
            get => dvirLogFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="ExceptionEvent"/> information obtained via data feed.
        /// </summary>
        public static FeedContainer ExceptionEventFeedContainer
        {
            get => exceptionEventFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="FaultData"/> information obtained via data feed.
        /// </summary>
        public static FeedContainer FaultDataFeedContainer
        {
            get => faultDataFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="LogRecord"/> information obtained via data feed.
        /// </summary>
        public static FeedContainer LogRecordFeedContainer
        {
            get => logRecordFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="StatusData"/> information obtained via data feed.
        /// </summary>
        public static FeedContainer StatusDataFeedContainer
        {
            get => statusDataFeedContainer;
        }

        /// <summary>
        /// Holds <see cref="Trip"/> information obtained via data feed.
        /// </summary>
        public static FeedContainer TripFeedContainer
        {
            get => tripFeedContainer;
        }

        /// <summary>
        /// Updates the <see cref="FeedContainer.FeedResultData"/> of a <see cref="FeedContainer"/> using GetFeed() calls.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of <see cref="Entity"/> for which data is to be retrieved.</typeparam>
        /// <param name="feedContainer">The <see cref="FeedContainer"/> representing the <see cref="Type"/> of <see cref="Entity"/> for which data is to be retrieved.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        public static async Task GetFeedDataAsync<T>(FeedContainer feedContainer, CancellationTokenSource cancellationTokenSource) where T : Entity
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

                    logger.Debug($"GetFeedAsync<{typeParameterType.Name}> returned with {feedResult.Data.Count} records.");
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
                    logger.Info($"{typeParameterType.Name} feed polled with {feedContainer.FeedResultData.Count} records returned.");
                }
                else
                {
                    logger.Debug($"{typeParameterType.Name} data feed not polled; {feedContainer.FeedPollingIntervalSeconds} seconds have not passed since last poll.");
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{typeParameterType.Name}'");
        }

        /// <summary>
        /// Rolls-back the LastFeedVersion of each <see cref="FeedContainer"/> to the last processed feed version that was committed to the database. Also resets the LastFeedRetrievalTimeUtc to <see cref="DateTime.MinValue"/>. For use in the event that not all feed results for a given iteration are successfully processed due to a connectivity issue.
        /// </summary>
        /// <param name="dbConfigFeedVersions">The list of <see cref="DbConfigFeedVersion"/> objects conatining the latest ToVersion for each of the supported data feeds.</param>
        /// <returns></returns>
        public static void RollbackFeedContainerLastFeedVersions(List<DbConfigFeedVersion> dbConfigFeedVersions)
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
        /// <param name="configuredFeedStartOption">The FeedStartOption configured in appsettings.json.</param>
        /// <param name="startOption">Determines whether the next GetFeed() call for the subject <see cref="Entity"/> type will use FromDate or FromVersion.</param>
        /// <param name="startTime">The <see cref="DateTime"/> to use as the FromDate when making the next GetFeed() call for the subject <see cref="Entity"/> type if <see cref="FeedStartOption"/> is set to <see cref="Common.FeedStartOption.CurrentTime"/> or <see cref="Common.FeedStartOption.SpecificTime"/>.</param>
        /// <param name="resultsLimit">The results limit to be supplied to the GetFeed() method for the subject <see cref="Entity"/> type.</param>
        /// <param name="lastFeedVersion">The <see cref="FeedResult{T}.ToVersion"/> returned by the latest GetFeed() call.</param>
        static void SetupFeedContainer<T>(FeedContainer feedContainer, bool current, bool enabled, int pollingIntervalSeonds,
            Globals.FeedStartOption configuredFeedStartOption, Globals.FeedStartOption startOption, DateTime startTime, int resultsLimit, long lastFeedVersion)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Setup a data feed for LogRecords.
            feedContainer.FeedCurrent = current;
            feedContainer.FeedEnabled = enabled;
            feedContainer.FeedPollingIntervalSeconds = pollingIntervalSeonds;
            feedContainer.FeedStartTimeUtc = startTime;
            feedContainer.FeedResultData = new Dictionary<Id, T>();
            feedContainer.FeedResultsLimit = resultsLimit;
            feedContainer.LastFeedVersion = lastFeedVersion;

            // When the adapter is run for the first time with an empty adapter database, a record will be added to the ConfigFeedVersions table for each feed type and the LastProcessedFeedVersion value will be set to zero for each of these records. If the adapter is then stopped and restarted for any reason before data has been obtained for all feeds, the automatic switch of FeedStartOption to "FeedVersion" would result in the configured option being ignored and ALL data being retrieved for any feeds for which data was not collected prior to the stop. To avoid this issue, honour the configured FeedStartOption for the subject feed if no data has yet been collected while using the potentially-overridden option otherwise to ensure the data duplication failsafe option works for any feeds where data WAS already collected.
            if (lastFeedVersion == 0 && configuredFeedStartOption != Globals.FeedStartOption.FeedVersion && startOption == Globals.FeedStartOption.FeedVersion)
            {
                feedContainer.FeedStartOption = configuredFeedStartOption;
            }
            else
            {
                feedContainer.FeedStartOption = startOption;
            }                

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
