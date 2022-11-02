using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="LogRecord"/> and <see cref="DbLogRecord"/> entities.
    /// </summary>
    public class GeotabLogRecordDbLogRecordObjectMapper : IGeotabLogRecordDbLogRecordObjectMapper
    {
        /// <inheritdoc/>
        public List<DbLogRecord> CreateEntities(List<LogRecord> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbLogRecords = new List<DbLogRecord>();
            foreach (var entity in entitiesToMapTo)
            { 
                var dbLogRecord = CreateEntity(entity);
                dbLogRecord.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbLogRecords.Add(dbLogRecord);
            }
            return dbLogRecords;
        }

        /// <inheritdoc/>
        public DbLogRecord CreateEntity(LogRecord entityToMapTo)
        {
            DbLogRecord dbLogRecord = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime.GetValueOrDefault(),
                DeviceId = entityToMapTo.Device.Id.ToString(),
                GeotabId = entityToMapTo.Id.ToString(),
                Latitude = entityToMapTo.Latitude,
                Longitude = entityToMapTo.Longitude,
                RecordCreationTimeUtc = DateTime.UtcNow,
                Speed = entityToMapTo.Speed
            };
            return dbLogRecord;
        }
    }
}
