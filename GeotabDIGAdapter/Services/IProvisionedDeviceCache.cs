namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// Interface for the device lookup source backed by the ProvisionedDevices table, which is populated by the device provisioning workflow.
    /// </summary>
    public interface IProvisionedDeviceCache : IDeviceLookupContributor
    {
    }
}
