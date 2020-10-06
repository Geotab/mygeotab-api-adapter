using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Utilities;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Manages in-memory caches of data pulled from MyGeotab.
    /// </summary>
    class CacheManager
    {
        const int MinFeedIntervalMilliseconds = 1100;
        static readonly Logger logger = LogManager.GetCurrentClassLogger();
        static readonly CacheContainer controllerCacheContainer = new CacheContainer();
        static readonly CacheContainer deviceCacheContainer = new CacheContainer();
        static readonly CacheContainer diagnosticCacheContainer = new CacheContainer();
        static readonly CacheContainer failureModeCacheContainer = new CacheContainer();
        static readonly CacheContainer groupCacheContainer = new CacheContainer();
        static readonly CacheContainer ruleCacheContainer = new CacheContainer();
        static readonly CacheContainer unitOfMeasureCacheContainer = new CacheContainer();
        static readonly CacheContainer userCacheContainer = new CacheContainer();
        static readonly CacheContainer zoneCacheContainer = new CacheContainer();

        /// <summary>
        /// Types of operations related to cache processing.
        /// </summary>
        enum CacheOperationType { None, Refresh, Update}

        /// <summary>
        /// Constructor is private. Use CreateAsync() method to instantiate. This is to facilitate use of MyGeotab async methods, since the 'await' operator can only be used within an async method.
        /// </summary>
        private CacheManager()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CacheManager"/> class.
        /// </summary>
        public static CacheManager Create()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var cacheManager = new CacheManager();
            controllerCacheContainer.Cache = new Dictionary<Id, Controller>();
            deviceCacheContainer.Cache = new Dictionary<Id, Device>();
            deviceCacheContainer.FeedResultsLimit = Globals.GetFeedResultLimitDevice;
            diagnosticCacheContainer.Cache = new Dictionary<Id, Diagnostic>();
            failureModeCacheContainer.Cache = new Dictionary<Id, FailureMode>();
            groupCacheContainer.Cache = new Dictionary<Id, Group>();
            ruleCacheContainer.Cache = new Dictionary<Id, Rule>();
            unitOfMeasureCacheContainer.Cache = new Dictionary<Id, UnitOfMeasure>();
            userCacheContainer.Cache = new Dictionary<Id, User>();
            userCacheContainer.FeedResultsLimit = Globals.GetFeedResultLimitUser;
            zoneCacheContainer.Cache = new Dictionary<Id, Zone>();

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return cacheManager;
        }

        /// <summary>
        /// A container for an in-memory cache of <see cref="Controller"/> objects.
        /// </summary>
        public CacheContainer ControllerCacheContainer
        {
            get => controllerCacheContainer;
        }

        /// <summary>
        /// A container for an in-memory cache of <see cref="Device"/> objects.
        /// </summary>
        public CacheContainer DeviceCacheContainer
        {
            get => deviceCacheContainer;
        }

        /// <summary>
        /// A container for an in-memory cache of <see cref="Diagnostic"/> objects.
        /// </summary>
        public CacheContainer DiagnosticCacheContainer
        {
            get => diagnosticCacheContainer;
        }

        /// <summary>
        /// A container for an in-memory cache of <see cref="FailureMode"/> objects.
        /// </summary>
        public CacheContainer FailureModeCacheContainer
        {
            get => failureModeCacheContainer;
        }

        /// <summary>
        /// A container for an in-memory cache of <see cref="Group"/> objects.
        /// </summary>
        public CacheContainer GroupCacheContainer
        {
            get => groupCacheContainer;
        }

        /// <summary>
        /// A container for an in-memory cache of <see cref="Rule"/> objects.
        /// </summary>
        public CacheContainer RuleCacheContainer
        {
            get => ruleCacheContainer;
        }

        /// <summary>
        /// A container for an in-memory cache of <see cref="UnitOfMeasure"/> objects.
        /// </summary>
        public CacheContainer UnitOfMeasureCacheContainer
        {
            get => unitOfMeasureCacheContainer;
        }

        /// <summary>
        /// A container for an in-memory cache of <see cref="User"/> objects.
        /// </summary>
        public CacheContainer UserCacheContainer
        {
            get => userCacheContainer;
        }

        /// <summary>
        /// A container for an in-memory cache of <see cref="Zone"/> objects.
        /// </summary>
        public CacheContainer ZoneCacheContainer
        {
            get => zoneCacheContainer;
        }

        /// <summary>
        /// Determines what <see cref="CacheOperationType"/> is to be performed with regard to the cache in the <paramref name="cacheContainer"/> based on the supplied parameter values.
        /// </summary>
        /// <param name="cacheObjectTypeName">The type of <see cref="Entity"/> being cached.</param>
        /// <param name="cacheContainer">The <see cref="CacheContainer"/>.</param>
        /// <param name="cacheIntervalDailyReferenceStartTimeUTC">The daily time, in Coordinated Universal Time (UTC), to be used as a starting basis for determining when the cache needs to be updated or refreshed. Used to ensure that actual cache update/refresh times do not shift over time due to the addition of data processing times.</param>
        /// <param name="cacheUpdateIntervalMinutes">The frequency, in minutes, by which the subject cache should be "updated" (capturing add and update deltas).</param>
        /// <param name="cacheRefreshIntervalMinutes">The frequency, in minutes, by which the subject cache should be "refreshed" (dumped and repopulated to make identification of deleted entities possible, since deletes do not propagate from MyGeotab through the MyGeotab API data feeds).</param>
        /// <returns></returns>
        CacheOperationType GetRequiredCacheOperationType(string cacheObjectTypeName, CacheContainer cacheContainer, DateTime cacheIntervalDailyReferenceStartTimeUTC, int cacheUpdateIntervalMinutes, int cacheRefreshIntervalMinutes)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{cacheObjectTypeName}'");

            // By default, no cache operation is required, unless the following logic determines an update or refresh is needed.
            var requiredCacheOperationType = CacheOperationType.None;

            if (!cacheContainer.InitialCacheRetrievalCompleted)
            {
                // If the subject cache has not yet been retrieved since application startup, it must be refreshed - regardless of update or refresh interval settings.
                logger.Debug($"Initial '{cacheObjectTypeName}' cache retrieval not yet executed - refresh required.");
                requiredCacheOperationType = CacheOperationType.Refresh;
            }
            else
            {
                // Initialize the cache interval reference DateTime by using the current date combined with the TimeOfDay from the "CacheIntervalDailyReferenceStartTimeUTC" configured for the current cache entity type.
                var currentDateTimeUTC = DateTime.UtcNow;
                var cacheIntervalReferenceDateTimeUTC = currentDateTimeUTC.Date + cacheIntervalDailyReferenceStartTimeUTC.TimeOfDay;

                // If the calculated cache interval reference time has not yet been reached in the current day, set the cache interval reference DateTime to the same time on the previous day.
                if (currentDateTimeUTC <= cacheIntervalReferenceDateTimeUTC)
                {
                    cacheIntervalReferenceDateTimeUTC = cacheIntervalReferenceDateTimeUTC.AddDays(-1);
                }

                // Calculate the next DateTime that the subject cache should be REFRESHED. Do so by starting with the cache interval reference DateTime and adding the configured refresh interval repeatedly until the calculated DateTime exceeds the DateTime at which the cache was last refreshed.
                var nextRequiredCacheRefreshDateTimeUTC = cacheIntervalReferenceDateTimeUTC;
                while (nextRequiredCacheRefreshDateTimeUTC < cacheContainer.LastRefreshedTimeUtc)
                {
                    nextRequiredCacheRefreshDateTimeUTC = nextRequiredCacheRefreshDateTimeUTC.AddMinutes((double)cacheRefreshIntervalMinutes);
                }

                if (currentDateTimeUTC > nextRequiredCacheRefreshDateTimeUTC)
                {
                    // If the current DateTime is greater than the calculated next DateTime that the subject cache should be refreshed, then a cache REFRESH is required.
                    logger.Debug($"Required refresh time of '{nextRequiredCacheRefreshDateTimeUTC:yyyy-MM-dd HH:mm:ss 'GMT'}' has passed for '{cacheObjectTypeName}' cache - refresh required.");
                    requiredCacheOperationType = CacheOperationType.Refresh;
                }
                else
                {
                    logger.Debug($"Required refresh time of '{nextRequiredCacheRefreshDateTimeUTC:yyyy-MM-dd HH:mm:ss 'GMT'}' has not yet passed for '{cacheObjectTypeName}' cache - refresh not required.");
                    // Given that a cache refresh is not required, determine whether the cache needs to be UPDATED. Calculate the next DateTime that the subject cache should be UPDATED. Do so by starting with the cache interval reference DateTime and adding the configured update interval repeatedly until the calculated DateTime exceeds the DateTime at which the cache was last updated.
                    var nextRequiredCacheUpdateDateTimeUTC = cacheIntervalReferenceDateTimeUTC;
                    while (nextRequiredCacheUpdateDateTimeUTC < cacheContainer.LastUpdatedTimeUtc)
                    {
                        nextRequiredCacheUpdateDateTimeUTC = nextRequiredCacheUpdateDateTimeUTC.AddMinutes((double)cacheUpdateIntervalMinutes);
                    }

                    // If the current DateTime is greater than the calculated next DateTime that the subject cache should be updated, then a cache UPDATE is required.
                    if (currentDateTimeUTC > nextRequiredCacheUpdateDateTimeUTC)
                    {
                        logger.Debug($"Required update time of '{nextRequiredCacheUpdateDateTimeUTC:yyyy-MM-dd HH:mm:ss 'GMT'}' has passed for '{cacheObjectTypeName}' cache - update required.");
                        requiredCacheOperationType = CacheOperationType.Update;
                    }
                    else
                    {
                        logger.Debug($"Required update time of '{nextRequiredCacheUpdateDateTimeUTC:yyyy-MM-dd HH:mm:ss 'GMT'}' has not yet passed for '{cacheObjectTypeName}' cache - update not required.");
                    }
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{cacheObjectTypeName}'");

            return requiredCacheOperationType;
        }

        /// <summary>
        /// Replaces the supplied <paramref name="controllerToHydrate"/> with a fully-populated <see cref="Controller"/> from the <see cref="ControllerCacheContainer"/> bearing the same <see cref="Id"/>. If no match is found, returns <see cref="NoController.Value"/>.
        /// </summary>
        /// <param name="controllerToHydrate"></param>
        /// <returns></returns>
        public Controller HydrateController(Controller controllerToHydrate)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (controllerToHydrate == null || controllerToHydrate is NoController)
            {
                return NoController.Value;
            }

            Dictionary<Id, Controller> controllerCache = (Dictionary<Id, Controller>)controllerCacheContainer.Cache;
            if (controllerCache.TryGetValue(controllerToHydrate.Id, out Controller controllerToReturn))
            {
                return controllerToReturn;
            }
            else
            {
                return NoController.Value;
            }
        }

        /// <summary>
        /// Replaces the supplied <paramref name="diagnosticToHydrate"/> with a fully-populated <see cref="Diagnostic"/> from the <see cref="DiagnosticCacheContainer"/> bearing the same <see cref="Id"/>. If no match is found, returns <see cref="NoDiagnostic.Value"/>.
        /// </summary>
        /// <param name="diagnosticToHydrate"></param>
        /// <returns></returns>
        public Diagnostic HydrateDiagnostic(Diagnostic diagnosticToHydrate)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (diagnosticToHydrate == null || diagnosticToHydrate is NoDiagnostic)
            {
                return NoDiagnostic.Value;
            }

            Dictionary<Id, Diagnostic> diagnosticCache = (Dictionary<Id, Diagnostic>)diagnosticCacheContainer.Cache;
            if (diagnosticCache.TryGetValue(diagnosticToHydrate.Id, out Diagnostic diagnosticToReturn))
            {
                return diagnosticToReturn;
            }
            else
            {
                return NoDiagnostic.Value;
            }
        }

        /// <summary>
        /// Replaces the supplied <paramref name="failureModeToHydrate"/> with a fully-populated <see cref="FailureMode"/> from the <see cref="FailureModeCacheContainer"/> bearing the same <see cref="Id"/>. If no match is found, returns <see cref="NoFailureMode.Value"/>.
        /// </summary>
        /// <param name="failureModeToHydrate"></param>
        /// <returns></returns>
        public FailureMode HydrateFailureMode(FailureMode failureModeToHydrate)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (failureModeToHydrate == null || failureModeToHydrate is NoFailureMode)
            {
                return NoFailureMode.Value;
            }

            Dictionary<Id, FailureMode> failureModeCache = (Dictionary<Id, FailureMode>)failureModeCacheContainer.Cache;
            if (failureModeCache.TryGetValue(failureModeToHydrate.Id, out FailureMode failureModeToReturn))
            {
                return failureModeToReturn;
            }
            else
            {
                return NoFailureMode.Value;
            }
        }

        /// <summary>
        /// Updates the <see cref="CacheContainer.Cache"/> of a <see cref="CacheContainer"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of object being cached by the <see cref="CacheContainer"/>.</typeparam>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="cacheContainer">The <see cref="CacheContainer"/> to be updated.</param>
        /// <param name="cacheIntervalDailyReferenceStartTimeUTC">The <see cref="DateTime"/> of which the time of day portion will be used as the basis for calculation of cache update and refresh intervals for the subject cache.</param>
        /// <param name="cacheUpdateIntervalMinutes">The interval, in seconds, used to determine whether the <see cref="CacheContainer.Cache"/> needs to be updated (i.e. items added and/or updated).</param>
        /// <param name="cacheRefreshIntervalMinutes">The interval, in minutes, used to determine whether the <see cref="CacheContainer.Cache"/> needs to be refreshed (i.e. purged and fully re-populated to remove items that have been deleted).</param>
        /// <param name="isGetFeed">If <c>true</c>, a GetFeed call will be used for data retrieval. If <c>false</c>, a Get call will be used instead. GetFeed should be used whenever an entity type is supported by a data feed.</param>
        /// <returns>Asynchronous task</returns>
        public async Task UpdateCacheAsync<T>(CancellationTokenSource cancellationTokenSource, CacheContainer cacheContainer, DateTime cacheIntervalDailyReferenceStartTimeUTC, int cacheUpdateIntervalMinutes, int cacheRefreshIntervalMinutes, bool isGetFeed = true) where T : Entity
        {
            // Obtain the type parameter type (for logging purposes).
            Type typeParameterType = typeof(T);

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{typeParameterType.Name}'");

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Process cache based on required cache operation type:
            CacheOperationType requiredCacheOperationType = GetRequiredCacheOperationType(typeParameterType.Name, cacheContainer, cacheIntervalDailyReferenceStartTimeUTC, cacheUpdateIntervalMinutes, cacheRefreshIntervalMinutes);
            if (requiredCacheOperationType == CacheOperationType.None)
            {
                logger.Debug($"{typeParameterType.Name} cache not updated; {cacheUpdateIntervalMinutes} minutes have not passed since last update.");
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();

                // If a cache refresh is required, clear the existing cache so that an entirely new set of records can be captured.
                if (requiredCacheOperationType == CacheOperationType.Refresh)
                {
                    logger.Debug($"{typeParameterType.Name} cache refresh required.");
                    cacheContainer.Cache = new Dictionary<Id, T>();
                    cacheContainer.LastFeedVersion = 0;
                    logger.Info($"{typeParameterType.Name} cache cleared.");
                }

                // Populate the cache, adding new items and updating existing items with their changed counterparts from the database.  Repeat execution of the GetFeedAsync method until no more results are returned to ensure that the cache is complete and up-to-date.
                FeedResult<T> feedResult;
                bool keepGoing = true;
                int cacheRecordsAdded = 0;
                
                DateTime lastApiCallTimeUtc = DateTime.MinValue;
                while (keepGoing == true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (Globals.TimeIntervalHasElapsed(lastApiCallTimeUtc, Globals.DateTimeIntervalType.Milliseconds, MinFeedIntervalMilliseconds))
                    {
                        if (isGetFeed == true)
                        {
                            // Use the GetFeed method for data retrieval.
                            try
                            {
                                logger.Debug($"Calling GetFeedAsync<{typeParameterType.Name}>.");
                                feedResult = await MyGeotabApiUtility.GetFeedAsync<T>(Globals.MyGeotabAPI, cacheContainer.LastFeedVersion, cacheContainer.FeedResultsLimit);
                                cacheContainer.LastFeedVersion = feedResult.ToVersion;
                                logger.Debug($"GetFeedAsync<{typeParameterType.Name}> returned with {feedResult.Data.Count} records.");
                            }
                            catch (Exception)
                            {
                                cancellationTokenSource.Cancel();
                                throw;
                            }
                        }
                        else
                        {
                            // Use the Get method for data retrieval.
                            try
                            {
                                logger.Debug($"Calling GetAsync<{typeParameterType.Name}>.");
                                feedResult = new FeedResult<T>
                                {
                                    Data = await MyGeotabApiUtility.GetAsync<T>(Globals.MyGeotabAPI)
                                };
                                logger.Debug($"GetAsync<{typeParameterType.Name}> returned with {feedResult.Data.Count} records.");
                            }
                            catch (Exception)
                            {
                                cancellationTokenSource.Cancel();
                                throw;
                            }
                        }
                        lastApiCallTimeUtc = DateTime.UtcNow;
                        if (feedResult == null)
                        {
                            logger.Warn($"{methodBase.ReflectedType.Name}.{methodBase.Name} of type '{ typeParameterType.Name}' produced zero results indicating a serious problem with the subject type. Investigation required.");
                            break;
                        }
                        foreach (Entity feedResultItem in feedResult.Data)
                        {
                            // If cache is being updated, add or update cache items. If cache is being refreshed, simply add cache items.
                            if (requiredCacheOperationType == CacheOperationType.Update)
                            {
                                if (cacheContainer.Cache.Contains(feedResultItem.Id))
                                {
                                    cacheContainer.Cache[feedResultItem.Id] = feedResultItem;
                                }
                                else
                                {
                                    // Do not cache ZoneStop rules if ZoneStops are not being tracked.
                                    if (typeParameterType == typeof(Rule) && Globals.ConfigurationManager.TrackZoneStops == false)
                                    {
                                        var rule = (Rule)feedResultItem;
                                        if (rule.BaseType != ExceptionRuleBaseType.ZoneStop)
                                        {
                                            cacheContainer.Cache.Add(feedResultItem.Id, feedResultItem);
                                            cacheRecordsAdded += 1;
                                        }
                                    }
                                    else
                                    {
                                        cacheContainer.Cache.Add(feedResultItem.Id, feedResultItem);
                                        cacheRecordsAdded += 1;
                                    }
                                }
                            }
                            else
                            {
                                // Do not cache ZoneStop rules if ZoneStops are not being tracked.
                                if (typeParameterType == typeof(Rule) && Globals.ConfigurationManager.TrackZoneStops == false)
                                {
                                    var rule = (Rule)feedResultItem;
                                    if (rule.BaseType != ExceptionRuleBaseType.ZoneStop)
                                    {
                                        cacheContainer.Cache.Add(feedResultItem.Id, feedResultItem);
                                        cacheRecordsAdded += 1;
                                    }
                                }
                                else
                                {
                                    cacheContainer.Cache.Add(feedResultItem.Id, feedResultItem);
                                    cacheRecordsAdded += 1;
                                }
                            }
                        }
                        if (feedResult.Data.Count < cacheContainer.FeedResultsLimit)
                        {
                            keepGoing = false;
                        }
                    }
                }
                DateTime currentDateTime = DateTime.UtcNow;
                // The LastUpdatedTimeUtc is always updated because a refresh encompasses everyting that is done during an update.
                cacheContainer.LastUpdatedTimeUtc = currentDateTime;
                logger.Info($"{typeParameterType.Name} cache updated with {cacheRecordsAdded} records added.");
                if (requiredCacheOperationType == CacheOperationType.Refresh)
                {
                    cacheContainer.LastRefreshedTimeUtc = currentDateTime;
                    logger.Info($"{typeParameterType.Name} cache refresh completed.");
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name} for type '{typeParameterType.Name}'");
        }
    }
}
