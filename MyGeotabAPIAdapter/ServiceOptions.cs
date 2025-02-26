namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that contains options for BackgroundService instances.
    /// </summary>
    public class ServiceOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the service should pause operation for database maintenance windows.
        /// </summary>
        public bool PauseForDatabaseMaintenance { get; set; }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string ServiceName { get; set; }
    }
}
