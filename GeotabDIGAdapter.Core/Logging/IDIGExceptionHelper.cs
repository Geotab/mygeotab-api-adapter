namespace MyGeotabAPIAdapter.Logging
{
    /// <summary>
    /// Interface for DIG API-specific exception handling.
    /// </summary>
    public interface IDIGExceptionHelper
    {
        /// <summary>
        /// Indicates whether the <paramref name="exception"/> is related to connectivity loss with the DIG API.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to evaluate.</param>
        /// <returns><c>true</c> if the exception is connectivity-related; otherwise, <c>false</c>.</returns>
        bool ExceptionIsRelatedToDIGConnectivityLoss(Exception exception);

        /// <summary>
        /// Indicates whether one or more <see cref="Exception"/>s in the <paramref name="aggregateException"/>'s 
        /// <see cref="Exception.InnerException"/>s is a DIG API connectivity issue.
        /// </summary>
        /// <param name="aggregateException">The <see cref="AggregateException"/> to evaluate.</param>
        /// <returns><c>true</c> if a DIG connectivity issue is detected; otherwise, <c>false</c>.</returns>
        bool DIGConnectivityIssueDetected(AggregateException aggregateException);
    }
}