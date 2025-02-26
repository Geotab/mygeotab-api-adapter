namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A list of possible reasons for the current <see cref="State"/> of the <see cref="IStateMachine"/>.
    /// </summary>
    public enum StateReason { ApplicationNotInitialized, AdapterDatabaseMaintenance, AdapterDatabaseNotAvailable, MyGeotabNotAvailable, NoReason }
}
