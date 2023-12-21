using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that is used to filter lists of <see cref="Entity"/>s that are related to <see cref="Diagnostic"/>s based on the <see cref="AdapterConfiguration.DiagnosticsToTrackList"/>.
    /// </summary>
    internal interface IGeotabDiagnosticFilterer
    {
        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Evaluates the <paramref name="entitiesToBeFiltered"/>. If <see cref="AdapterConfiguration.ExcludeDiagnosticsToTrack"/> is <c>false</c>, filters-out any entities whose <see cref="Diagnostic"/> property does not match an item in the <see cref="AdapterConfiguration.DiagnosticsToTrackList"/>. If <see cref="AdapterConfiguration.ExcludeDiagnosticsToTrack"/> is <c>true</c>, filters-out ONLY entities whose <see cref="Diagnostic"/> property matches an item in the <see cref="AdapterConfiguration.DiagnosticsToTrackList"/>. Returns the filtered list of entities.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Entity"/> in the supplied list.</typeparam>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <param name="entitiesToBeFiltered">The list of entities to be filtered.</param>
        /// <returns></returns>
        Task<List<T>> ApplyDiagnosticFilterAsync<T>(CancellationTokenSource cancellationTokenSource, List<T> entitiesToBeFiltered) where T : Entity;
    }
}
