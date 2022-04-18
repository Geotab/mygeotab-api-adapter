using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// Interface for a generic class that maintains an in-memory cache of <see cref="IIdCacheableDbEntity"/>s.
    /// </summary>
    /// <typeparam name="T">The type of entity being cached.</typeparam>
    public interface IGenericDbObjectCache<T> where T : IIdCacheableDbEntity
    {
        /// <summary>
        /// The frequency, in minutes, by which the current <see cref="IGenericDbObjectCache<T>"/> should be updated to capture any new or changed objects.
        /// </summary>
        int CacheUpdateIntervalMinutes { get; set; }

        /// <summary>
        /// The default <see cref="DateTime"/> value to use in place of a null value.
        /// </summary>
        DateTime DefaultDateTime { get; }

        /// <summary>
        /// Indicates whether the <see cref="InitializeAsync(UnitOfWorkContext, Databases)"/> has been invoked since the current class instance was created.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Indicates whether the current <see cref="IGenericDbObjectCache<T>"/> is in the process of being updated.
        /// </summary>
        bool IsUpdating { get; }

        /// <summary>
        /// The last time the current <see cref="IGenericDbObjectCache<T>"/> was updated. If the current <see cref="IGenericDbObjectCache<T>"/> was never updated, the value wil be equal to <see cref="DefaultDateTime"/>.
        /// </summary>
        DateTime LastUpdated { get; }

        /// <summary>
        /// Indicates whether the current <see cref="IGenericDbObjectCache<T>"/> contains any items.
        /// </summary>
        /// <returns></returns>
        bool Any();

        /// <summary>
        /// Retrieves an object from the cache. If not found, returns <c>null</c>.
        /// </summary>
        /// <param name="id">The <see cref="IIdCacheableDbEntity.id"/> of the object to retrieve from the cache.</param>
        /// <returns></returns>
        Task<T> GetObjectAsync(long id);

        /// <summary>
        /// Retrieves an object from the cache. If not found, returns <c>null</c>.
        /// </summary>
        /// <param name="geotabId">The <see cref="IIdCacheableDbEntity.GeotabId"/> of the object to retrieve from the cache.</param>
        /// <returns></returns>
        Task<T> GetObjectAsync(string geotabId);

        /// <summary>
        /// Retrieves the <see cref="IIdCacheableDbEntity.id"/> of an object in the cache. If not found, returns <c>null</c>.
        /// </summary>
        /// <param name="geotabId">The <see cref="IIdCacheableDbEntity.GeotabId"/> of the object in the cache for which to return the <see cref="IIdCacheableDbEntity.id"/>.</param>
        /// <returns></returns>
        Task<long?> GetObjectIdAsync(string geotabId);

        /// <summary>
        /// Retrieves all objects from the cache. If no objects are found, the returned list will be empty.
        /// </summary>
        /// <returns></returns>
        Task<List<T>> GetObjectsAsync();

        /// <summary>
        /// Retrieves a list of objects from the cache where the <see cref="IIdCacheableDbEntity.LastUpsertedUtc"/> value is greater than or equal to <paramref name="changedSince"/>. If no objects are found to meet this criterion, the returned list will be empty.
        /// </summary>
        /// <param name="changedSince">The <see cref="DateTime"/> to be used as the minimum <see cref="IIdCacheableDbEntity.LastUpsertedUtc"/> value of objects to be retrieved.</param>
        /// <returns></returns>
        Task<List<T>> GetObjectsAsync(DateTime changedSince);

        /// <summary>
        /// If not already initialized, initializes the current <see cref="IGenericDbObjectCache<T>"/> instance and executes the <see cref="UpdateAsync"/> method.
        /// </summary>
        /// <param name="database">The <see cref="Databases"/> identifier (i.e. database) to use.</param>
        /// <returns></returns>
        Task InitializeAsync(Databases database);

        /// <summary>
        /// If the <see cref="CacheUpdateIntervalMinutes"/> has elapsed since <see cref="LastUpdated"/>, updates the current <see cref="IGenericDbObjectCache<T>"/> by adding any new dbEntities that were added to the database since the last time this method was called. Additionally, any entities that were changed in the database will be updated. If this is the first time this method has been called since application startup, all dbEntities will be loaded into the cache.
        /// </summary>
        /// <param name="ForceUpdate">If set to <c>true</c>, the cache will be updated regardless of whether the <see cref="CacheUpdateIntervalMinutes"/> has elapsed since <see cref="LastUpdated"/>.</param>
        /// <returns></returns>
        Task UpdateAsync(bool ForceUpdate);

        /// <summary>
        /// Waits until <see cref="IsUpdating"/> is <c>false</c>. Intended for use by methods that enumerate and retrieve cached objects.
        /// </summary>
        /// <returns></returns>
        Task WaitIfUpdatingAsync();
    }
}
