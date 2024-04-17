using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a data reduction class that is used to reduce lists of <see cref="Entity"/>s by applying the configured <see cref="AdapterConfiguration.MinimumIntervalSamplingIntervalSeconds"/> setting to ensure that a minimum of the specified interval exists between each entity that is kept for later downstream persistence for the subject <see cref="Device"/> + <see cref="Entity"/> type (+ <see cref="Diagnostic"/> if applicable) combination."/>
    /// </summary>
    /// <typeparam name="T">The type of Geotab <see cref="Entity"/> to be processed by this <see cref="IMinimumIntervalSampler{T}"/> instance.</typeparam>
    internal interface IMinimumIntervalSampler<T> where T : Entity
    {
        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Evaluates the <paramref name="entitiesToBeFiltered"/> and returns a filtered list of entities that have a minimum interval of <see cref="AdapterConfiguration.MinimumIntervalSamplingIntervalSeconds"/> between each entity that is kept for later downstream persistence for the subject <see cref="Device"/> + <see cref="Entity"/> type (+ <see cref="Diagnostic"/> if applicable) combination.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="entitiesToBeFiltered">The list of entities to be filtered.</param>
        /// <returns></returns>
        Task<List<T>> ApplyMinimumIntervalAsync(CancellationTokenSource cancellationTokenSource, List<T> entitiesToBeFiltered);
    }
}
