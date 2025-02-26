#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that contains the results of a check to determine whether all prerequisite services are running.
    /// </summary>
    internal class PrerequisiteServiceOperationCheckResult
    {
        readonly TimeSpan recommendedDelayBeforeNextCheck = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Indicates whether all prerequisite services are running.
        /// </summary>
        public bool AllPrerequisiteServicesRunning { get; }

        /// <summary>
        /// A list of any prerequisite services that have never been run.
        /// </summary>
        public List<AdapterService> ServicesNeverRun { get; }

        /// <summary>
        /// A statement listing any prerequisite services that have never been run. Intended for use as part of a log message.
        /// </summary>
        public string ServicesNeverRunStatement { get; }

        /// <summary>
        /// A list of any prerequisite services that are not currently running.
        /// </summary>
        public List<AdapterService> ServicesNotRunning { get; }

        /// <summary>
        /// A statement listing any prerequisite services that are not currently running. Intended for use as part of a log message.
        /// </summary>
        public string ServicesNotRunningStatement { get; }

        /// <summary>
        /// A list of any prerequisite services that may have been run or be currently running, but which have not processed any data.
        /// </summary>
        public List<AdapterService> ServicesWithNoDataProcessed { get; }

        /// <summary>
        /// A statement listing any prerequisite services that may have been run or be currently running, but which have not processed any data. Intended for use as part of a log message.
        /// </summary>
        public string ServicesWithNoDataProcessedStatement { get; }

        /// <summary>
        /// The recommended time to wait, after checking whether all prerequisite services are running, before checking again.
        /// </summary>
        public TimeSpan RecommendedDelayBeforeNextCheck { get => recommendedDelayBeforeNextCheck; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrerequisiteServiceOperationCheckResult"/> class.
        /// </summary>
        /// <param name="allPrerequisiteServicesRunning">Indicates whether all prerequisite services are running.</param>
        /// <param name="servicesNeverRun">A list of any prerequisite services that have never been run.</param>
        /// <param name="servicesNotRunning">A list of any prerequisite services that are not currently running.</param>
        /// <param name="servicesWithNoDataProcessed">A list of any prerequisite services that may have been run or be currently running, but which have not yet processed any data.</param>
        public PrerequisiteServiceOperationCheckResult(bool allPrerequisiteServicesRunning, List<AdapterService>? servicesNeverRun, List<AdapterService>? servicesNotRunning, List<AdapterService>? servicesWithNoDataProcessed)
        {
            AllPrerequisiteServicesRunning = allPrerequisiteServicesRunning;

            // Services never run statement:
            if (servicesNeverRun == null)
            {
                ServicesNeverRun = new List<AdapterService>();
            }
            else
            {
                ServicesNeverRun = servicesNeverRun;
            }

            if (servicesNeverRun != null && servicesNeverRun.Any())
            {
                if (servicesNeverRun.Count == 1)
                {
                    ServicesNeverRunStatement = $"The prerequisite {servicesNeverRun[0]} has never been run.";
                }
                else
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.Append("The prerequisite ");
                    for (int i = 0; i < servicesNeverRun.Count; i++)
                    {
                        if (i > 0 && i < servicesNeverRun.Count - 1)
                        {
                            stringBuilder.Append($", ");
                        }
                        else if (i > 0 && i >= servicesNeverRun.Count - 1)
                        {
                            stringBuilder.Append($" and ");
                        }
                        stringBuilder.Append($"{servicesNeverRun[i]}");
                    }
                    stringBuilder.Append(" have never been run.");
                    ServicesNeverRunStatement = stringBuilder.ToString();
                }
            }
            else
            {
                ServicesNeverRunStatement = "";
            }

            // Services not running statement:
            if (servicesNotRunning == null)
            {
                ServicesNotRunning = new List<AdapterService>();
            }
            else
            {
                ServicesNotRunning = servicesNotRunning;
            }

            if (servicesNotRunning != null && servicesNotRunning.Any())
            {
                if (servicesNotRunning.Count == 1)
                {
                    ServicesNotRunningStatement = $"The prerequisite {servicesNotRunning[0]} is not currently running or has not yet completed initialization.";
                }
                else
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.Append("The prerequisite ");
                    for (int i = 0; i < servicesNotRunning.Count; i++)
                    {
                        if (i > 0 && i < servicesNotRunning.Count - 1)
                        {
                            stringBuilder.Append($", ");
                        }
                        else if (i > 0 && i >= servicesNotRunning.Count - 1)
                        {
                            stringBuilder.Append($" and ");
                        }
                        stringBuilder.Append($"{servicesNotRunning[i]}");
                    }
                    stringBuilder.Append(" are not currently running.");
                    ServicesNotRunningStatement = stringBuilder.ToString();
                }
            }
            else
            {
                ServicesNotRunningStatement = "";
            }

            // Services that have not yet processed any data statement:
            if (servicesWithNoDataProcessed == null)
            {
                ServicesWithNoDataProcessed = new List<AdapterService>();
            }
            else
            {
                ServicesWithNoDataProcessed = servicesWithNoDataProcessed;
            }

            if (servicesWithNoDataProcessed != null && servicesWithNoDataProcessed.Any())
            {
                if (servicesWithNoDataProcessed.Count == 1)
                {
                    ServicesWithNoDataProcessedStatement = $"The prerequisite {servicesWithNoDataProcessed[0]} has not yet processed any data.";
                }
                else
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.Append("The prerequisite ");
                    for (int i = 0; i < servicesWithNoDataProcessed.Count; i++)
                    {
                        if (i > 0 && i < servicesWithNoDataProcessed.Count - 1)
                        {
                            stringBuilder.Append($", ");
                        }
                        else if (i > 0 && i >= servicesWithNoDataProcessed.Count - 1)
                        {
                            stringBuilder.Append($" and ");
                        }
                        stringBuilder.Append($"{servicesWithNoDataProcessed[i]}");
                    }
                    stringBuilder.Append(" have not yet processed any data.");
                    ServicesWithNoDataProcessedStatement = stringBuilder.ToString();
                }
            }
            else
            {
                ServicesWithNoDataProcessedStatement = "";
            }
        }
    }
}
