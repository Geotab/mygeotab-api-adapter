using Geotab.Checkmate.ObjectModel;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A generic class that builds and maintains a ConcurrentDictionary of Geotab <see cref="T"/> objects corresponding with a supplied list of Geotab <see cref="Entity"/> Ids. Intended to be injected (via Dependency Injection) into type-specific classes that are used to filter lists of other types of Geotab <see cref="Entity"/> objects that are related to the subject <see cref="T"/> objects in the <see cref="GeotabObjectsToFilterOn"/>. 
    /// </summary>
    /// <typeparam name="T">The type of <see cref="Entity"/> to comprise the <see cref="GeotabObjectsToFilterOn"/>.</typeparam>
    internal class GenericGeotabObjectFiltererBase<T> : IGenericGeotabObjectFiltererBase<T> where T : Entity
    {
        const string WildcardString = "*";

        // Obtain the type parameter type (for logging purposes).
        readonly Type typeParameterType = typeof(T);

        static string CurrentClassName { get => nameof(GenericGeotabObjectFiltererBase<T>); }
        static readonly string geotabObjectTypeName = typeof(T).Name;

        readonly TimeSpan GeotabObjectCacherInitializationCheckInterval = TimeSpan.FromSeconds(1);

        readonly SemaphoreSlim initializationLock = new(1, 1);
        bool isInitialized;
        bool isInternalUpdate;

        readonly IGenericGeotabObjectCacher<T> genericGeotabObjectCacher;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public ConcurrentDictionary<Id, T> GeotabObjectsToFilterOn { get; private set; }

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public bool IsInitialized { get => isInitialized; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericGeotabObjectFiltererBase{T}"/> class.
        /// </summary>
        public GenericGeotabObjectFiltererBase(IGenericGeotabObjectCacher<T> genericGeotabObjectCacher)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.genericGeotabObjectCacher = genericGeotabObjectCacher;
            GeotabObjectsToFilterOn = new ConcurrentDictionary<Id, T>();

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(GenericGeotabObjectFiltererBase<T>)}<{typeParameterType}> [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Builds the <see cref="GeotabObjectsToFilterOn"/> ConcurrentDictionary. 
        /// </summary>
        /// <param name="idsOfGeotabObjectsToFilterOn">The comma-separated list of <see cref="T.Id"/>s representing the <see cref="T"/>s to be filtered on.</param>
        /// <returns></returns>
        void BuildGeotabObjectsToFilterOnConcurrentDictionary(string idsOfGeotabObjectsToFilterOn)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            ValidateInitialized();
            GeotabObjectsToFilterOn.Clear();

            if (idsOfGeotabObjectsToFilterOn != string.Empty && idsOfGeotabObjectsToFilterOn != WildcardString)
            {
                string[] idList = idsOfGeotabObjectsToFilterOn.Split(',');
                for (int idListIndex = 0; idListIndex < idList.Length; idListIndex++)
                {
                    var id = Geotab.Checkmate.ObjectModel.Id.Create(idList[idListIndex]);
                    if (genericGeotabObjectCacher.GeotabObjectCache.TryGetValue(id, out T checkedGeotabObject))
                    {
                        if (GeotabObjectsToFilterOn.ContainsKey(id))
                        {
                            logger.Warn($"The value '{id}' is contained multiple times in the list of {geotabObjectTypeName}s to filter on. This instance will be ignored.");
                        }
                        else
                        {
                            GeotabObjectsToFilterOn.AddOrUpdate(id, checkedGeotabObject, (key, oldValue) => checkedGeotabObject);
                        }
                    }
                    else
                    {
                        logger.Warn($"'{id}' is not a valid {geotabObjectTypeName} Id; as such the intended {geotabObjectTypeName} will not be tracked.");
                    }
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(CancellationTokenSource cancellationTokenSource, string idsOfGeotabObjectsToFilterOn, DateTime cacheIntervalDailyReferenceStartTimeUTC, int cacheUpdateIntervalMinutes, int cacheRefreshIntervalMinutes, int feedResultsLimit, bool useGetFeed)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await initializationLock.WaitAsync();
            try
            {
                if (isInitialized == false)
                {
                    if (idsOfGeotabObjectsToFilterOn != string.Empty)
                    {
                        isInternalUpdate = true;
                        await WaitForGeotabObjectCacherInitializationAsync(cacheIntervalDailyReferenceStartTimeUTC, cacheUpdateIntervalMinutes, cacheRefreshIntervalMinutes, feedResultsLimit, useGetFeed);
                        BuildGeotabObjectsToFilterOnConcurrentDictionary(idsOfGeotabObjectsToFilterOn);
                        isInternalUpdate = false;
                    }
                    isInitialized = true;
                }
                else
                {
                    logger.Debug($"The current {CurrentClassName} has already been initialized.");
                }
            }
            finally
            {
                initializationLock.Release();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Checks whether the <see cref="InitializeAsync(CancellationTokenSource, string)"/> method has been invoked since the current class instance was created and throws an <see cref="InvalidOperationException"/> if initialization has not been performed.
        /// </summary>
        void ValidateInitialized()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (isInitialized == false && isInternalUpdate == false)
            {
                throw new InvalidOperationException($"The current {CurrentClassName} has not been initialized. The {nameof(InitializeAsync)} method must be called before other methods can be invoked.");
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Checks if the <see cref="genericGeotabObjectCacher"/> has been initialized and, if not, keeps checking every <see cref="GeotabObjectCacherInitializationCheckInterval"/> until it has been initialized.
        /// </summary>
        /// <param name="cacheIntervalDailyReferenceStartTimeUTC">Sets the <see cref="CacheIntervalDailyReferenceStartTimeUTC"/> property value.</param>
        /// <param name="cacheUpdateIntervalMinutes">Sets the <see cref="CacheUpdateIntervalMinutes"/> property value.</param>
        /// <param name="cacheRefreshIntervalMinutes">Sets the <see cref="CacheRefreshIntervalMinutes"/> property value.</param>
        /// <param name="feedResultsLimit">Sets the <see cref="FeedResultsLimit"/> property value.</param>
        /// <param name="useGetFeed">Sets the <see cref="UseGetFeed"/> property value.</param>
        /// <returns></returns>
        async Task WaitForGeotabObjectCacherInitializationAsync(DateTime cacheIntervalDailyReferenceStartTimeUTC, int cacheUpdateIntervalMinutes, int cacheRefreshIntervalMinutes, int feedResultsLimit, bool useGetFeed)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var geotabObjectCacherIsInitialized = false;
                while (geotabObjectCacherIsInitialized == false)
                {
                    geotabObjectCacherIsInitialized = genericGeotabObjectCacher.IsInitialized;
                    if (geotabObjectCacherIsInitialized == false)
                    {
                        await Task.Delay(GeotabObjectCacherInitializationCheckInterval);
                    }
                    // If the GeotabObjectCacher is still not initialized, call the initialization method.
                    geotabObjectCacherIsInitialized = genericGeotabObjectCacher.IsInitialized;
                    if (geotabObjectCacherIsInitialized == false)
                    {
                        await genericGeotabObjectCacher.InitializeAsync(cancellationTokenSource, cacheIntervalDailyReferenceStartTimeUTC, cacheUpdateIntervalMinutes, cacheRefreshIntervalMinutes, feedResultsLimit, useGetFeed);
                    }
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
