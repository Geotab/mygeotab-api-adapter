#nullable enable
namespace MyGeotabAPIAdapter.DataOptimizer.EstimatorsAndInterpolators
{
    /// <summary>
    /// A class that contains the results of a longitude/latitude interpolation request.
    /// </summary>
    public class LongitudeLatitudeInterpolationResult
    {
        /// <summary>
        /// The <see href="https://en.wikipedia.org/wiki/Bearing_(angle)">bearing</see> between the origin and destination points used in the subject longitude/latitude interpolation request.
        /// </summary>
        public double? Bearing { get; }

        /// <summary>
        /// A string representation of the compass direction (e.g. "N", "SE", "WSW", etc.) associated with the <see cref="Bearing"/>. Based on the cardinal directions on a <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see>.
        /// </summary>
        public string? Direction { get; }

        /// <summary>
        /// The interpolated latitude coordinate value. If <see cref="Success"/> is <c>false</c>, the value will be zero.
        /// </summary>
        public double? Latitude { get; }

        /// <summary>
        /// The interpolated longitude coordinate value. If <see cref="Success"/> is <c>false</c>, the value will be zero.
        /// </summary>
        public double? Longitude { get; }

        /// <summary>
        /// The reason for the <see cref="Success"/> value.
        /// </summary>
        public LongitudeLatitudeInterpolationResultReason Reason { get; }

        /// <summary>
        /// Indicates whether interpolation was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LongitudeLatitudeInterpolationResult"/> class.
        /// </summary>
        /// <param name="success">The <see cref="Success"/> value.</param>
        /// <param name="reason">The <see cref="Reason"/> value.</param>
        /// <param name="longitude">The <see cref="Longitude"/> value.</param>
        /// <param name="latitde">The <see cref="Latitude"/> value.</param>
        /// <param name="bearing">The <see cref="Bearing"/> value.</param>
        /// <param name="direction">The <see cref="Direction"/> value.</param>
        public LongitudeLatitudeInterpolationResult(bool success, LongitudeLatitudeInterpolationResultReason reason = LongitudeLatitudeInterpolationResultReason.None, double? longitude = null, double? latitde = null, double? bearing = null, string? direction = null)
        {
            Success = success;
            Reason = reason;
            Longitude = longitude;
            Latitude = latitde;
            Bearing = bearing;
            Direction = direction;
        }
    }

    /// <summary>
    /// The list of possible values for <see cref="LongitudeLatitudeInterpolationResult.Reason"/>.
    /// </summary>
    public enum LongitudeLatitudeInterpolationResultReason 
    {
        None = 0,
        LagLeadDbLogRecordTInfoNotFound = 1,
        LeadDateTimeLessThanLagDateTime = 2,
        TargetDateTimeGreaterThanLeadDateTime = 3,
        TargetDateTimeLessThanLagDateTime = 4,
        TargetEntityDateTimeBelowMinDbLogRecordTDateTime = 5,
        TargetEntityDateTimeBelowMinDbLogRecordTDateTimeForDevice = 6
    }
}
