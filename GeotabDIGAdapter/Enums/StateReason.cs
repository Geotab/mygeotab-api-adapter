namespace GeotabDIGAdapter
{
    /// <summary>
    /// A list of possible reasons for the current <see cref="State"/> of the <see cref="IStateMachine"/>.
    /// </summary>
    public enum StateReason { ApplicationNotInitialized, AdapterDatabaseMaintenance, AdapterDatabaseNotAvailable, DIGNotAvailable, MyAdminNotAvailable, MyGeotabNotAvailable, NoReason }
}
