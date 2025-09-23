using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database.Enums;
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
            Rule? rule = entityToMapTo.Rule;

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
                State = state.Key,
                Version = entityToMapTo.Version,
                RecordLastChangedUtc = DateTime.UtcNow
            };

            // Handle null Rule and Rule.Id = NoRule (need to use our sentinel value so that the associated seninel record id will be retrieved in the database merge procedure/function):
            if (rule != null)
            {
                if (rule.GetType() == typeof(NoRule))
                {
                    dbStgExceptionEvent2.RuleGeotabId = nameof(AdapterDbSentinelIdsForMYGKnownIds.NoRuleId);
                }
                else if (rule.Id != null)
                {
                    dbStgExceptionEvent2.RuleGeotabId = rule.Id.ToString();
                }
            }

            return dbStgExceptionEvent2;
        }
    }
}
