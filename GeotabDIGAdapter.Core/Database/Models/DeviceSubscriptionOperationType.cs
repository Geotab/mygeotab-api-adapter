namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// The values of the <c>OperationType</c> column on <c>gda.Q_DeviceSubscriptions</c> and <c>gda.stg_DeviceSubscriptions</c>, which say what a subscription instruction is asking for.
    /// <para>
    /// The numbering is deliberate: <see cref="Terminate"/> is 0 and <see cref="Subscribe"/> is 1, so that it runs in the same direction as <see cref="DbGdaDeviceSubscription.SubscriptionStatusTerminated"/> and <see cref="DbGdaDeviceSubscription.SubscriptionStatusActive"/>, and as every other status column in the gda schema, where 0 is the inactive or not-yet-done state. An instruction and the state it produces therefore carry the same number, and there is nothing to invert when reading one against the other.
    /// </para>
    /// <para>
    /// Both values are legal, so a wrong one does not fail - it does the opposite of what was intended, with no error and a success in the log. A Terminate that resolves to an active subscription stops telemetry for that device and consumes termination-guard budget. Use these constants rather than a literal, and never assume an unset value is safe: the default of a <see cref="byte"/> is 0, which is Terminate.
    /// </para>
    /// <para>
    /// Defined once, on purpose. The queue model and the staging model each carried their own copy of this pair before it was consolidated here, which is how the two could have drifted apart without anything noticing.
    /// </para>
    /// </summary>
    public static class DeviceSubscriptionOperationType
    {
        /// <summary>
        /// An instruction that terminates a device's subscription. Matches <see cref="DbGdaDeviceSubscription.SubscriptionStatusTerminated"/>.
        /// </summary>
        public const byte Terminate = 0;

        /// <summary>
        /// An instruction that subscribes a device, or that updates the subscription of a device that is already subscribed. Matches <see cref="DbGdaDeviceSubscription.SubscriptionStatusActive"/>.
        /// </summary>
        public const byte Subscribe = 1;
    }
}
