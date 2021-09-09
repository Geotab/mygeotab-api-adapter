using Geotab.Checkmate.ObjectModel.Engine;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// Designed for use in tracking Geotab <see cref="Diagnostic"/>s that have not been included in VSSPathMap file. <see cref="StatusData"/> entities representing the subject <see cref="Diagnostic"/>s will not have OVDS server commands generated and will not be sent to an OVDS server.
    /// </summary>
    public class UnmappedDiagnostic
    {
        /// <summary>
        /// The <see cref="Diagnostic.Id"/>.
        /// </summary>
        public string DiagnosticId { get; set; }
        /// <summary>
        /// The <see cref="Diagnostic.Name"/>.
        /// </summary>
        public string DiagnosticName { get; set; }
        /// <summary>
        /// The number of <see cref="StatusData"/> entities bearing the <see cref="DiagnosticId"/> that have been processed since application startup.
        /// </summary>
        public long OccurrencesSinceApplicationStartup { get; set; }
        /// <summary>
        /// A sample showing the string representation of the <see cref="StatusData.Data"/> of the most-recently processed <see cref="StatusData"/> bearing the <see cref="DiagnosticId"/>.
        /// </summary>
        public string SampleValueString { get; set; }
    }
}
