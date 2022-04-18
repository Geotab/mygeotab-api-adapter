using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using NLog;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// A class that maintains an in-memory cache of <see cref="DbDiagnosticIdT"/>s.
    /// </summary>
    public class DbDiagnosticIdTObjectCache : GenericDbObjectCache<DbDiagnosticIdT>
    {
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericDbObjectCache{T}"/> class.
        /// </summary>
        public DbDiagnosticIdTObjectCache(IDateTimeHelper dateTimeHelper, UnitOfWorkContext context) : base(dateTimeHelper, context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// *** UNIT TESTING CONSTRUCTOR *** This constructor is designed for unit testing only. It accepts an additional <see cref="IBaseRepository2{T}"/> input, which may be a test class. Under normal operation, a <see cref="BaseRepository2{T}"/> is created and used within this class when required. Under normal operation, the other constructor should be used in conjunction with Dependency Injection.
        /// </summary>
        public DbDiagnosticIdTObjectCache(IDateTimeHelper dateTimeHelper, UnitOfWorkContext context, IBaseRepository2<DbDiagnosticIdT> testDbEntityRepo) : base(dateTimeHelper, context, testDbEntityRepo)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Retrieves a list of <see cref="DbDiagnosticIdT"/>s from the cache where the <see cref="DbDiagnosticIdT.GeotabGUID"/> value is equal to <paramref name="geotabGUID"/> and the <see cref="DbDiagnosticIdT.GeotabId"/> value is equal to <paramref name="geotabId"/>. If no objects are found to meet this criterion, the returned list will be empty.
        /// </summary>
        /// <param name="geotabGUID">The value of the <see cref="DbDiagnosticIdT.GeotabGUID"/> for any objects to be returned.</param>
        /// <param name="geotabId">The value of the <see cref="DbDiagnosticIdT.GeotabId"/> for any objects to be returned.</param>
        /// <returns></returns>
        public async Task<List<DbDiagnosticIdT>> GetObjectsAsync(string geotabGUID, string geotabId)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await UpdateAsync();
            await WaitIfUpdatingAsync();

            var results = new List<DbDiagnosticIdT>();
            var objectEnumerator = objectCache.GetEnumerator();
            objectEnumerator.Reset();
            while (objectEnumerator.MoveNext())
            {
                var currentKVP = objectEnumerator.Current;
                var currentObject = currentKVP.Value;
                if (currentObject.GeotabGUID == geotabGUID && currentObject.GeotabId == geotabId)
                {
                    results.Add(currentObject);
                }
            }
            return results;
        }
    }
}
