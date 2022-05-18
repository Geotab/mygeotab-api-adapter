using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// Interface for a generic class that maintains an in-memory cache of <see cref="IGeotabGUIDCacheableDbEntity"/>s.
    /// </summary>
    /// <typeparam name="T">The type of entity being cached.</typeparam>
    public interface IGenericGeotabGUIDCacheableDbObjectCache<T> where T : IGeotabGUIDCacheableDbEntity
    {
        /// <summary>
        /// The frequency, in minutes, by which the current <see cref="IGenericGeotabGUIDCacheableDbObjectCache<T>"/> should be updated to capture any new or changed objects.
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
        /// Indicates whether the current <see cref="IGenericGeotabGUIDCacheableDbObjectCache<T>"/> is in the process of being updated.
        /// </summary>
        bool IsUpdating { get; }

        /// <summary>
        /// The last time the current <see cref="IGenericGeotabGUIDCacheableDbObjectCache<T>"/> was updated. If the current <see cref="IGenericGeotabGUIDCacheableDbObjectCache<T>"/> was never updated, the value wil be equal to <see cref="DefaultDateTime"/>.
        /// </summary>
        DateTime LastUpdated { get; }

        /// <summary>
        /// Indicates whether the current <see cref="IGenericGeotabGUIDCacheableDbObjectCache<T>"/> contains any items.
        /// </summary>
        /// <returns></returns>
        bool Any();

        /// <summary>
        /// Retrieves an object from the cache. If not found, returns <c>null</c>.
        /// </summary>
        /// <param name="id">The <see cref="IGeotabGUIDCacheableDbEntity.id"/> of the object to retrieve from the cache.</param>
        /// <returns></returns>
        Task<T> GetObjectAsync(long id);

        /// <summary>
        /// Retrieves an object from the cache. If not found, returns <c>null</c>.
        /// </summary>
        /// <param name="geotabGUID">The <see cref="IGeotabGUIDCacheableDbEntity.GeotabGUID"/> of the object to retrieve from the cache.</param>
        /// <returns></returns>
        Task<T> GetObjectByGeotabGUIDAsync(string geotabGUID);

        /// <summary>
        /// Retrieves an object from the cache. If not found, returns <c>null</c>. If multiple objects with the same <see cref="IGeotabGUIDCacheableDbEntity.GeotabId"/> are found, the one with the most recent <see cref="IGeotabGUIDCacheableDbEntity.LastUpsertedUtc"/> value will be returned.
        /// </summary>
        /// <param name="geotabGUID">The <see cref="IGeotabGUIDCacheableDbEntity.GeotabId"/> of the object to retrieve from the cache.</param>
        /// <returns></returns>
        Task<T> GetObjectByGeotabIdAsync(string geotabId);

        /// <summary>
        /// Retrieves the <see cref="IGeotabGUIDCacheableDbEntity.id"/> of an object in the cache. If not found, returns <c>null</c>.
        /// </summary>
        /// <param name="geotabGUID">The <see cref="IGeotabGUIDCacheableDbEntity.GeotabGUID"/> of the object in the cache for which to return the <see cref="IGeotabGUIDCacheableDbEntity.id"/>.</param>
        /// <returns></returns>
        Task<long?> GetObjectIdByGeotabGUIDAsync(string geotabGUID);

        /// <summary>
        /// Retrieves the <see cref="IGeotabGUIDCacheableDbEntity.id"/> of an object in the cache. If not found, returns <c>null</c>. If multiple objects with the same <see cref="IGeotabGUIDCacheableDbEntity.GeotabId"/> are found, the <see cref="IGeotabGUIDCacheableDbEntity.id"/> of the one with the most recent <see cref="IGeotabGUIDCacheableDbEntity.LastUpsertedUtc"/> value will be returned.
        /// </summary>
        /// <param name="geotabGUID">The <see cref="IGeotabGUIDCacheableDbEntity.GeotabId"/> of the object in the cache for which to return the <see cref="IGeotabGUIDCacheableDbEntity.id"/>.</param>
        /// <returns></returns>
        Task<long?> GetObjectIdByGeotabIdAsync(string geotabId);

        /// <summary>
        /// Retrieves all objects from the cache. If no objects are found, the returned list will be empty.
        /// </summary>
        /// <returns></returns>
        Task<List<T>> GetObjectsAsync();

        /// <summary>
        /// Retrieves a list of objects from the cache where the <see cref="IGeotabGUIDCacheableDbEntity.LastUpsertedUtc"/> value is greater than or equal to <paramref name="changedSince"/>. If no objects are found to meet this criterion, the returned list will be empty.
        /// </summary>
        /// <param name="changedSince">The <see cref="DateTime"/> to be used as the minimum <see cref="IGeotabGUIDCacheableDbEntity.LastUpsertedUtc"/> value of objects to be retrieved.</param>
        /// <returns></returns>
        Task<List<T>> GetObjectsAsync(DateTime changedSince);

        /// <summary>
        /// Retrieves a list of objects from the cache where the <see cref="IGeotabGUIDCacheableDbEntity.GeotabGUID"/> value is equal to <paramref name="geotabGUID"/> and the <see cref="IGeotabGUIDCacheableDbEntity.GeotabId"/> value is equal to <paramref name="geotabId"/>. If no objects are found to meet this criterion, the returned list will be empty.
        /// </summary>
        /// <param name="geotabGUID">The value of the <see cref="IGeotabGUIDCacheableDbEntity.GeotabGUID"/> for any objects to be returned.</param>
        /// <param name="geotabId">The value of the <see cref="IGeotabGUIDCacheableDbEntity.GeotabId"/> for any objects to be returned.</param>
        /// <returns></returns>
        Task<List<T>> GetObjectsAsync(string geotabGUID, string geotabId);

        /// <summary>
        /// If not already initialized, initializes the current <see cref="IGenericGeotabGUIDCacheableDbObjectCache<T>"/> instance and executes the <see cref="UpdateAsync"/> method.
        /// </summary>
        /// <param name="database">The <see cref="Databases"/> identifier (i.e. database) to use.</param>
        /// <returns></returns>
        Task InitializeAsync(Databases database);

        /// <summary>
        /// If the <see cref="CacheUpdateIntervalMinutes"/> has elapsed since <see cref="LastUpdated"/>, updates the current <see cref="IGenericGeotabGUIDCacheableDbObjectCache<T>"/> by adding any new dbEntities that were added to the database since the last time this method was called. Additionally, any entities that were changed in the database will be updated. If this is the first time this method has been called since application startup, all dbEntities will be loaded into the cache.
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
