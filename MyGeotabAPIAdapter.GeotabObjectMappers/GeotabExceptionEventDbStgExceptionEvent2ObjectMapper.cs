using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="ExceptionEvent"/> and <see cref="DbStgExceptionEvent2"/> entities.
    /// </summary>
    public class GeotabExceptionEventDbStgExceptionEvent2ObjectMapper : IGeotabExceptionEventDbStgExceptionEvent2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabExceptionEventDbStgExceptionEvent2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }
        /// <inheritdoc/>
        public DbStgExceptionEvent2 CreateEntity(ExceptionEvent entityToMapTo, long deviceId, long? driverId)
        {
            ExceptionEventState state = entityToMapTo.State;
            Rule rule = entityToMapTo.Rule;

            DbStgExceptionEvent2 dbStgExceptionEvent2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                ActiveFrom = (DateTime)entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                DeviceId = deviceId,
                Distance = entityToMapTo.Distance,
                DriverId = driverId,
                DurationTicks = entityToMapTo.Duration?.Ticks,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToGuid(entityToMapTo.Id),
                RuleGeotabId = rule.Id.ToString(),
                State = state.Key,
                Version = entityToMapTo.Version,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            return dbStgExceptionEvent2;
        }
    }
}
