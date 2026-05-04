namespace MyGeotabAPIAdapter.Logging
{
    /// <summary>
    /// Interface extending <see cref="IExceptionHelper"/> with MyAdmin API-specific exception handling.
    /// </summary>
    public interface IMyAdminExceptionHelper : IExceptionHelper
    {
        /// <summary>
        /// Indicates whether the <paramref name="exception"/> is indicative of an issue with connectivity to the MyAdmin API.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to be evaluated.</param>
        /// <returns><c>true</c> if the exception is related to MyAdmin connectivity loss; otherwise, <c>false</c>.</returns>
        bool ExceptionIsRelatedToMyAdminConnectivityLoss(Exception exception);

        /// <summary>
        /// Indicates whether one or more <see cref="Exception"/>s in the <paramref name="aggregateException"/>'s 
        /// <see cref="Exception.InnerException"/>s is a <see cref="MyAdminConnectionException"/>.
        /// </summary>
        /// <param name="aggregateException">The <see cref="AggregateException"/> to evaluate.</param>
        /// <returns><c>true</c> if a MyAdmin connectivity issue is detected; otherwise, <c>false</c>.</returns>
        bool MyAdminConnectivityIssueDetected(AggregateException aggregateException);
    }

    /// <summary>
    /// Extended <see cref="ConnectivityIssueType"/> values for MyAdmin API connectivity issues.
    /// </summary>
    public class ConnectivityIssueTypeExtensions : ConnectivityIssueType
    {
        // Extended values start at 100 to avoid collision with base values
        public static readonly ConnectivityIssueType MyAdmin = new(100, nameof(MyAdmin));
        public static readonly ConnectivityIssueType MyAdminOrDatabase = new(101, nameof(MyAdminOrDatabase));

        public ConnectivityIssueTypeExtensions() : base() { }

        public ConnectivityIssueTypeExtensions(int id, string name) : base(id, name) { }
    }
}