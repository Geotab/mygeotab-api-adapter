using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that is used to filter lists of <see cref="Entity"/>s that are related to <see cref="Device"/>s based on the <see cref="AdapterConfiguration.DevicesToTrackList"/>.
    /// </summary>
    internal interface IGeotabDeviceFilterer
    {
        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Evaluates the <paramref name="entitiesToBeFiltered"/>, filters-out any entities whose <see cref="Device"/> property does not match an item in the <see cref="AdapterConfiguration.DevicesToTrackList"/> and returns the filtered list of entities.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> in the supplied list.</typeparam>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="entitiesToBeFiltered">The list of entities to be filtered.</param>
        /// <returns></returns>
        Task<List<T>> ApplyDeviceFilterAsync<T>(CancellationTokenSource cancellationTokenSource, List<T> entitiesToBeFiltered) where T : Entity;
    }
}
