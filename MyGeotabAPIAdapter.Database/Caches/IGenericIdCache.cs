using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// Interface for a generic class that maintains an in-memory cache of <see cref="IIdCacheableDbEntity"/>s
    /// </summary>
    /// <typeparam name="T">The type of entity being cached.</typeparam>
    public interface IGenericIdCache<T> : IIdCache<T> where T : IIdCacheableDbEntity
    {
    }
}
