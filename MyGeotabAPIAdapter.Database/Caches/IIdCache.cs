using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// Interface for a class that maintains an in-memory cache of <see cref="IIdCacheableDbEntity"/>s
    /// </summary>
    /// <typeparam name="T">The type of entity being cached.</typeparam>
    public interface IIdCache<T> where T : IIdCacheableDbEntity
    {
        /// <summary>
        /// The maximum duration in minutes that can elapse after the cache has been refreshed before the <see cref="RefreshAsync"/> method is automatically called (the next time either the <see cref="GetGeotabId(long)"/> method or the <see cref="GetId(string)"/> method is called).
        /// </summary>
        int AutoRefreshIntervalMinutes { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="InitializeAsync(UnitOfWorkContext, Databases)"/> method has been called since this <see cref="IIdCache{T}"/> instance was created.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// The last time the cache was refreshed.
        /// </summary>
        DateTime LastRefreshed { get; }

        /// <summary>
        /// Returns the <see cref="IIdCacheableDbEntity.GeotabId"/> associated with the <paramref name="id"/> or <see cref="string.Empty"/> if not found.
        /// </summary>
        /// <param name="id">The <see cref="IIdCacheableDbEntity.id"/> for which to retrieve the corresponding <see cref="IIdCacheableDbEntity.GeotabId"/>.</param>
        /// <returns></returns>
        Task<string> GetGeotabIdAsync(long id);

        /// <summary>
        /// Returns the <see cref="IIdCacheableDbEntity.id"/> associated with the <paramref name="geotabId"/> or <c>null</c> if not found.
        /// </summary>
        /// <param name="geotabId">The <see cref="IIdCacheableDbEntity.GeotabId"/> for which to retrieve the corresponding <see cref="IIdCacheableDbEntity.id"/>.</param>
        /// <returns></returns>
        Task<long?> GetIdAsync(string geotabId);

        /// <summary>
        /// Initializes this <see cref="IIdCache{T}"/> instance and calls the <see cref="RefreshAsync"/> method to populate the cache.
        /// </summary>
        /// <param name="database">The <see cref="Databases"/> to use.</param>
        /// <param name="autoRefreshIntervalMinutes">The value to use for <see cref="AutoRefreshIntervalMinutes"/>.</param>
        /// <returns></returns>
        Task InitializeAsync(Databases database, int autoRefreshIntervalMinutes);

        /// <summary>
        /// Initializes this <see cref="IIdCache{T}"/> instance and calls the <see cref="RefreshAsync"/> method to populate the cache.
        /// </summary>
        /// <param name="database">The <see cref="Databases"/> to use.</param>
        /// <returns></returns>
        Task InitializeAsync(Databases database);

        /// <summary>
        /// Refreshes the in-memory cache with a fresh snapshot from the database.
        /// </summary>
        /// <returns></returns>
        Task RefreshAsync();
    }
}
