using MyGeotabAPIAdapter.Services;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that tracks the <see cref="Orchestrator"/> service.
    /// </summary>
    internal class OrchestratorServiceTracker : IOrchestratorServiceTracker
    {
        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public bool OrchestratorServiceInitialized { get; set; }
    }
}
