using MyGeotabAPIAdapter.DataOptimizer.Services;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// Interface for a class that tracks the <see cref="Orchestrator"/> service.
    /// </summary>
    internal interface IOrchestratorServiceTracker
    {
        /// <summary>
        /// Indicates whether the <see cref="Orchestrator"/> service has been initialized.
        /// </summary>
        bool OrchestratorServiceInitialized { get; set; }
    }
}
