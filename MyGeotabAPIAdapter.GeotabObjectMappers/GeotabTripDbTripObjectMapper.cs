using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Trip"/> and <see cref="DbTrip"/> entities.
    /// </summary>
    public class GeotabTripDbTripObjectMapper : IGeotabTripDbTripObjectMapper
    {
        /// <inheritdoc/>
        public List<DbTrip> CreateEntities(List<Trip> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbTrips = new List<DbTrip>();
            foreach (var entity in entitiesToMapTo)
            {
                if (entity.Device != null)
                {
                    var dbTrip = CreateEntity(entity);
                    dbTrip.RecordCreationTimeUtc = recordCreationTimeUtc;
                    dbTrips.Add(dbTrip);
                }
            }
            return dbTrips;
        }

        /// <inheritdoc/>
        public DbTrip CreateEntity(Trip entityToMapTo)
        {
            DbTrip dbTrip = new()
            {
                AfterHoursDistance = entityToMapTo.AfterHoursDistance,
                AfterHoursDrivingDuration = entityToMapTo.AfterHoursDrivingDuration,
                AfterHoursStopDuration = entityToMapTo.AfterHoursStopDuration,
                AverageSpeed = entityToMapTo.AverageSpeed,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DeviceId = entityToMapTo.Device.Id.ToString(),
                Distance = entityToMapTo.Distance,
                DriverId = entityToMapTo.Driver.Id.ToString(),
                DrivingDuration = entityToMapTo.DrivingDuration,
                GeotabId = entityToMapTo.Id.ToString(),
                IdlingDuration = entityToMapTo.IdlingDuration,
                MaximumSpeed = entityToMapTo.MaximumSpeed,
                NextTripStart = entityToMapTo.NextTripStart,
                RecordCreationTimeUtc = DateTime.UtcNow,
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
                WorkStopDuration = entityToMapTo.WorkStopDuration
            };
            if (entityToMapTo.AfterHoursEnd != null)
            {
                dbTrip.AfterHoursEnd = entityToMapTo.AfterHoursEnd;
            }
            if (entityToMapTo.AfterHoursStart != null)
            {
                dbTrip.AfterHoursStart = entityToMapTo.AfterHoursStart;
            }
            if (entityToMapTo.StopPoint != null)
            {
                dbTrip.StopPointX = entityToMapTo.StopPoint.X;
                dbTrip.StopPointY = entityToMapTo.StopPoint.Y;
            }
            return dbTrip;
        }
    }
}
