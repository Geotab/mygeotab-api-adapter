using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using BinaryData = Geotab.Checkmate.ObjectModel.BinaryData;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="BinaryData"/> and <see cref="DbBinaryData"/> entities.
    /// </summary>
    public class GeotabBinaryDataDbBinaryDataObjectMapper : IGeotabBinaryDataDbBinaryDataObjectMapper
    {
        /// <inheritdoc/>
        public List<DbBinaryData> CreateEntities(List<Geotab.Checkmate.ObjectModel.BinaryData> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbBinaryDatas = new List<DbBinaryData>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbBinaryData = CreateEntity(entity);
                dbBinaryData.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbBinaryDatas.Add(dbBinaryData);
            }
            return dbBinaryDatas;
        }

        /// <inheritdoc/>
        public DbBinaryData CreateEntity(BinaryData entityToMapTo)
        {
            Device binaryDataDevice = entityToMapTo.Device;
            DbBinaryData dbBinaryData = new()
            {
                GeotabId = entityToMapTo.Id.ToString(),
                ControllerId = entityToMapTo.Controller.Id.ToString(),
                Data = Convert.ToBase64String(entityToMapTo.Data),
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime.GetValueOrDefault(),
                RecordCreationTimeUtc = DateTime.UtcNow
            };
            if (binaryDataDevice.Id != null)
            {
                dbBinaryData.DeviceId = binaryDataDevice.Id.ToString();
            }
            if (entityToMapTo.BinaryType != null)
            {
                dbBinaryData.BinaryType = entityToMapTo.BinaryType.ToString();
            }
            if (entityToMapTo.Version != null)
            {
                dbBinaryData.Version = entityToMapTo.Version.ToString();
            }
            return dbBinaryData;
        }
    }
}
