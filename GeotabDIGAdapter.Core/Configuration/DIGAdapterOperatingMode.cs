namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// Which device control plane a deployment intends to use.
    /// <para>
    /// This is a declaration of intent, not a switch: it never enables or disables a service. Its only job is to
    /// let the application reject a configuration that contradicts itself at startup, naming the offending key,
    /// rather than starting into a shape that fails later and silently. Deriving the enable flags from it was
    /// considered and rejected, because it would make the per-service startup log lines untrue.
    /// </para>
    /// </summary>
    public enum DIGAdapterOperatingMode
    {
        /// <summary>
        /// Devices are provisioned through MyAdmin by this adapter. The original workflow, but NOT the default:
        /// since 5.2.0.0 the default is <see cref="OemSubscription"/>, and a deployment running the provisioning
        /// workflow must declare this mode explicitly.
        /// </summary>
        DeviceProvisioning,

        /// <summary>
        /// Devices are subscribed and terminated by an external party, and communicated to the operator. No
        /// MyAdmin account is needed or used.
        /// </summary>
        OemSubscription,

        /// <summary>
        /// Both control planes are in use at once. Where the two disagree about a serial number, neither entry is
        /// transmitted for and the collision is logged.
        /// </summary>
        Both
    }
}
