using Geotab.Checkmate.ObjectModel;
using System;
using System.Text;

namespace MyGeotabAPIAdapter.Models
{
    /// <summary>
    /// A class that contains the results of an attempt to update a <see cref="DVIRLog"/> in a MyGeotab database.
    /// </summary>
    public class DVIRLogUpdateProcessingResult
    {
        /// <summary>
        /// Indicates whether the subject <see cref="DVIRLog"/> was sucessfully updated.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// If <see cref="Success"/> is <c>false</c>, indicates the reason for failure to update the subject <see cref="DVIRLog"/>.
        /// </summary>
        public string FailureReason { get; init; }

        /// <summary>
        /// If <see cref="Success"/> is <c>false</c>, includes exception information related to the failure to update the subject <see cref="DVIRLog"/>.
        /// </summary>
        public Exception Exception { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DVIRLogUpdateProcessingResult"/> class for a successful update of a <see cref="DVIRLog"/>.
        /// </summary>
        public static DVIRLogUpdateProcessingResult CreateSuccess()
            => new() { Success = true };

        /// <summary>
        /// Initializes a new instance of the <see cref="DVIRLogUpdateProcessingResult"/> class for a failed attempt to update a <see cref="DVIRLog"/>.
        /// </summary>
        public static DVIRLogUpdateProcessingResult CreateFailure(Exception ex)
            => new() { Success = false, Exception = ex, FailureReason = GenerateFailureMessage(ex) };

        /// <summary>
        /// Generates the <see cref="FailureReason"/> associated with a failed attempt to update a <see cref="DVIRLog"/>.
        /// </summary>
        /// <param name="exception">The <see cref="System.Exception"/> containing the information that will be used to build the <see cref="FailureReason"/>.</param>
        /// <returns></returns>
        private static string GenerateFailureMessage(Exception exception)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.Append($"TYPE: [{exception.GetType().Name}];");
            messageBuilder.Append($" MESSAGE [{exception.Message}];");

            var innerEx = exception.InnerException;
            while (innerEx != null)
            {
                messageBuilder.Append($" > INNER EXCEPTION:");
                messageBuilder.Append($" TYPE: [{innerEx.GetType().Name}];");
                messageBuilder.Append($" MESSAGE [{innerEx.Message}];");
                innerEx = innerEx.InnerException;
            }

            return messageBuilder.ToString();
        }
    }
}
