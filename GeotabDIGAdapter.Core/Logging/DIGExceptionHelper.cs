using MyGeotabAPIAdapter.Exceptions;
using NLog;
using System.Net.Sockets;

namespace MyGeotabAPIAdapter.Logging
{
    /// <summary>
    /// A helper class extending <see cref="ExceptionHelper"/> with DIG API-specific exception handling.
    /// </summary>
    public class DIGExceptionHelper : ExceptionHelper, IDIGExceptionHelper
    {
        // DIG API connectivity-related exception message constants.
        public const string DIGConnectionExceptionMessage_HttpRequestException = "HttpRequestException";
        public const string DIGConnectionExceptionMessage_ServiceUnavailable = "Service is unavailable";
        public const string DIGConnectionExceptionMessage_Unauthorized = "401";
        public const string DIGConnectionExceptionMessage_Forbidden = "403";

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="DIGExceptionHelper"/> class.
        /// </summary>
        public DIGExceptionHelper() : base()
        {
        }

        /// <inheritdoc/>
        public bool DIGConnectivityIssueDetected(AggregateException aggregateException)
        {
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is DIGConnectionException)
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public bool ExceptionIsRelatedToDIGConnectivityLoss(Exception exception)
        {
            if (exception.Message.Contains(DIGConnectionExceptionMessage_HttpRequestException)
                || exception.Message.Contains(DIGConnectionExceptionMessage_ServiceUnavailable)
                || exception is System.Net.Http.HttpRequestException
                || exception is TaskCanceledException
                || exception is OperationCanceledException
                || exception is SocketException
                || exception is IOException)
            {
                return true;
            }

            // Check the inner exception chain for connectivity-related exceptions.
            if (exception.InnerException != null)
            {
                return ExceptionIsRelatedToDIGConnectivityLoss(exception.InnerException);
            }

            return false;
        }
    }
}