using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// Interface for a generic base repository class that handles database CRUD operations.
    /// </summary>
    /// <typeparam name="T">The type of entity to be used for representation of database records and for which CRUD operations are to be performed.</typeparam>
    public interface IBaseRepository2<T> : IDisposable where T : class
    {
        /// <summary>
        /// A representative maximum DateTime value to supply as a database parameter in place of null.
        /// </summary>
        DateTime MaxDateTimeForDatabaseParameters { get; }

        /// <summary>
        /// A representative minimum DateTime value to supply as a database parameter in place of null.
        /// </summary>
        DateTime MinDateTimeForDatabaseParameters { get; }

        /// <summary>
        /// Deletes a <see cref="T"/> entity from its associated database table.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be deleted.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        Task<bool> DeleteAsync(T entity, CancellationTokenSource methodCancellationTokenSource);

        /// <summary>
        /// Returns all <see cref="T"/> entities from the associated database table or view.
        /// </summary>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="resultsLimit">The maximum number of entities to return. If null, no limit is applied.</param>
        /// <param name="changedSince">Only select entities where the <see cref="ChangeTrackerAttribute"/> property has a value greater than this <see cref="DateTime"/>. If null, no <see cref="DateTime"/> filter is applied.</param>
        /// <returns></returns>
        Task<IEnumerable<T>> GetAllAsync(CancellationTokenSource methodCancellationTokenSource, int? resultsLimit = null, DateTime? changedSince = null);

        /// <summary>
        /// Returns a collection of <see cref="T"/> entities matching the specified search criteria from the associated database table.
        /// </summary>
        /// <param name="dynamicParams">The dynamic parameters to be used comprise the WHERE clause of the subject operation.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="resultsLimit">The maximum number of entities to return. If null, no limit is applied.</param>
        /// <param name="changedSince">Only select entities where the <see cref="ChangeTrackerAttribute"/> property has a value greater than this <see cref="DateTime"/>. If null, no <see cref="DateTime"/> filter is applied.</param>
        /// <returns></returns>
        Task<IEnumerable<T>> GetAsync(dynamic dynamicParams, CancellationTokenSource methodCancellationTokenSource, int? resultsLimit = null, DateTime? changedSince = null);

        /// <summary>
        /// Returns a <see cref="T"/> entity from its associated database table.
        /// </summary>
        /// <param name="Id">The ID of the <see cref="T"/> entity to be returned.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        Task<T> GetAsync(int Id, CancellationTokenSource methodCancellationTokenSource);

        /// <summary>
        /// Returns a <see cref="T"/> entity from its associated database table.
        /// </summary>
        /// <param name="Id">The ID of the <see cref="T"/> entity to be returned.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        Task<T> GetAsync(string Id, CancellationTokenSource methodCancellationTokenSource);

        /// <summary>
        /// Inserts a <see cref="T"/> entity into its associated database table.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be inserted.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        Task<long> InsertAsync(T entity, CancellationTokenSource methodCancellationTokenSource);

        /// <summary>
        /// Inserts a number of a <see cref="T"/> entities into the associated database table.
        /// </summary>
        /// <param name="entities">The <see cref="T"/> entities to be inserted.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        Task<long> InsertAsync(IEnumerable<T> entities, CancellationTokenSource methodCancellationTokenSource);

        /// <summary>
        /// Updates a <see cref="T"/> entity in its associated database table.
        /// </summary>
        /// <param name="entity">The <see cref="T"/> entity to be updated.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        Task<bool> UpdateAsync(T entity, CancellationTokenSource methodCancellationTokenSource);

        /// <summary>
        /// Updates a number of a <see cref="T"/> entities in the associated database table.
        /// </summary>
        /// <param name="entities">The <see cref="T"/> entities to be updated.</param>
        /// <param name="methodCancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        Task<long> UpdateAsync(IEnumerable<T> entities, CancellationTokenSource methodCancellationTokenSource);
    }
}
