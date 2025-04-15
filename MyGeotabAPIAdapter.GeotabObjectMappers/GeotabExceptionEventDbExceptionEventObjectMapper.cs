using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="ExceptionEvent"/> and <see cref="DbExceptionEvent"/> entities.
    /// </summary>
    public class GeotabExceptionEventDbExceptionEventObjectMapper : IGeotabExceptionEventDbExceptionEventObjectMapper
    {
        /// <inheritdoc/>
        public List<DbExceptionEvent> CreateEntities(List<ExceptionEvent> entitiesToMapTo)
        {
            DateTime recordLastChangedUtc = DateTime.UtcNow;
            var dbExceptionEvents = new List<DbExceptionEvent>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbExceptionEvent = CreateEntity(entity);
                dbExceptionEvent.RecordLastChangedUtc = recordLastChangedUtc;
                dbExceptionEvents.Add(dbExceptionEvent);
            }
            return dbExceptionEvents;
        }

        /// <inheritdoc/>
        public DbExceptionEvent CreateEntity(ExceptionEvent entityToMapTo)
        {
            Device device = entityToMapTo.Device;
            Driver driver = entityToMapTo.Driver;
            Rule rule = entityToMapTo.Rule;
            ExceptionEventState state = entityToMapTo.State;

            DbExceptionEvent dbExceptionEvent = new()
            {
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DeviceId = device.Id.ToString(),
                Distance = entityToMapTo.Distance,
                DriverId = driver.Id.ToString(),
                Duration = entityToMapTo.Duration,
                GeotabId = entityToMapTo.Id.ToString(),
                LastModifiedDateTime = (DateTime)entityToMapTo.LastModifiedDateTime,
                RecordLastChangedUtc = DateTime.UtcNow,
                RuleId = rule.Id.ToString(),
                State = state.Key,
                Version = entityToMapTo.Version
            };
            return dbExceptionEvent;
        }
    }
}
