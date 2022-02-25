using Geotab.Checkmate;
using System;
using System.Reflection;
using System.Text;
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
        public const string MyGeotabConnectionExceptionStackTraceSource_Geotab_Checkmate_Web_WebServerInvoker = "Geotab.Checkmate.Web.WebServerInvoker";
            
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

        // OBD-II DTC prefixes associated with Controller Ids.
        public const string OBD2DTCPrefixBody = "B";
        public const string OBD2DTCPrefixChassis = "C";
        public const string OBD2DTCPrefixNetworking = "U";
        public const string OBD2DTCPrefixPowertrain = "P";

        static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Types of connectivity issues that may be encountered.
        /// </summary>
        public enum ConnectivityIssueType { Database, MyGeotab, MyGeotabOrDatabase }

        /// <summary>
        /// Interval types for use when working with DateTime functions.
        /// </summary>
        public enum DateTimeIntervalType { Milliseconds, Seconds, Minutes, Hours, Days }

        /// <summary>
        /// Alternate ways that polling via data feeds may be initiated. <see cref="FeedStartOption.CurrentTime"/> = feed to be started at the current point in time; <see cref="FeedStartOption.SpecificTime"/> = feed to be started as a specific point in time (in the past); <see cref="FeedStartOption.FeedVersion"/> = feed to be started using a specific version (i.e. to continue from where it left-off).
        /// </summary>
        public enum FeedStartOption { CurrentTime, SpecificTime, FeedVersion }

        /// <summary>
        /// A list of MyGeotab object types for which data feeds are utilized in this application (i.e. those for which the <see cref="FeedManager"/> has a <see cref="FeedContainer"/>). Any time a new feed type is added, this enum will need to be updated. 
        /// </summary>
        //public enum SupportedFeedTypes { DriverChange, DVIRLog, ExceptionEvent, FaultData, LogRecord, StatusData, Trip }
        public enum SupportedFeedTypes { BinaryData, DeviceStatusInfo, DriverChange, DVIRLog, ExceptionEvent, FaultData, LogRecord, StatusData, Trip }

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
                if (exception is not TaskCanceledException)
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
        /// Indicates whether one or more <see cref="Exception"/>s in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s is a <see cref="DatabaseConnectionException"/>.
        /// </summary>
        /// <param name="aggregateException"></param>
        /// <returns></returns>
        public static bool DatabaseConnectivityIssueDetected(AggregateException aggregateException)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            bool connectivityIssueDetected = false;
            foreach (var exception in aggregateException.InnerExceptions)
            {
                if (exception is DatabaseConnectionException)
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
            if (exception.StackTrace != null && exception.StackTrace.Contains(MyGeotabConnectionExceptionStackTraceSource_Geotab_Checkmate_Web_WebServerInvoker))
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
            LogExceptions(aggregateException, NLog.LogLevel.Warn);
            bool connectivityIssueDetected = ConnectivityIssueDetected(aggregateException);
            bool allInnerExceptionsAreTaskCanceledExceptions = AllInnerExceptionsAreTaskCanceledExceptions(aggregateException);

            if (connectivityIssueDetected == true)
            {
                switch (connectivityIssueType)
                {
                    case ConnectivityIssueType.Database:
                        throw new DatabaseConnectionException($"{errorMessage}", aggregateException);
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
                            throw new DatabaseConnectionException($"{errorMessage}", aggregateException);
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

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="errorMessageLogLevel">The <see cref="LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        public static void LogException(Exception exception, LogLevel errorMessageLogLevel, string errorMessagePrefix = "An exception was encountered")
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
                case NLogLogLevelNameDebug:
                    logger.Debug(messageBuilder.ToString());
                    break;
                case NLogLogLevelNameError:
                    logger.Error(messageBuilder.ToString());
                    break;
                case NLogLogLevelNameFatal:
                    logger.Fatal(messageBuilder.ToString());
                    break;
                case NLogLogLevelNameInfo:
                    logger.Info(messageBuilder.ToString());
                    break;
                case NLogLogLevelNameOff:
                    break;
                case NLogLogLevelNameTrace:
                    logger.Trace(messageBuilder.ToString());
                    break;
                case NLogLogLevelNameWarn:
                    logger.Warn(messageBuilder.ToString());
                    break;
                default:
                    logger.Debug(messageBuilder.ToString());
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
        /// Indicates whether one or more <see cref="Exception"/>s in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s is a <see cref="MyGeotabConnectionException"/>.
        /// </summary>
        /// <param name="aggregateException"></param>
        /// <returns></returns>
        public static bool MyGeotabConnectivityIssueDetected(AggregateException aggregateException)
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
                case DateTimeIntervalType.Milliseconds:
                    endTime = startTimeUtc.AddMilliseconds(interval);
                    break;
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
