using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Trip"/> and <see cref="DbStgTrip2"/> entities.
    /// </summary>
    public class GeotabTripDbStgTrip2ObjectMapper : IGeotabTripDbStgTrip2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;
     
        public GeotabTripDbStgTrip2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbStgTrip2 CreateEntity(Trip entityToMapTo, long deviceId, long? driverId)
        {
            DbStgTrip2 dbStgTrip2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                AfterHoursDistance = entityToMapTo.AfterHoursDistance,
                AfterHoursDrivingDuration = entityToMapTo.AfterHoursDrivingDuration,
                AfterHoursEnd = entityToMapTo.AfterHoursEnd,
                AfterHoursStart = entityToMapTo.AfterHoursStart,
                AfterHoursStopDuration = entityToMapTo.AfterHoursStopDuration,
                AverageSpeed = entityToMapTo.AverageSpeed,
                DeletedDateTime = entityToMapTo.DeletedDateTime,
                DeviceId = deviceId,
                Distance = entityToMapTo.Distance,
                DriverId = driverId,
                DrivingDuration = entityToMapTo.DrivingDuration,
                EntityStatus = (int)Common.DatabaseRecordStatus.Active, // Set to Active by default. Database logic will handle setting to inactive if necessary (i.e. for detected deletions on the MYG side).
                GeotabId = entityToMapTo.Id.ToString(),
                IdlingDuration = entityToMapTo.IdlingDuration,
                MaximumSpeed = entityToMapTo.MaximumSpeed,
                NextTripStart = entityToMapTo.NextTripStart,
                SpeedRange1 = entityToMapTo.SpeedRange1,
                SpeedRange1Duration = entityToMapTo.SpeedRange1Duration,
                SpeedRange2 = entityToMapTo.SpeedRange2,
                SpeedRange2Duration = entityToMapTo.SpeedRange2Duration,
                SpeedRange3 = entityToMapTo.SpeedRange3,
                SpeedRange3Duration = entityToMapTo.SpeedRange3Duration,
                Start = entityToMapTo.Start,
                Stop = entityToMapTo.Stop,
                StopDuration = entityToMapTo.StopDuration,
                WorkDistance = entityToMapTo.WorkDistance,
                WorkDrivingDuration = entityToMapTo.WorkDrivingDuration,
                WorkStopDuration = entityToMapTo.WorkStopDuration,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.StopPoint != null)
            {
                dbStgTrip2.StopPointX = entityToMapTo.StopPoint.X;
                dbStgTrip2.StopPointY = entityToMapTo.StopPoint.Y;
            }
            return dbStgTrip2;
        }
    }
}
