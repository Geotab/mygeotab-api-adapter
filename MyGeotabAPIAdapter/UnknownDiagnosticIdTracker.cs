using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MyGeotabAPIAdapter
{
    /// <inheritdoc/>
    internal class UnknownDiagnosticIdTracker : IUnknownDiagnosticIdTracker
    {
        ConcurrentDictionary<string, byte> unknownIds = new();

        /// <inheritdoc/>
        public bool HasUnknownDiagnosticIdStrings => !unknownIds.IsEmpty;

        /// <inheritdoc/>
        public void RegisterUnknownDiagnosticIdStrings(IEnumerable<string> diagnosticIdStrings)
        {
            foreach (var id in diagnosticIdStrings)
            {
                unknownIds.TryAdd(id, 0);
            }
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<string> ConsumeUnknownDiagnosticIdStrings()
        {
            var old = Interlocked.Exchange(ref unknownIds, new ConcurrentDictionary<string, byte>());
            return old.Keys.ToList();
        }
    }
}
