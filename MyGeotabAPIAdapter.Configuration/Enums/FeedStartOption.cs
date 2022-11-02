namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// Alternate ways that polling via data feeds may be initiated. <see cref="FeedStartOption.CurrentTime"/> = feed to be started at the current point in time; <see cref="FeedStartOption.SpecificTime"/> = feed to be started as a specific point in time (in the past); <see cref="FeedStartOption.FeedVersion"/> = feed to be started using a specific version (i.e. to continue from where it left-off).
    /// </summary>
    public enum FeedStartOption { CurrentTime, SpecificTime, FeedVersion }
}
