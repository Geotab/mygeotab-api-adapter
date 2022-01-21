using System;

namespace MyGeotabAPIAdapter.Logging
{
    /// <summary>
    /// Interface for a helper class to assist with exception handling and logging.
    /// </summary>
    public interface IExceptionHelper
    {
        /// <summary>
        /// Types of connectivity issues that may be encountered.
        /// </summary>
        ConnectivityIssueType ConnectivityIssueType { get; }

        /// <summary>
        /// NLog log level names.
        /// </summary>
        NLogLogLevelName NLogLogLevelName { get; }

        /// <summary>
        /// Indicates whether all <see cref="Exception"/>(s) in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s are <see cref="TaskCanceledException"/>s.
        /// </summary>
        /// <param name="aggregateException"></param>
        /// <returns></returns>
        bool AllInnerExceptionsAreTaskCanceledExceptions(AggregateException aggregateException);

        /// <summary>
        /// Indicates whether one or more <see cref="Exception"/>s in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s is either a <see cref="MyGeotabConnectionException"/> or a <see cref="DatabaseConnectionException"/>.
        /// </summary>
        /// <param name="aggregateException"></param>
        /// <returns></returns>
        bool ConnectivityIssueDetected(AggregateException aggregateException);

        /// <summary>
        /// Indicates whether one or more <see cref="Exception"/>s in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s is a <see cref="DatabaseConnectionException"/>.
        /// </summary>
        /// <param name="aggregateException"></param>
        /// <returns></returns>
        bool DatabaseConnectivityIssueDetected(AggregateException aggregateException);

        /// <summary>
        /// Indicates whether the <paramref name="exception"/> is indicative of an issue with connectivity to the MyGeotab server. Intended for use in raising <see cref="MyGeotabConnectionException"/> exceptions in this application when needed.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to be evaluated.</param>
        /// <returns></returns>
        bool ExceptionIsRelatedToMyGeotabConnectivityLoss(Exception exception);

        /// <summary>
        /// Handles connectivity-related <see cref="AggregateException"/>s that may arise when executing <see cref="Task.WaitAll(Task[])"/> or similar operations. Any inner exceptions of the <paramref name="aggregateException"/> will be logged. If any are connectivity-related, a new exception of the type indicated by the <paramref name="connectivityIssueType"/> parameter will be raised using the supplied <paramref name="errorMessage"/>. Otherwise, the <paramref name="aggregateException"/> will be passed along (unless all of its inner exceptions are <see cref="TaskCanceledException"/>s, in which case the <paramref name="aggregateException"/> is ignored).
        /// </summary>
        /// <param name="aggregateException">The <see cref="AggregateException"/> to be handled.</param>
        /// <param name="connectivityIssueType">The <see cref="ConnectivityIssueType"/> which determines the type of <see cref="Exception"/> to be thrown.</param>
        /// <param name="errorMessage">The error message to be included with the exception that is thrown.</param>
        /// <returns></returns>
        void HandleConnectivityRelatedAggregateException(AggregateException aggregateException, ConnectivityIssueType connectivityIssueType, string errorMessage);

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="errorMessageLogLevel">The <see cref="LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        void LogException(Exception exception, NLogLogLevelName errorMessageLogLevel, string errorMessagePrefix);

        /// <summary>
        /// Generates and logs an error message for each <see cref="Exception"/> in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s.
        /// </summary>
        /// <param name="aggregateException">The <see cref="AggregateException"/>.</param>
        /// <param name="errorMessageLogLevel">The <see cref="LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        void LogExceptions(AggregateException aggregateException, NLogLogLevelName errorMessageLogLevel, string errorMessagePrefix);

        /// <summary>
        /// Indicates whether one or more <see cref="Exception"/>s in the <paramref name="aggregateException"/>'s <see cref="Exception.InnerException"/>s is a <see cref="MyGeotabConnectionException"/>.
        /// </summary>
        /// <param name="aggregateException"></param>
        /// <returns></returns>
        bool MyGeotabConnectivityIssueDetected(AggregateException aggregateException);
    }

    /// <summary>
    /// Types of connectivity issues that may be encountered.
    /// </summary>
    public enum ConnectivityIssueType { Database, MyGeotab, MyGeotabOrDatabase }

    /// <summary>
    /// NLog log level names.
    /// </summary>
    public enum NLogLogLevelName { Fatal, Error, Warn, Info, Debug, Trace, Off }
}
