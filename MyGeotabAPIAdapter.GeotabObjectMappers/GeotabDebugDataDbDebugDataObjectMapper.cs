using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DebugData"/> and <see cref="DbDebugData"/> entities.
    /// </summary>
    public class GeotabDebugDataDbDebugDataObjectMapper : IGeotabDebugDataDbDebugDataObjectMapper
    {
        /// <inheritdoc/>
        public List<DbDebugData> CreateEntities(List<DebugData> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbDebugDatas = new List<DbDebugData>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbDebugData = CreateEntity(entity);
                dbDebugData.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbDebugDatas.Add(dbDebugData);
            }
            return dbDebugDatas;
        }

        /// <inheritdoc/>
        public DbDebugData CreateEntity(DebugData entityToMapTo)
        {
            var debugDataDevice = entityToMapTo.Device;
            var debugDataDriver = entityToMapTo.Driver;
            var debugReason = entityToMapTo.DebugReason;
            DbDebugData dbDebugData = new()
            {
                GeotabId = entityToMapTo.Id.ToString(),
                Data = Convert.ToBase64String(entityToMapTo.Data),
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime.GetValueOrDefault(),
                RecordCreationTimeUtc = DateTime.UtcNow
            };
            if (debugDataDevice != null && debugDataDevice.Id != null)
            {
                dbDebugData.DeviceId = debugDataDevice.Id.ToString();
            }
            if (debugDataDriver != null && debugDataDriver.Id != null)
            { 
                dbDebugData.DriverId = debugDataDriver.Id.ToString();
            }
            if (debugReason != null)
            {
                dbDebugData.DebugReasonId = (long?)debugReason;
                dbDebugData.DebugReasonName = debugReason.ToString();
            }
            return dbDebugData;
        }
    }
}
