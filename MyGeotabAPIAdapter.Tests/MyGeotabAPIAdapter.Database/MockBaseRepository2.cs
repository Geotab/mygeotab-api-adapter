using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// A generic mock base repository class that handles database CRUD operations. Instead of interacting with an actual database, an internal list of objects is used and the methods of this class interact with that list.
    /// </summary>
    /// <typeparam name="T">The type of entity being used for mocking.</typeparam>
    public class MockBaseRepository2<T> : IBaseRepository2<T>, IDisposable where T : class, IIdCacheableDbEntity
    {
        const int TimeoutSeconds = 120;
        readonly List<T> dataStore;
        long sequence = 0;

        /// <inheritdoc/>
        public DateTime MaxDateTimeForDatabaseParameters { get => new(2100, 1, 1); }

        /// <inheritdoc/>
        public DateTime MinDateTimeForDatabaseParameters { get => new(1900, 1, 1); }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockBaseRepository2{T}"/> class.
        /// </summary>
        public MockBaseRepository2()
        {
            dataStore = new List<T>();
        }

        /// <summary>
        /// Removes a <see cref="T"/> entity the internal <see cref="List{T}"/>.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be removed.</param>
        /// <param name="methodCancellationTokenSource">NOT USED</param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync(T entity, CancellationTokenSource methodCancellationTokenSource)
        {
            await Task.Delay(1);
            int index = dataStore.FindIndex(existingEntity => existingEntity.id == entity.id);
            if (index >= 0)
            {
                dataStore.RemoveAt(index);
                return true;
            }
            throw new Exception($"Entity with id \"{entity.id}\" not found in internal list.");
        }

        /// <summary>
        /// Disposes the current <see cref="MockBaseRepository2{T}"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Add any clean-up code here.
            }
        }

        /// <summary>
        /// Returns all <see cref="T"/> entities from the internal <see cref="List{T}"/>.
        /// </summary>
        /// <param name="methodCancellationTokenSource">NOT USED</param>
        /// <param name="resultsLimit">The maximum number of entities to return. If null, no limit is applied.</param>
        /// <param name="changedSince">Only select entities where the <see cref="ChangeTrackerAttribute"/> property has a value greater than this <see cref="DateTime"/>. If null, no <see cref="DateTime"/> filter is applied.</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetAllAsync(CancellationTokenSource methodCancellationTokenSource, int? resultsLimit = null, DateTime? changedSince = null, string sortColumnName = "")
        {
            await Task.Delay(1);
            var entitiesSorted = dataStore.OrderBy(entity => entity.LastUpsertedUtc);
            var entitiesSortedWithChangeSinceApplied = entitiesSorted;
            if (changedSince != null)
            { 
                entitiesSortedWithChangeSinceApplied = (IOrderedEnumerable<T>)entitiesSorted.Where(entity => entity.LastUpsertedUtc >= changedSince).OrderBy(entity => entity.LastUpsertedUtc);
            }
            var entitiesSortedWithChangeSinceAndResultsLimitApplied = entitiesSortedWithChangeSinceApplied;
            if (resultsLimit != null)
            {
                entitiesSortedWithChangeSinceAndResultsLimitApplied = (IOrderedEnumerable<T>)entitiesSortedWithChangeSinceApplied.Take((int)resultsLimit);
            }
            return entitiesSortedWithChangeSinceAndResultsLimitApplied.ToList();
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public async Task<IEnumerable<T>> GetAsync(dynamic dynamicParams, CancellationTokenSource methodCancellationTokenSource, int? resultsLimit = null, DateTime? changedSince = null, string sortColumnName = "")
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a <see cref="T"/> entity from the internal <see cref="List{T}"/>.
        /// </summary>
        /// <param name="Id">The ID of the <see cref="T"/> entity to be returned.</param>
        /// <param name="methodCancellationTokenSource">NOT USED</param>
        /// <returns></returns>
        public async Task<T> GetAsync(int Id, CancellationTokenSource methodCancellationTokenSource)
        {
            await Task.Delay(1);
            var result = dataStore.Where(entity => entity.id == Id).FirstOrDefault();
            return result;
        }

        /// <summary>
        /// NOT IMPLEMENTED
        /// </summary>
        public async Task<T> GetAsync(string Id, CancellationTokenSource methodCancellationTokenSource)
        {
            await Task.Delay(1);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Inserts a <see cref="T"/> entity into the internal <see cref="List{T}"/>.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be inserted.</param>
        /// <param name="methodCancellationTokenSource">NOT USED</param>
        /// <returns></returns>
        public async Task<long> InsertAsync(T entity, CancellationTokenSource methodCancellationTokenSource)
        {
            await Task.Delay(1);

            entity.id = NewId();
            dataStore.Add(entity);
            return entity.id;
        }

        /// <summary>
        /// Inserts a number of <see cref="T"/> entities into the internal <see cref="List{T}"/>.
        /// </summary>
        /// <param name="entities">The <see cref="T"/> entities to be inserted.</param>
        /// <param name="methodCancellationTokenSource">NOT USED</param>
        /// <returns></returns>
        public async Task<long> InsertAsync(IEnumerable<T> entities, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationTokenSource timeoutCancellationTokenSource = new();
            timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

            long insertedEntityCount = 0;
            foreach (var entity in entities)
            {
                await InsertAsync(entity, timeoutCancellationTokenSource);
                insertedEntityCount += 1;
            }
            return insertedEntityCount;
        }

        /// <summary>
        /// Generates and returns the next Id in the sequence to simulate the database sequence.
        /// </summary>
        /// <returns></returns>
        long NewId()
        {
            sequence += 1;
            return sequence;
        }

        /// <summary>
        /// Updates a <see cref="T"/> entity in the internal <see cref="List{T}"/>.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be inserted.</param>
        /// <param name="methodCancellationTokenSource">NOT USED</param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(T entity, CancellationTokenSource methodCancellationTokenSource)
        {
            await Task.Delay(1);
            int index = dataStore.FindIndex(existingEntity => existingEntity.id == entity.id);
            if (index >= 0)
            {
                dataStore[index] = entity;
                return true;
            }
            throw new Exception($"Entity with id \"{entity.id}\" not found in internal list.");
        }

        /// <summary>
        /// Updates a number of <see cref="T"/> entities in the internal <see cref="List{T}"/>.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entities to be updated.</param>
        /// <param name="methodCancellationTokenSource">NOT USED</param>
        /// <returns></returns>
        public async Task<long> UpdateAsync(IEnumerable<T> entities, CancellationTokenSource methodCancellationTokenSource)
        {
            CancellationTokenSource timeoutCancellationTokenSource = new();
            timeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

            long updatedEntityCount = 0;
            foreach (var entity in entities)
            {
                await UpdateAsync(entity, timeoutCancellationTokenSource);
                updatedEntityCount += 1;
            }
            return updatedEntityCount;
        }
    }
}
