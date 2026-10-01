namespace GeotabDIGAdapter.Services
{
    /// <summary>
    /// Interface for the device lookup source backed by the DeviceSubscriptions table, which is populated by the subscription control plane.
    /// </summary>
    public interface IDeviceSubscriptionCache : IDeviceLookupContributor
    {
    }
}
