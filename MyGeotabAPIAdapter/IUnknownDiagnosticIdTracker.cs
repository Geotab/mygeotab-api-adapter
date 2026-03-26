using System.Collections.Generic;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Tracks diagnostic ID strings that were not found in the DiagnosticIds2 cache during StatusData or FaultData processing. DiagnosticProcessor2 consumes these IDs to determine whether an out-of-cycle sync is warranted.
    /// </summary>
    internal interface IUnknownDiagnosticIdTracker
    {
        /// <summary>
        /// <c>true</c> if there is at least one unresolved diagnostic ID registered since the last call to <see cref="ConsumeUnknownDiagnosticIdStrings"/>.
        /// </summary>
        bool HasUnknownDiagnosticIdStrings { get; }

        /// <summary>
        /// Records one or more diagnostic GUID strings that could not be resolved during data processing. Thread-safe; may be called concurrently by both StatusDataProcessor2 and FaultDataProcessor2.
        /// </summary>
        /// <param name="diagnosticIdStrings">The diagnostic ID strings (GeotabGUIDString form) that could not be found in the cache.</param>
        void RegisterUnknownDiagnosticIdStrings(IEnumerable<string> diagnosticIdStrings);

        /// <summary>
        /// Atomically retrieves all currently registered unknown diagnostic ID strings and clears the internal set. Returns an empty collection if none are registered.
        /// </summary>
        /// <returns>A read-only collection of the previously registered diagnostic ID strings.</returns>
        IReadOnlyCollection<string> ConsumeUnknownDiagnosticIdStrings();
    }
}
