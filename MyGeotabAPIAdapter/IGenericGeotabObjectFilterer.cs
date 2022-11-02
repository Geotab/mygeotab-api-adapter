using Geotab.Checkmate.ObjectModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that evaluates lists of <typeparamref name="T2"/> entities and filters-out any entities whose <typeparamref name="T1"/>-related properties do not match an item in a defined list of <typeparamref name="T1"/> entities to filter on.
    /// </summary>
    /// <typeparam name="T1">A type of <see cref="Entity"/> by which lists of <typeparamref name="T2"/> entities will be filtered.</typeparam>
    /// <typeparam name="T2">A type of <see cref="Entity"/> of which lists will be filtered against a defined list of <typeparamref name="T1"/> entities to filter on.</typeparam>
    internal interface IGenericGeotabObjectFilterer<T1, T2> where T1 : Entity where T2 : Entity
    {
        /// <summary>
        /// The list of <see cref="T1"/> Geotab objects that the the <see cref="GetFilteredGeotabObjectsAsync(List{T2})"/> method should filter the <see cref="T2"/> objects on.
        /// </summary>
        ConcurrentDictionary<Id, T1> GeotabObjectsToFilterOn { get; }

        /// <summary>
        /// Indicates whether the <see cref="InitializeAsync(CancellationTokenSource, string)"/> method has been invoked since the current class instance was created.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Evaluates the supplied list of <see cref="T2"/> entities, filters-out any entity whose <see cref="T1"/>-related property does not match an item contained in the <see cref="GeotabObjectsToFilterOn"/>, and returns the filtered list of <see cref="T2"/> entities.
        /// </summary>
        /// <param name="entitiesToBeFiltered">The list of <see cref="T2"/> entities to be filtered.</param>
        /// <returns></returns>
        List<T2> GetFilteredGeotabObjectsAsync(List<T2> entitiesToBeFiltered);

        /// <summary>
        /// If not already initialized, initializes the current <see cref="IGenericGeotabObjectFilterer{T1, T2}"/> instance and populates the <see cref="GeotabObjectsToFilterOn"/> ConcurrentDictionary.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="devicesToFilterOn">The comma-separated list of <see cref="T1.Id"/>s representing the <see cref="T1"/>s that <see cref="T2"/> entities should be filtered on.</param>
        /// <returns></returns>
        Task InitializeAsync(CancellationTokenSource cancellationTokenSource, string devicesToFilterOn);
    }
}
