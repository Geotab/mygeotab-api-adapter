#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A class that contains the results of a check to determine whether all prerequisite processors are running.
    /// </summary>
    public class PrerequisiteProcessorOperationCheckResult
    {
        readonly TimeSpan recommendedDelayBeforeNextCheck = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Indicates whether all prerequisite processors are running.
        /// </summary>
        public bool AllPrerequisiteProcessorsRunning { get; }

        /// <summary>
        /// A list of any prerequisite processors that have never been run.
        /// </summary>
        public List<DataOptimizerProcessor> ProcessorsNeverRun { get; }

        /// <summary>
        /// A statement listing any prerequisite processors that have never been run. Intended for use as part of a log message.
        /// </summary>
        public string ProcessorsNeverRunStatement { get; }

        /// <summary>
        /// A list of any prerequisite processors that are not currently running.
        /// </summary>
        public List<DataOptimizerProcessor> ProcessorsNotRunning { get; }

        /// <summary>
        /// A statement listing any prerequisite processors that are not currently running. Intended for use as part of a log message.
        /// </summary>
        public string ProcessorsNotRunningStatement { get; }

        /// <summary>
        /// A list of any prerequisite processors that may have been run or be currently running, but which have not processed any data.
        /// </summary>
        public List<DataOptimizerProcessor> ProcessorsWithNoDataProcessed { get; }

        /// <summary>
        /// A statement listing any prerequisite processors that may have been run or be currently running, but which have not processed any data. Intended for use as part of a log message.
        /// </summary>
        public string ProcessorsWithNoDataProcessedStatement { get; }

        /// <summary>
        /// The recommended time to wait, after checking whether all prerequisite processors are running, before checking again.
        /// </summary>
        public TimeSpan RecommendedDelayBeforeNextCheck { get => recommendedDelayBeforeNextCheck; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrerequisiteProcessorOperationCheckResult"/> class.
        /// </summary>
        /// <param name="allPrerequisiteProcessorsRunning">Indicates whether all prerequisite processors are running.</param>
        /// <param name="processorsNeverRun">A list of any prerequisite processors that have never been run.</param>
        /// <param name="processorsNotRunning">A list of any prerequisite processors that are not currently running.</param>
        /// <param name="processorsWithNoDataProcessed">A list of any prerequisite processors that may have been run or be currently running, but which have not yet processed any data.</param>
        public PrerequisiteProcessorOperationCheckResult(bool allPrerequisiteProcessorsRunning, List<DataOptimizerProcessor>? processorsNeverRun, List<DataOptimizerProcessor>? processorsNotRunning, List<DataOptimizerProcessor>? processorsWithNoDataProcessed)
        {
            AllPrerequisiteProcessorsRunning = allPrerequisiteProcessorsRunning;
            
            // Processors never run statement:
            if (processorsNeverRun == null)
            {
                ProcessorsNeverRun = new List<DataOptimizerProcessor>();
            }
            else
            {
                ProcessorsNeverRun = processorsNeverRun;
            }

            if (processorsNeverRun != null && processorsNeverRun.Any())
            {
                if (processorsNeverRun.Count == 1)
                {
                    ProcessorsNeverRunStatement = $"The prerequisite {processorsNeverRun[0]} has never been run.";
                }
                else
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.Append("The prerequisite ");
                    for (int i = 0; i < processorsNeverRun.Count; i++)
                    {
                        if (i > 0 && i < processorsNeverRun.Count - 1)
                        {
                            stringBuilder.Append($", ");
                        }
                        else if (i > 0 && i >= processorsNeverRun.Count - 1)
                        {
                            stringBuilder.Append($" and ");
                        }
                        stringBuilder.Append($"{processorsNeverRun[i]}");
                    }
                    stringBuilder.Append(" have never been run.");
                    ProcessorsNeverRunStatement = stringBuilder.ToString();
                }
            }
            else
            {
                ProcessorsNeverRunStatement = "";
            }

            // Processors not running statement:
            if (processorsNotRunning == null)
            {
                ProcessorsNotRunning = new List<DataOptimizerProcessor>();
            }
            else
            {
                ProcessorsNotRunning = processorsNotRunning;
            }

            if (processorsNotRunning != null && processorsNotRunning.Any())
            {
                if (processorsNotRunning.Count == 1)
                {
                    ProcessorsNotRunningStatement = $"The prerequisite {processorsNotRunning[0]} is not currently running.";
                }
                else
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.Append("The prerequisite ");
                    for (int i = 0; i < processorsNotRunning.Count; i++)
                    {
                        if (i > 0 && i < processorsNotRunning.Count - 1)
                        {
                            stringBuilder.Append($", ");
                        }
                        else if (i > 0 && i >= processorsNotRunning.Count - 1)
                        {
                            stringBuilder.Append($" and ");
                        }
                        stringBuilder.Append($"{processorsNotRunning[i]}");
                    }
                    stringBuilder.Append(" are not currently running.");
                    ProcessorsNotRunningStatement = stringBuilder.ToString();
                }
            }
            else
            {
                ProcessorsNotRunningStatement = "";
            }

            // Processors that have not yet processed any data statement:
            if (processorsWithNoDataProcessed == null)
            {
                ProcessorsWithNoDataProcessed = new List<DataOptimizerProcessor>();
            }
            else
            {
                ProcessorsWithNoDataProcessed = processorsWithNoDataProcessed;
            }

            if (processorsWithNoDataProcessed != null && processorsWithNoDataProcessed.Any())
            {
                if (processorsWithNoDataProcessed.Count == 1)
                {
                    ProcessorsWithNoDataProcessedStatement = $"The prerequisite {processorsWithNoDataProcessed[0]} has not yet processed any data.";
                }
                else
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.Append("The prerequisite ");
                    for (int i = 0; i < processorsWithNoDataProcessed.Count; i++)
                    {
                        if (i > 0 && i < processorsWithNoDataProcessed.Count - 1)
                        {
                            stringBuilder.Append($", ");
                        }
                        else if (i > 0 && i >= processorsWithNoDataProcessed.Count - 1)
                        {
                            stringBuilder.Append($" and ");
                        }
                        stringBuilder.Append($"{processorsWithNoDataProcessed[i]}");
                    }
                    stringBuilder.Append(" have not yet processed any data.");
                    ProcessorsWithNoDataProcessedStatement = stringBuilder.ToString();
                }
            }
            else
            {
                ProcessorsWithNoDataProcessedStatement = "";
            }
        }
    }
}
