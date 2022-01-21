using System;

namespace MyGeotabAPIAdapter.Logging
{
    /// <summary>
    /// Interface for a class that handles the logging of specific types of messages that are repeated in multiple classes. Intended to reduce repetition and maintain consistency. 
    /// </summary>
    public interface IMessageLogger
    {
        /// <summary>
        /// Logs a message indicating that the <paramref name="loggingClassName"/> service is being paused and provides related details.
        /// </summary>
        /// <param name="loggingClassName">The class name of the service being paused.</param>
        /// <param name="serviceDailyStartTimeUTC">The time of day that the service is scheduled to resume operation each day.</param>
        /// <param name="serviceDailyRuntimeSeconds">The number of seconds that the service is configured to run for each day.</param>
        /// <param name="serviceNextScheduledStartTimeUTC">The next time the service is scheduled to resume operation.</param>
        void LogScheduledServicePause(string loggingClassName, TimeSpan serviceDailyStartTimeUTC, int serviceDailyRuntimeSeconds, DateTime serviceNextScheduledStartTimeUTC);

        /// <summary>
        /// Logs a message indicating that the <paramref name="loggingClassName"/> service is resuming operation after having been paused and provides related details.
        /// </summary>
        /// <param name="loggingClassName">The class name of the service resuming operation.</param>
        /// <param name="serviceDailyStartTimeUTC">The time of day that the service is scheduled to resume operation each day.</param>
        /// <param name="serviceDailyRuntimeSeconds">The number of seconds that the service is configured to run for each day.</param>
        /// <param name="nextScheduledPauseTimeUTC">The next time the service is scheduled to pause operation.</param>
        void LogScheduledServiceResumption(string loggingClassName, TimeSpan serviceDailyStartTimeUTC, int serviceDailyRuntimeSeconds, DateTime nextScheduledPauseTimeUTC);

        /// <summary>
        /// Logs a message indicating that the <paramref name="loggingClassName"/> service is being paused due to prerequisite processor(s) not running and provides related details.
        /// </summary>
        /// <param name="loggingClassName">The class name of the service being paused.</param>
        /// <param name="processorsNeverRunStatement">A sentence listing all prerequisite processors that have never been run.</param>
        /// <param name="processorsNotRunningStatement">>A sentence listing all prerequisite processors that are not currently running.</param>
        /// <param name="delayBeforeNextCheck"></param>
        void LogWaitForPrerequisiteProcessorsServicePause(string loggingClassName, string processorsNeverRunStatement, string processorsNotRunningStatement, TimeSpan delayBeforeNextCheck);

        /// <summary>
        /// Logs a message indicating that the <paramref name="loggingClassName"/> service is resuming operation now that all prerequisite processors are running.
        /// </summary>
        /// <param name="loggingClassName"></param>
        void LogWaitForPrerequisiteProcessorsServiceResumption(string loggingClassName);
    }
}
