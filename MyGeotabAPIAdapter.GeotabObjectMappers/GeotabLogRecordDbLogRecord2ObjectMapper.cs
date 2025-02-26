using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="LogRecord"/> and <see cref="DbLogRecord2"/> entities.
    /// </summary>
    public class GeotabLogRecordDbLogRecord2ObjectMapper : IGeotabLogRecordDbLogRecord2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabLogRecordDbLogRecord2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        { 
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public List<DbLogRecord2> CreateEntities(List<LogRecord> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbLogRecord2s = new List<DbLogRecord2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbLogRecord2 = CreateEntity(entity);
                dbLogRecord2.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbLogRecord2s.Add(dbLogRecord2);
            }
            return dbLogRecord2s;
        }

        /// <inheritdoc/>
        public DbLogRecord2 CreateEntity(LogRecord entityToMapTo)
        {
            long id = geotabIdConverter.ToLong(entityToMapTo.Id);
            long deviceId = geotabIdConverter.ToLong(entityToMapTo.Device.Id);

            DbLogRecord2 dbLogRecord2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime.GetValueOrDefault(),
                DeviceId = deviceId,
                GeotabId = entityToMapTo.Id.ToString(),
                id = id,
                Latitude = (double)entityToMapTo.Latitude,
                Longitude = (double)entityToMapTo.Longitude,
                RecordCreationTimeUtc = DateTime.UtcNow,
                Speed = (float)entityToMapTo.Speed
            };
            return dbLogRecord2;
        }
    }
}
