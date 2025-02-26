#nullable enable
using System;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// Interface to allow DbOServiceTracking and DbOServiceTracking2 to be used interchangeably.
    /// </summary>
    public interface IDbOServiceTracking : IDbEntity, IIdCacheableDbEntity
    {
        static DateTime DefaultDateTime { get; }
        bool EntitiesHaveBeenProcessed { get; }
        string ServiceId { get; set; }
        string? AdapterVersion { get; set; }
        string? AdapterMachineName { get; set; }
        DateTime? EntitiesLastProcessedUtc { get; set; }
        long? LastProcessedFeedVersion { get; set; }
        DateTime RecordLastChangedUtc { get; set; }
    }
}
