namespace MyGeotabAPIAdapter.DataOptimizer.EstimatorsAndInterpolators
{
    /// <summary>
    /// The list of possible values for <see cref="DriverEstimationResult.Reason"/>.
    /// </summary>
    public enum DriverIdEstimationResultReason
    {
        None = 0,
        LagDbDriverChangeTNotFound = 3,
        TargetEntityDateTimeBelowMinDbDriverChangeTDateTime = 1,
        TargetEntityDateTimeBelowMinDbDriverChangeTDateTimeForDevice = 2
    }
}
