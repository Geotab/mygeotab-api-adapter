using Geotab.Checkmate.ObjectModel.Engine;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// Interface for a class that assists with tracking and logging of <see cref="UnmappedDiagnostic"/>s. Generated log entries may be evaluated in order to determine whether there are additional <see cref="Diagnostic"/>s that should be mapped in the VSSPathMaps file.
    /// </summary>
    internal interface IUnmappedDiagnosticManager
    {
        /// <summary>
        /// Extracts the <see cref="StatusData.Diagnostic"/> information from the <paramref name="statusData"/> and adds/updates the <see cref="UnmappedDiagnosticsDictionary"/>. 
        /// </summary>
        /// <param name="statusData"></param>
        void AddUnmappedDiagnosticToDictionary(StatusData statusData);

        /// <summary>
        /// Write information about all UnmappedDiagnostics that have been collected since application startup to the log file.
        /// </summary>
        void LogUnmappedDiagnostics();
    }
}
