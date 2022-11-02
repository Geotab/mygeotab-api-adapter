using MyGeotabAPIAdapter.Services;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that tracks the <see cref="Orchestrator"/> service.
    /// </summary>
    internal interface IOrchestratorServiceTracker
    {
        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates whether the <see cref="Orchestrator"/> service has been initialized.
        /// </summary>
        bool OrchestratorServiceInitialized { get; set; }
    }
}
