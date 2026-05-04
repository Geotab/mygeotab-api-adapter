namespace GeotabDIGAdapter
{
    /// <summary>
    /// A class that holds the results of a check to determine whether prerequisite services are running.
    /// </summary>
    public class PrerequisiteServiceOperationCheckResult
    {
        /// <summary>
        /// Indicates whether all prerequisite services are running.
        /// </summary>
        public bool AllPrerequisiteServicesRunning { get; set; }

        /// <summary>
        /// The recommended delay before checking again.
        /// </summary>
        public TimeSpan RecommendedDelayBeforeNextCheck { get; set; }

        /// <summary>
        /// A list of services that have never been run.
        /// </summary>
        public List<DIGAdapterService> ServicesNeverRun { get; set; } = [];

        /// <summary>
        /// A statement describing services that have never been run.
        /// </summary>
        public string ServicesNeverRunStatement { get; set; } = string.Empty;

        /// <summary>
        /// A list of services that are not currently running.
        /// </summary>
        public List<DIGAdapterService> ServicesNotRunning { get; set; } = [];

        /// <summary>
        /// A statement describing services that are not currently running.
        /// </summary>
        public string ServicesNotRunningStatement { get; set; } = string.Empty;

        /// <summary>
        /// A list of services that have not processed any data.
        /// </summary>
        public List<DIGAdapterService> ServicesWithNoDataProcessed { get; set; } = [];

        /// <summary>
        /// A statement describing services that have not processed any data.
        /// </summary>
        public string ServicesWithNoDataProcessedStatement { get; set; } = string.Empty;
    }
}
