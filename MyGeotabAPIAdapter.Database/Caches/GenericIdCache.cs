using Microsoft.VisualStudio.Threading;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using NLog;
using Polly;
using Polly.Retry;
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

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        int autoRefreshIntervalMinutes;
        IDictionary<long, string> cache;
        Databases database;
        readonly SemaphoreSlim initializationLock = new(1, 1);
        bool isInitialized;
        DateTime lastRefreshed = DateTime.MinValue;
        readonly AsyncReaderWriterLock asyncReaderWriterLock = new(null);

        readonly IDateTimeHelper dateTimeHelper;
        readonly OptimizerDatabaseUnitOfWorkContext context;
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
        public GenericIdCache(IDateTimeHelper dateTimeHelper, OptimizerDatabaseUnitOfWorkContext context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dateTimeHelper = dateTimeHelper;
            this.context = context;

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<string> GetGeotabIdAsync(long id)
        {
            await RefreshIfRequiredAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                if (cache.TryGetValue(id, out string geotabId))
                {
                    return geotabId;
                }
            }
            return string.Empty;
        }

        /// <inheritdoc/>
        public async Task<long?> GetIdAsync(string geotabId)
        {
            await RefreshIfRequiredAsync();
            using (await asyncReaderWriterLock.ReadLockAsync())
            {
                var id = cache.FirstOrDefault(item => item.Value.Contains(geotabId)).Key;
                if (id != 0)
                {
                    return id;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(Databases database, int autoRefreshIntervalMinutes)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await initializationLock.WaitAsync();
            try
            {
                this.database = database;
                this.autoRefreshIntervalMinutes = autoRefreshIntervalMinutes;
                isInitialized = true;
                await RefreshAsync();
            }
            finally
            {
                initializationLock.Release();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task InitializeAsync(Databases database)
        {
            await InitializeAsync(database, DefaultAutoRefreshIntervalMinutes);
        }

        /// <inheritdoc/>
        public async Task RefreshAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            using (await asyncReaderWriterLock.WriteLockAsync())
            {
                if (isInitialized == false)
                {
                    throw new InvalidOperationException($"The {nameof(InitializeAsync)} method must be called at least once before calling the {nameof(RefreshAsync)} method.");
                }

                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var dbEntityRepo = new BaseRepository<T>(context);
                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        using (var uow = context.CreateUnitOfWork(database))
                        {
                            var dbEntities = await dbEntityRepo.GetAllAsync(cancellationTokenSource);
                            cache = new Dictionary<long, string>();
                            foreach (var dbEntity in dbEntities)
                            {
                                cache.Add(dbEntity.id, dbEntity.GeotabId);
                            }
                        }
                    }, new Context());
                }
                lastRefreshed = DateTime.UtcNow;
            }

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
