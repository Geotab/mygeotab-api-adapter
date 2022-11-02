using MyGeotabAPIAdapter.DataOptimizer.Services;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A class that tracks the <see cref="Orchestrator"/> service.
    /// </summary>
    internal class OrchestratorServiceTracker : IOrchestratorServiceTracker
    {
        /// <inheritdoc/>
        public bool OrchestratorServiceInitialized { get; set; }
    }
}
