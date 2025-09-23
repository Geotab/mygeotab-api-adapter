using System.Collections.Generic;

namespace MyGeotabAPIAdapter.Database.Enums
{
    /// <summary>
    /// A class to hold sentinel ID values used in the Adapter database to represent MyGeotab KnownIds.
    /// </summary>
    public static class AdapterDbSentinelIdsForMYGKnownIds
    {
        /// <summary>
        /// The Adapter database value that represents the MyGeotab KnownId "NoDeviceId".
        /// </summary>
        public const long NoDeviceId = -1;

        /// <summary>
        /// The Adapter database value that represents the MyGeotab KnownId "NoDriverId".
        /// </summary>
        public const long NoDriverId = -2;

        /// <summary>
        /// The Adapter database value that represents the MyGeotab KnownId "NoRuleId".
        /// </summary>
        public const long NoRuleId = -1;

        /// <summary>
        /// The Adapter database value that represents the MyGeotab KnownId "NoUserId".
        /// </summary>
        public const long NoUserId = -1;

        /// <summary>
        /// The Adapter database value that represents the MyGeotab KnownId "NoZoneId".
        /// </summary>
        public const long NoZoneId = -1;

        /// <summary>
        /// The Adapter database value that represents the MyGeotab KnownId "UnknownDriverId".
        /// </summary>
        public const long UnknownDriverId = -3;
    }
}
