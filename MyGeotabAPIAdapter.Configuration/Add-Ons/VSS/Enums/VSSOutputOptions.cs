namespace MyGeotabAPIAdapter.Configuration.Add_Ons.VSS
{
    /// <summary>
    /// VSS-related output options derived based on configuration settings:
    /// <list type="bullet">
    /// <item><see cref="AdapterRecordOnly"/ - Only the core adapter data (e.g. <see cref="DbLogRecord"/> or <see cref="DbStatusData"/> entities) should be output.></item>
    /// <item><see cref="AdapterRecordAndDbOVDSServerCommand"/ - Both the core adapter data (e.g. <see cref="DbLogRecord"/> or <see cref="DbStatusData"/> entities) and VSS data (i.e. <see cref="DbOVDSServerCommand"/> entities) should be output.></item>
    /// <item><see cref="DbOVDSServerCommandOnly"/ - Only the VSS data (i.e. <see cref="DbOVDSServerCommand"/> entities) should be output.></item>
    /// <item><see cref="None"/ - Neither the core adapter data (e.g. <see cref="DbLogRecord"/> or <see cref="DbStatusData"/> entities) nor VSS data (i.e. <see cref="DbOVDSServerCommand"/> entities) should be output. This option indicates a configuration issue leading to unnecessary data processing.></item>
    /// </list>
    /// </summary>
    public enum VSSOutputOptions { AdapterRecordOnly, AdapterRecordAndDbOVDSServerCommand, DbOVDSServerCommandOnly, None }
}
