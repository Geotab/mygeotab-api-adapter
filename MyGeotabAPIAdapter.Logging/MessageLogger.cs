﻿using NLog;
using System;
using System.Text;

namespace MyGeotabAPIAdapter.Logging
{
    /// <summary>
    /// A class that handles the logging of specific types of messages that are repeated in multiple classes. Intended to reduce repetition and maintain consistency. 
    /// </summary>
    public class MessageLogger : IMessageLogger
    {
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageLogger"/> class.
        /// </summary>
        public MessageLogger()
        { 
        
        }

        /// <inheritdoc/>
        public void LogScheduledServicePause(string loggingClassName, TimeSpan serviceDailyStartTimeUTC, int serviceDailyRuntimeSeconds, DateTime serviceNextScheduledStartTimeUTC)
        {
            logger.Info($"******** PAUSING SERVICE: {loggingClassName}. This service is configured to operate on a scheduled basis, starting each day at {serviceDailyStartTimeUTC} (UTC) and running for a duration of {serviceDailyRuntimeSeconds} seconds. Since it is now outside of the scheduled operating window, the service will be paused and will resume at the next scheduled daily start time, which is {serviceNextScheduledStartTimeUTC} (UTC).");
        }

        /// <inheritdoc/>
        public void LogScheduledServiceResumption(string loggingClassName, TimeSpan serviceDailyStartTimeUTC, int serviceDailyRuntimeSeconds, DateTime nextScheduledPauseTimeUTC)
        {
            logger.Info($"******** RESUMING SERVICE: {loggingClassName}. This service is configured to operate on a scheduled basis, starting each day at {serviceDailyStartTimeUTC} (UTC) and running for a duration of {serviceDailyRuntimeSeconds} seconds. Since it is now within the scheduled operating window, the service will resume and will continue until the next scheduled pause time, which is {nextScheduledPauseTimeUTC} (UTC).");
        }

        /// <inheritdoc/>
        public void LogWaitForOrchestratorServiceServicePause(string loggingClassName, TimeSpan delayBeforeNextCheck)
        {
            var nextCheckDateTime = DateTime.UtcNow.Add(delayBeforeNextCheck);

            var message = new StringBuilder();
            message.AppendLine($"******** PAUSING SERVICE: {loggingClassName} because of the following:");
            message.AppendLine($"> The Orchestrator service has not completed initialization.");
            message.AppendLine($"The {loggingClassName} will check again at {nextCheckDateTime} (UTC) and will resume operation if the Orchestrator service has completed initialization at that time.");

            logger.Info(message.ToString());
        }

        /// <inheritdoc/>
        public void LogWaitForPrerequisiteServicesServicePause(string loggingClassName, string processorsNeverRunStatement, string processorsNotRunningStatement, string processorsWithNoDataProcessedStatement, TimeSpan delayBeforeNextCheck)
        {
            var nextCheckDateTime = DateTime.UtcNow.Add(delayBeforeNextCheck);

            var message = new StringBuilder();
            message.AppendLine($"******** PAUSING SERVICE: {loggingClassName} because of the following:");
            if (processorsNeverRunStatement != "")
            {
                message.AppendLine($"> {processorsNeverRunStatement}");
            }
            if (processorsNotRunningStatement != "")
            {
                message.AppendLine($"> {processorsNotRunningStatement}");
            }
            if (processorsWithNoDataProcessedStatement != "")
            {
                message.AppendLine($"> {processorsWithNoDataProcessedStatement}");
            }
            message.AppendLine($"Please ensure that all prerequisite processors are running. The {loggingClassName} will check again at {nextCheckDateTime} (UTC) and will resume operation if all prerequisite processors are running at that time.");

            logger.Info(message.ToString());
        }

        /// <inheritdoc/>
        public void LogWaitForPrerequisiteServicesServiceResumption(string loggingClassName)
        {
            logger.Info($"******** RESUMING SERVICE: {loggingClassName} now that all prerequisite processors are running.");
        }

        /// <inheritdoc/>
        public void LogWaitForPrerequisiteServiceToProcessEntitiesPause(string loggingClassName, string prerequisiteService, TimeSpan delayBeforeNextCheck)
        {
            var nextCheckDateTime = DateTime.UtcNow.Add(delayBeforeNextCheck);

            var message = new StringBuilder();
            message.AppendLine($"******** PAUSING SERVICE: {loggingClassName} because of the following:");
            message.AppendLine($"> Waiting for {prerequisiteService} to complete its next iteration of entity processing.");
            message.AppendLine($"The {loggingClassName} will check again at {nextCheckDateTime} (UTC) and will resume operation if the {prerequisiteService} service has processed entities between now and then.");

            logger.Info(message.ToString());
        }

        /// <inheritdoc/>
        public void LogWaitForPrerequisiteServiceToProcessEntitiesResumption(string loggingClassName, string prerequisiteService)
        {
            logger.Info($"******** RESUMING SERVICE: {loggingClassName} now that the {prerequisiteService} service has completed another iteration of entity processing.");
        }
    }
}
