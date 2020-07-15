using Geotab.Checkmate;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using MyGeotabAPIAdapter.Database;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Global variables and methods.
    /// </summary>
    public static class Globals
    {
        // Exception-related constants.
        public const string MyGeotabConnectionExceptionMessageSource = "System.Net.Http";
        public const string MyGeotabConnectionExceptionMessage_DbUnavailableException = "DbUnavailableException";
        
        // For GetFeed result limts see <see href="https://geotab.github.io/sdk/software/api/reference/#M:Geotab.Checkmate.Database.DataStore.GetFeed1">GetFeed(...)</see>.
        public const int GetFeedResultLimitDefault = 50000;
        public const int GetFeedResultLimitDevice = 5000;
        public const int GetFeedResultLimitRoute = 10000;
        public const int GetFeedResultLimitRule = 10000;
        public const int GetFeedResultLimitUser = 5000;
        public const int GetFeedResultLimitZone = 10000;

        // NLog log level names:
        public const string NLogLogLevelNameDebug = "Debug";
        public const string NLogLogLevelNameError = "Error";
        public const string NLogLogLevelNameFatal = "Fatal";
        public const string NLogLogLevelNameInfo = "Info";
        public const string NLogLogLevelNameOff = "Off";
        public const string NLogLogLevelNameTrace = "Trace";
        public const string NLogLogLevelNameWarn = "Warn";

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Types of connectivity issues that may be encountered.
        /// </summary>
        public enum ConnectivityIssueType { Database, MyGeotab }

        /// <summary>
        /// Interval types for use when working with DateTime functions.
        /// </summary>
        public enum DateTimeIntervalType { Seconds, Minutes, Hours, Days }

        /// <summary>
        /// Alternate ways that polling via data feeds may be initiated. <see cref="FeedStartOption.CurrentTime"/> = feed to be started at the current point in time; <see cref="FeedStartOption.SpecificTime"/> = feed to be started as a specific point in time (in the past); <see cref="FeedStartOption.FeedVersion"/> = feed to be started using a specific version (i.e. to continue from where it left-off).
        /// </summary>
        public enum FeedStartOption { CurrentTime, SpecificTime, FeedVersion }

        /// <summary>
        /// A list of MyGeotab object types for which data feeds are utilized in this application (i.e. those for which the <see cref="FeedManager"/> has a <see cref="FeedContainer"/>). Any time a new feed type is added, this enum will need to be updated. 
        /// </summary>
        public enum SupportedFeedTypes { DVIRLog, ExceptionEvent, FaultData, LogRecord, StatusData, Trip }

        /// <summary>
        /// The Global ConfigurationManager reference object
        /// </summary>
        public static ConfigurationManager ConfigurationManager { get; set; }

        /// <summary>
        /// Indicates whether all <see cref="Exception"/>(s) in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s are <see cref="TaskCanceledException"/>s.
        /// </summary>
        /// <param name="aggregateException"></param>
        /// <returns></returns>
        public static bool AllInnerExceptionsAreTaskCanceledExceptions(AggregateException aggregateException)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            bool allInnerExceptionsAreTaskCanceledExceptions = true;
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (!(exception is TaskCanceledException))
                {
                    allInnerExceptionsAreTaskCanceledExceptions = false;
                    break;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return allInnerExceptionsAreTaskCanceledExceptions;
        }

        /// <summary>
        /// The Global <see cref="API"/> for the project that may be authenticated using the <see cref="AuthenticateMyGeotabApiAsync"/> method, which will use the parameter values gathered by the <see cref="MyGeotabAPIAdapter.ConfigurationManager"/> instance.
        /// </summary>
        public static API MyGeotabAPI { get; private set; }
               
        /// <summary>
        /// Authenticates the <see cref="API"/> object contained by the <see cref="MyGeotabAPIAdapter.ConfigurationManager"/> and accessible via the <see cref="MyGeotabApi"/> property.
        /// </summary>
        /// <returns></returns>
        public static async Task AuthenticateMyGeotabApiAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Trace($"Authenticating MyGeotab API (server:'{ConfigurationManager.MyGeotabServer}', database:'{ConfigurationManager.MyGeotabDatabase}', user:'{ConfigurationManager.MyGeotabUser}').");
            MyGeotabAPI = new API(ConfigurationManager.MyGeotabUser, ConfigurationManager.MyGeotabPassword, null, ConfigurationManager.MyGeotabDatabase, ConfigurationManager.MyGeotabServer);

            try
            {
                await MyGeotabAPI.AuthenticateAsync();
            }
            catch (Exception exception)
            {
                // If the exception is related to connectivity, wrap it in a MyGeotabConnectionException. Otherwise, just pass it along.
                if (ExceptionIsRelatedToMyGeotabConnectivityLoss(exception))
                {
                    throw new MyGeotabConnectionException("An exception occurred while attempting to authenticate the MyGeotab API.", exception);
                }
                else
                {
                    throw;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Indicates whether one or more <see cref="Exception"/>s in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s is either a <see cref="MyGeotabConnectionException"/> or a <see cref="DatabaseConnectionException"/>.
        /// </summary>
        /// <param name="aggregateException"></param>
        /// <returns></returns>
        public static bool ConnectivityIssueDetected(AggregateException aggregateException)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            bool connectivityIssueDetected = false;
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is MyGeotabConnectionException || exception is DatabaseConnectionException)
                {
                    connectivityIssueDetected = true;
                    break;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return connectivityIssueDetected;
        }

        /// <summary>
        /// Indicates whether the <paramref name="exception"/> is indicative of an issue with connectivity to the MyGeotab server. Intended for use in raising <see cref="MyGeotabConnectionException"/> exceptions in this application when needed.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to be evaluated.</param>
        /// <returns></returns>
        public static bool ExceptionIsRelatedToMyGeotabConnectivityLoss(Exception exception)
        {
            if (exception.Message.Contains(MyGeotabConnectionExceptionMessage_DbUnavailableException))
            {
                return true;
            }
            if (exception.Source == MyGeotabConnectionExceptionMessageSource)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handles connectivity-related <see cref="AggregateException"/>s that may arise when executing <see cref="Task.WaitAll(Task[])"/> or similar operations. Any inner exceptions of the <paramref name="aggregateException"/> will be logged. If any are connectivity-related, a new exception of the type indicated by the <paramref name="connectivityIssueType"/> parameter will be raised using the supplied <paramref name="errorMessage"/>. Otherwise, the <paramref name="aggregateException"/> will be passed along (unless all of its inner exceptions are <see cref="TaskCanceledException"/>s, in which case the <paramref name="aggregateException"/> is ignored).
        /// </summary>
        /// <param name="aggregateException">The <see cref="AggregateException"/> to be handled.</param>
        /// <param name="connectivityIssueType">The <see cref="ConnectivityIssueType"/> which determines the type of <see cref="Exception"/> to be thrown.</param>
        /// <param name="errorMessage">The error message to be included with the exception that is thrown.</param>
        /// <returns></returns>
        public static void HandleConnectivityRelatedAggregateException(AggregateException aggregateException, ConnectivityIssueType connectivityIssueType, string errorMessage)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Any exceptions that occurred when executing any of the tasks executed via Task.WaitAll will be included in this AggregateException. Log the exception(s) and, if any are connectivity-related, raise a new MyGeotabConnectionException or DatabaseConnectionException. Otherwise, simply pass the AggregateException along (unless all of its inner exceptions are TaskCanceledExceptions, in which case the AggregateException can be ignored).
            Globals.LogExceptions(aggregateException, NLog.LogLevel.Warn);
            bool connectivityIssueDetected = Globals.ConnectivityIssueDetected(aggregateException);
            bool allInnerExceptionsAreTaskCanceledExceptions = Globals.AllInnerExceptionsAreTaskCanceledExceptions(aggregateException);

            if (connectivityIssueDetected == true)
            {
                switch (connectivityIssueType)
                {
                    case ConnectivityIssueType.Database:
                        throw new DatabaseConnectionException($"{errorMessage}", aggregateException);
                    case ConnectivityIssueType.MyGeotab:
                        throw new MyGeotabConnectionException($"{errorMessage}", aggregateException);
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

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="errorMessageLogLevel">The <see cref="LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        public static void LogException(Exception exception, LogLevel errorMessageLogLevel, string errorMessagePrefix = "An exception was encountered")
        {
            string errorMessage;
            string exceptionSource = "";
            string innerExceptionMessage = "";
            string innerExceptionSource = "";
            string innerExceptionStackTrace = "";
            if (exception.Source != null)
            {
                exceptionSource = exception.Source;
            }
            if (exception.InnerException != null)
            {
                Exception innerException = exception.InnerException;
                innerExceptionMessage = innerException.Message;
                if (innerException.Source != null)
                {
                    innerExceptionSource = innerException.Source;
                }
                if (innerException.StackTrace != null)
                {
                    innerExceptionStackTrace = innerException.StackTrace;
                }
            }

            errorMessage = $"{errorMessagePrefix}: \nMESSAGE [{exception.Message}]; \nSOURCE [{exceptionSource}]; \nINNER EXCEPTION MESSAGE [{innerExceptionMessage}]; \nINNER EXCEPTION SOURCE [{innerExceptionSource}]; \nSTACK TRACE [{exception.StackTrace}]; \nINNER EXCEPTION STACT TRACE [{innerExceptionStackTrace}]";

            string logLevelName = errorMessageLogLevel.Name;
            switch (logLevelName)
            {
                case NLogLogLevelNameDebug:
                    logger.Debug(errorMessage);
                    break;
                case NLogLogLevelNameError:
                    logger.Error(errorMessage);
                    break;
                case NLogLogLevelNameFatal:
                    logger.Fatal(errorMessage);
                    break;
                case NLogLogLevelNameInfo:
                    logger.Info(errorMessage);
                    break;
                case NLogLogLevelNameOff:
                    break;
                case NLogLogLevelNameTrace:
                    logger.Trace(errorMessage);
                    break;
                case NLogLogLevelNameWarn:
                    logger.Warn(errorMessage);
                    break;
                default:
                    logger.Debug(errorMessage);
                    break;
            }
        }

        /// <summary>
        /// Generates and logs an error message for each <see cref="Exception"/> in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s.
        /// </summary>
        /// <param name="aggregateException">The <see cref="AggregateException"/>.</param>
        /// <param name="errorMessageLogLevel">The <see cref="LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        public static void LogExceptions(AggregateException aggregateException, LogLevel errorMessageLogLevel, string errorMessagePrefix = "An exception was encountered")
        {
            foreach (var exception in aggregateException.InnerExceptions)
            {
                LogException(exception, errorMessageLogLevel, errorMessagePrefix);
            }
        }

        /// <summary>
        /// Returns a boolean indicating whether the time interval defined by adding <paramref name="interval"/> to <paramref name="startTimeUtc"/> has elapsed.
        /// </summary>
        /// <param name="startTimeUtc">The interval start time.</param>
        /// <param name="dateTimeIntervalType">The <see cref="DateTimeIntervalType"/> to use in the calcualation.</param>
        /// <param name="interval">The interval duration.</param>
        /// <returns></returns>
        public static bool TimeIntervalHasElapsed(DateTime startTimeUtc, DateTimeIntervalType dateTimeIntervalType, int interval)
        {
            DateTime endTime = DateTime.MinValue;
            switch (dateTimeIntervalType)
            {
                case DateTimeIntervalType.Seconds:
                    endTime = startTimeUtc.AddSeconds(interval);
                    break;
                case DateTimeIntervalType.Minutes:
                    endTime = startTimeUtc.AddMinutes(interval);
                    break;
                case DateTimeIntervalType.Hours:
                    endTime = startTimeUtc.AddHours(interval);
                    break;
                case DateTimeIntervalType.Days:
                    endTime = startTimeUtc.AddDays(interval);
                    break;
                default:
                    break;
            }
            if (DateTime.UtcNow > endTime)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
