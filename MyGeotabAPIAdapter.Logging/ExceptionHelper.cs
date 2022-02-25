using MyGeotabAPIAdapter.Exceptions;
using NLog;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Logging
{
    /// <summary>
    /// A helper class to assist with exception handling and logging.
    /// </summary>
    public class ExceptionHelper : IExceptionHelper
    {
        // Exception-related constants.
        public const string MyGeotabConnectionExceptionMessageSource = "System.Net.Http";
        public const string MyGeotabConnectionExceptionMessage_DbUnavailableException = "DbUnavailableException";
        public const string MyGeotabConnectionExceptionStackTraceSource_Geotab_Checkmate_Web_WebServerInvoker = "Geotab.Checkmate.Web.WebServerInvoker";

        /// <inheritdoc/>
        public ConnectivityIssueType ConnectivityIssueType { get; }

        /// <inheritdoc/>
        public NLogLogLevelName NLogLogLevelName { get; }

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHelper"/> class.
        /// </summary>
        public ExceptionHelper()
        {

        }

        /// <inheritdoc/>
        public bool AllInnerExceptionsAreTaskCanceledExceptions(AggregateException aggregateException)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            bool allInnerExceptionsAreTaskCanceledExceptions = true;
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is not TaskCanceledException)
                {
                    allInnerExceptionsAreTaskCanceledExceptions = false;
                    break;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return allInnerExceptionsAreTaskCanceledExceptions;
        }

        /// <inheritdoc/>
        public bool ConnectivityIssueDetected(AggregateException aggregateException)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            bool connectivityIssueDetected = false;
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is MyGeotabConnectionException || exception is AdapterDatabaseConnectionException || exception is OptimizerDatabaseConnectionException)
                {
                    connectivityIssueDetected = true;
                    break;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return connectivityIssueDetected;
        }

        /// <inheritdoc/>
        public bool DatabaseConnectivityIssueDetected(AggregateException aggregateException)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            bool connectivityIssueDetected = false;
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is AdapterDatabaseConnectionException || exception is OptimizerDatabaseConnectionException)
                {
                    connectivityIssueDetected = true;
                    break;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return connectivityIssueDetected;
        }

        /// <inheritdoc/>
        public bool ExceptionIsRelatedToMyGeotabConnectivityLoss(Exception exception)
        {
            if (exception.Message.Contains(MyGeotabConnectionExceptionMessage_DbUnavailableException))
            {
                return true;
            }
            if (exception.Source == MyGeotabConnectionExceptionMessageSource)
            {
                return true;
            }
            if (exception.StackTrace != null && exception.StackTrace.Contains(MyGeotabConnectionExceptionStackTraceSource_Geotab_Checkmate_Web_WebServerInvoker))
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public void HandleConnectivityRelatedAggregateException(AggregateException aggregateException, ConnectivityIssueType connectivityIssueType, string errorMessage)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Any exceptions that occurred when executing any of the tasks executed via Task.WaitAll will be included in this AggregateException. Log the exception(s) and, if any are connectivity-related, raise a new MyGeotabConnectionException or DatabaseConnectionException. Otherwise, simply pass the AggregateException along (unless all of its inner exceptions are TaskCanceledExceptions, in which case the AggregateException can be ignored).
            LogExceptions(aggregateException, NLogLogLevelName.Warn);
            bool connectivityIssueDetected = ConnectivityIssueDetected(aggregateException);
            bool allInnerExceptionsAreTaskCanceledExceptions = AllInnerExceptionsAreTaskCanceledExceptions(aggregateException);

            if (connectivityIssueDetected == true)
            {
                switch (connectivityIssueType)
                {
                    case ConnectivityIssueType.Database:
                        throw new AdapterDatabaseConnectionException($"{errorMessage}", aggregateException);
                    case ConnectivityIssueType.MyGeotab:
                        throw new MyGeotabConnectionException($"{errorMessage}", aggregateException);
                    case ConnectivityIssueType.MyGeotabOrDatabase:
                        // In cases where it is not explicitly known whether the connectivity issue is on the MyGeotab side or the database side (e.g. when running a series of tasks that include both MyGeotab and database operations), check if the AggregateException includes a MyGeotabConnectionException and, if so, handle it. If not, then it must be a database-related connectivity issue, since connectivityIssueDetected == true. In the event that the AggregateException contains both types of connectivity exceptions, the MyGeotab one will be handled (the database one will be dealt with if it still persists once MyGeotab connectivity has been restored).
                        bool myGeotabConnectivityIssueDetected = MyGeotabConnectivityIssueDetected(aggregateException);
                        if (myGeotabConnectivityIssueDetected)
                        {
                            throw new MyGeotabConnectionException($"{errorMessage}", aggregateException);
                        }
                        else
                        {
                            throw new AdapterDatabaseConnectionException($"{errorMessage}", aggregateException);
                        }
                    default:
                        errorMessage = $"The ConnectivityIssueType type '{nameof(connectivityIssueType)}' is not supported by the '{methodBase.ReflectedType.Name}.{methodBase.Name}' method.";
                        logger.Error(errorMessage);
                        throw new Exception(errorMessage);
                }
            }
            else if (allInnerExceptionsAreTaskCanceledExceptions == false)
            {
                throw aggregateException;
            }
        }

        /// <inheritdoc/>
        public void LogException(Exception exception, NLogLogLevelName errorMessageLogLevel, string errorMessagePrefix = "An exception was encountered")
        {
            string exceptionTypeName = exception.GetType().Name;
            string exceptionStackTrace = exception.StackTrace ?? "null";

            StringBuilder messageBuilder = new();
            messageBuilder.AppendLine($"{errorMessagePrefix}:");
            messageBuilder.AppendLine($"TYPE: [{exceptionTypeName}];");
            messageBuilder.AppendLine($"MESSAGE [{exception.Message}];");
            messageBuilder.AppendLine($"STACK TRACE [{exceptionStackTrace}];");

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                exceptionTypeName = exception.GetType().Name;
                exceptionStackTrace = exception.StackTrace ?? "null";

                messageBuilder.AppendLine($"---------- INNER EXCEPTION ----------");
                messageBuilder.AppendLine($"TYPE: [{exceptionTypeName}];");
                messageBuilder.AppendLine($"MESSAGE [{exception.Message}];");
                messageBuilder.AppendLine($"STACK TRACE [{exceptionStackTrace}];");
            }

            string logLevelName = errorMessageLogLevel.ToString();
            switch (logLevelName)
            {
                case nameof(NLogLogLevelName.Debug):
                    logger.Debug(messageBuilder.ToString());
                    break;
                case nameof(NLogLogLevelName.Error):
                    logger.Error(messageBuilder.ToString());
                    break;
                case nameof(NLogLogLevelName.Fatal):
                    logger.Fatal(messageBuilder.ToString());
                    break;
                case nameof(NLogLogLevelName.Info):
                    logger.Info(messageBuilder.ToString());
                    break;
                case nameof(NLogLogLevelName.Off):
                    break;
                case nameof(NLogLogLevelName.Trace):
                    logger.Trace(messageBuilder.ToString());
                    break;
                case nameof(NLogLogLevelName.Warn):
                    logger.Warn(messageBuilder.ToString());
                    break;
                default:
                    logger.Debug(messageBuilder.ToString());
                    break;
            }
        }

        /// <inheritdoc/>
        public void LogExceptions(AggregateException aggregateException, NLogLogLevelName errorMessageLogLevel, string errorMessagePrefix = "An exception was encountered")
        {
            foreach (var exception in aggregateException.InnerExceptions)
            {
                LogException(exception, errorMessageLogLevel, errorMessagePrefix);
            }
        }

        /// <inheritdoc/>
        public bool MyGeotabConnectivityIssueDetected(AggregateException aggregateException)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            bool connectivityIssueDetected = false;
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is MyGeotabConnectionException)
                {
                    connectivityIssueDetected = true;
                    break;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return connectivityIssueDetected;
        }
    }
}
