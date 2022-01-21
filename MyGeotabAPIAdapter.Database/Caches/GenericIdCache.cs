using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// A generic class that maintains an in-memory cache of <see cref="IIdCacheableDbEntity"/>s
    /// </summary>
    /// <typeparam name="T">The type of entity being cached.</typeparam>
    public class GenericIdCache<T> : IGenericIdCache<T> where T : class, IIdCacheableDbEntity
    {
        static string CurrentClassName { get => $"{nameof(GenericIdCache<T>)}<{typeof(T).Name}>"; }
        const int DefaultAutoRefreshIntervalMinutes = 60;
        const int MaxAutoRefreshIntervalMinutes = 1440;
        const int MinAutoRefreshIntervalMinutes = 1;

        int autoRefreshIntervalMinutes;
        IDictionary<long, string> cache;
        UnitOfWorkContext context;
        Databases database;
        bool isInitialized;
        DateTime lastRefreshed = DateTime.MinValue;

        readonly IDateTimeHelper dateTimeHelper;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public int AutoRefreshIntervalMinutes 
        {
            get => autoRefreshIntervalMinutes;
            set
            {
                if (value < MinAutoRefreshIntervalMinutes)
                {
                    autoRefreshIntervalMinutes = MinAutoRefreshIntervalMinutes;
                }
                else if (value > MaxAutoRefreshIntervalMinutes)
                {
                    autoRefreshIntervalMinutes = MaxAutoRefreshIntervalMinutes;
                }
                else
                {
                    autoRefreshIntervalMinutes = value;
                }
            }
        }

        /// <inheritdoc/>
        public bool IsInitialized { get => isInitialized; }

        /// <inheritdoc/>
        public DateTime LastRefreshed { get => lastRefreshed; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericIdCache{T}"/> class.
        /// </summary>
        public GenericIdCache(IDateTimeHelper dateTimeHelper)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dateTimeHelper = dateTimeHelper;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<string> GetGeotabIdAsync(long id)
        {
            await RefreshIfRequiredAsync();
            if (cache.TryGetValue(id, out string geotabId))
            {
                return geotabId;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public async Task<long?> GetIdAsync(string geotabId)
        {
            await RefreshIfRequiredAsync();
            var id = cache.FirstOrDefault(item => item.Value.Contains(geotabId)).Key;
            if (id != 0)
            {
                return id;
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(UnitOfWorkContext context, Databases database, int autoRefreshIntervalMinutes)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.context = context;
            this.database = database;
            this.autoRefreshIntervalMinutes = autoRefreshIntervalMinutes;
            isInitialized = true;
            await RefreshAsync();

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(UnitOfWorkContext context, Databases database)
        {
            await InitializeAsync(context, database, DefaultAutoRefreshIntervalMinutes);
        }

        /// <inheritdoc/>
        public async Task RefreshAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (isInitialized == false)
            {
                throw new InvalidOperationException($"The {nameof(InitializeAsync)} method must be called at least once before calling the {nameof(RefreshAsync)} method.");
            }

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var dbEntityRepo = new BaseRepository2<T>(context);
                using (var uow = context.CreateUnitOfWork(database))
                {
                    var dbEntites = await dbEntityRepo.GetAllAsync(cancellationTokenSource);
                    cache = new Dictionary<long, string>();
                    foreach (var dbEntity in dbEntites)
                    {
                        cache.Add(dbEntity.id, dbEntity.GeotabId);
                    }
                }
            }
            lastRefreshed = DateTime.UtcNow;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Determines if a cache refresh is required based on the <see cref="AutoRefreshIntervalMinutes"/> and, if so, executes the <see cref="RefreshAsync"/> method. A refresh is also deemed necessary if the cache contains no items.
        /// </summary>
        /// <returns></returns>
        async Task RefreshIfRequiredAsync()
        {
            if (dateTimeHelper.TimeIntervalHasElapsed(LastRefreshed, DateTimeIntervalType.Minutes, autoRefreshIntervalMinutes) || !cache.Any())
            {
                logger.Debug($"{CurrentClassName} cache refresh is required. Executing {nameof(RefreshAsync)}.");
                await RefreshAsync();
            }
        }
    }
}
