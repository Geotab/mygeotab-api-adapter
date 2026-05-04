using MyGeotabAPIAdapter.Exceptions;
using NLog;
using System.Reflection;

namespace MyGeotabAPIAdapter.Logging
{
    /// <summary>
    /// A helper class extending <see cref="ExceptionHelper"/> with MyAdmin API-specific exception handling.
    /// </summary>
    public class MyAdminExceptionHelper : ExceptionHelper, IMyAdminExceptionHelper
    {
        // MyAdmin API connectivity-related exception message constants.
        public const string MyAdminConnectionExceptionMessage_HttpRequestException = "HttpRequestException";
        public const string MyAdminConnectionExceptionMessage_ServiceUnavailable = "Service temporarily unavailable";
        public const string MyAdminConnectionExceptionMessage_SessionIdInvalid = "SessionId is invalid";
        public const string MyAdminConnectionExceptionMessage_InvalidSession = "Invalid session";
        public const string MyAdminConnectionExceptionMessage_Unauthorized = "Unauthorized";

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="MyAdminExceptionHelper"/> class.
        /// </summary>
        public MyAdminExceptionHelper() : base()
        {
        }

        /// <summary>
        /// Indicates whether one or more <see cref="Exception"/>s in the <paramref name="aggregateException"/>'s 
        /// <see cref="Exception.InnerException"/>s includes any connectivity-related exception (MyGeotab, MyAdmin, or Database).
        /// </summary>
        /// <param name="aggregateException">The <see cref="AggregateException"/> to evaluate.</param>
        /// <returns><c>true</c> if any connectivity issue is detected; otherwise, <c>false</c>.</returns>
        public bool ConnectivityIssueDetectedIncludingMyAdmin(AggregateException aggregateException)
        {
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is MyGeotabConnectionException 
                    || exception is AdapterDatabaseConnectionException 
                    || exception is MyAdminConnectionException)
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public bool ExceptionIsRelatedToMyAdminConnectivityLoss(Exception exception)
        {
            if (exception is System.Net.Http.HttpRequestException
                || exception is TaskCanceledException
                || exception is OperationCanceledException
                || (exception is System.IO.IOException ioEx
                    && (ioEx.InnerException is System.Net.Sockets.SocketException
                        || ioEx.InnerException is System.IO.EndOfStreamException))
                || exception.Message.Contains(MyAdminConnectionExceptionMessage_ServiceUnavailable))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handles connectivity-related <see cref="AggregateException"/>s including MyAdmin API connectivity issues.
        /// Extends the base implementation to support <see cref="ConnectivityIssueTypeExtensions.MyAdmin"/> 
        /// and <see cref="ConnectivityIssueTypeExtensions.MyAdminOrDatabase"/> types.
        /// </summary>
        /// <param name="aggregateException">The <see cref="AggregateException"/> to be handled.</param>
        /// <param name="connectivityIssueType">The <see cref="ConnectivityIssueType"/> which determines the type of exception to be thrown.</param>
        /// <param name="errorMessage">The error message to be included with the exception that is thrown.</param>
        public override void HandleConnectivityRelatedAggregateException(AggregateException aggregateException, ConnectivityIssueType connectivityIssueType, string errorMessage)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();

            // Log all inner exceptions
            LogExceptions(aggregateException, NLogLogLevelName.Warn);

            bool connectivityIssueDetected = ConnectivityIssueDetectedIncludingMyAdmin(aggregateException);
            bool allInnerExceptionsAreTaskCanceledExceptions = AllInnerExceptionsAreTaskCanceledExceptions(aggregateException);

            if (connectivityIssueDetected)
            {
                // Handle base types first
                if (connectivityIssueType == ConnectivityIssueType.Database)
                {
                    throw new AdapterDatabaseConnectionException(errorMessage, aggregateException);
                }
                else if (connectivityIssueType == ConnectivityIssueType.MyGeotab)
                {
                    throw new MyGeotabConnectionException(errorMessage, aggregateException);
                }
                else if (connectivityIssueType == ConnectivityIssueType.MyGeotabOrDatabase)
                {
                    bool myGeotabConnectivityIssueDetected = MyGeotabConnectivityIssueDetected(aggregateException);
                    if (myGeotabConnectivityIssueDetected)
                    {
                        throw new MyGeotabConnectionException(errorMessage, aggregateException);
                    }
                    else
                    {
                        throw new AdapterDatabaseConnectionException(errorMessage, aggregateException);
                    }
                }
                // Handle extended MyAdmin types
                else if (connectivityIssueType == ConnectivityIssueTypeExtensions.MyAdmin)
                {
                    throw new MyAdminConnectionException(errorMessage, aggregateException);
                }
                else if (connectivityIssueType == ConnectivityIssueTypeExtensions.MyAdminOrDatabase)
                {
                    bool myAdminConnectivityIssueDetected = MyAdminConnectivityIssueDetected(aggregateException);
                    if (myAdminConnectivityIssueDetected)
                    {
                        throw new MyAdminConnectionException(errorMessage, aggregateException);
                    }
                    else
                    {
                        throw new AdapterDatabaseConnectionException(errorMessage, aggregateException);
                    }
                }
                else
                {
                    // Unknown connectivity issue type
                    string unsupportedTypeError = $"The ConnectivityIssueType '{connectivityIssueType.Name}' is not supported by the '{methodBase.ReflectedType.Name}.{methodBase.Name}' method.";
                    logger.Error(unsupportedTypeError);
                    throw new NotSupportedException(unsupportedTypeError);
                }
            }
            else if (!allInnerExceptionsAreTaskCanceledExceptions)
            {
                throw aggregateException;
            }
        }

        /// <inheritdoc/>
        public bool MyAdminConnectivityIssueDetected(AggregateException aggregateException)
        {
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is MyAdminConnectionException)
                {
                    return true;
                }
            }
            return false;
        }
    }
}