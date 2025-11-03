using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Models
{
    /// <summary>
    /// A class that contains the results of the processing of a batch of items or tasks.
    /// </summary>
    public class BatchProcessingResult
    {
        /// <summary>
        /// The total number of items or tasks in the batch that was processed.
        /// </summary>
        public required int TotalCount { get; init; }

        /// <summary>
        /// The number of items or tasks in the batch that were successfully processed.
        /// </summary>
        public required int SuccessCount { get; init; }

        /// <summary>
        /// The number of items or tasks in the batch that were NOT successfully processed.
        /// </summary>
        public required int FailureCount { get; init; }

        /// <summary>
        /// Indicates whether throttling (e.g. adding a delay before initiating processing of the next batch of items or tasks) should be engaged.
        /// </summary>
        /// <param name="throttleThreshold">The threshold by which <c>true</c> will be returned if the provided value is greater than <see cref="TotalCount"/>.</param>
        public bool ShouldThrottle(int throttleThreshold)
            => TotalCount < throttleThreshold;
    }
}
