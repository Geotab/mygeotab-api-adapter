using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// A generic provider of the <see cref="IGenericDbObjectCache{T}"/> interface. Used to wrap the "true provider" so that multiple implementations of the same interface can be distinguised and correctly retrieved when used with dependency injection. 
    /// </summary>
    public class GenericGenericDbObjectCache<T1, T2> : IGenericGenericDbObjectCache<T1, T2> where T1 : class, IIdCacheableDbEntity where T2 : IGenericDbObjectCache<T1>
    {
        private readonly T2 implementation;

        public int CacheUpdateIntervalMinutes { get => implementation.CacheUpdateIntervalMinutes; set => implementation.CacheUpdateIntervalMinutes = value; }

        public int Count => implementation.Count;

        public DateTime DefaultDateTime => implementation.DefaultDateTime;

        /// <inheritdoc/>
        public string Id { get; private set; }

        public bool IsInitialized => implementation.IsInitialized;

        public bool IsUpdating => implementation.IsUpdating;

        public DateTime LastUpdated => implementation.LastUpdated;

        public GenericGenericDbObjectCache(T1 t1, T2 implementation)
        { 
            this.implementation = implementation;
        }

        public bool Any()
        {
            return implementation.Any();
        }

        public async Task<T1> GetObjectAsync(long id)
        {
            return await implementation.GetObjectAsync(id);
        }

        public async Task<T1> GetObjectAsync(string geotabId)
        {
            return await implementation.GetObjectAsync(geotabId);
        }

        public async Task<long?> GetObjectIdAsync(string geotabId)
        {
            return await implementation.GetObjectIdAsync(geotabId);
        }

        public async Task<List<T1>> GetObjectsAsync()
        {
            return await implementation.GetObjectsAsync();
        }

        public async Task<List<T1>> GetObjectsAsync(DateTime changedSince)
        {
            return await implementation.GetObjectsAsync(changedSince);
        }

        public async Task InitializeAsync(Databases database)
        {
            await implementation.InitializeAsync(database);
        }

        public async Task UpdateAsync(bool forceUpdate, IList<T1> deletedItemsToRemoveFromCache = null)
        {
            await implementation?.UpdateAsync(forceUpdate, deletedItemsToRemoveFromCache);
        }

        public async Task WaitIfUpdatingAsync()
        {
            await implementation.WaitIfUpdatingAsync();
        }
    }
}
