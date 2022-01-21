#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A class that contains the results of a check to determine whether all prerequisite processors are running.
    /// </summary>
    public class PrerequisiteProcessorOperationCheckResult
    {
        readonly TimeSpan recommendedDelayBeforeNextCheck = TimeSpan.FromMinutes(5);

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
        /// The recommended time to wait, after checking whether all prerequisite processors are running, before checking again.
        /// </summary>
        public TimeSpan RecommendedDelayBeforeNextCheck { get => recommendedDelayBeforeNextCheck; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrerequisiteProcessorOperationCheckResult"/> class.
        /// </summary>
        /// <param name="allPrerequisiteProcessorsRunning">Indicates whether all prerequisite processors are running.</param>
        /// <param name="processorsNeverRun">A list of any prerequisite processors that have never been run.</param>
        /// <param name="processorsNotRunning">A list of any prerequisite processors that are not currently running.</param>
        public PrerequisiteProcessorOperationCheckResult(bool allPrerequisiteProcessorsRunning, List<DataOptimizerProcessor>? processorsNeverRun, List<DataOptimizerProcessor>? processorsNotRunning)
        {
            AllPrerequisiteProcessorsRunning = allPrerequisiteProcessorsRunning;
            
            if (processorsNeverRun == null)
            {
                ProcessorsNeverRun = new List<DataOptimizerProcessor>();
            }
            else
            {
                ProcessorsNeverRun = processorsNeverRun;
            }

            if (processorsNeverRun != null && processorsNeverRun.Count > 0)
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

            if (processorsNotRunning == null)
            {
                ProcessorsNotRunning = new List<DataOptimizerProcessor>();
            }
            else
            {
                ProcessorsNotRunning = processorsNotRunning;
            }

            if (processorsNotRunning != null && processorsNotRunning.Count > 0)
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
        }
    }
}
