using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;
using BinaryData = Geotab.Checkmate.ObjectModel.BinaryData;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="BinaryData"/> and <see cref="DbBinaryData2"/> entities.
    /// </summary>
    public class GeotabBinaryDataDbBinaryData2ObjectMapper : IGeotabBinaryDataDbBinaryData2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabBinaryDataDbBinaryData2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbBinaryData2 CreateEntity(BinaryData entityToMapTo, long deviceId)
        {
            DbBinaryData2 dbBinaryData2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                ControllerId = entityToMapTo.Controller.Id.ToString(),
                Data = entityToMapTo.Data,
                DateTime = entityToMapTo.DateTime.GetValueOrDefault(),
                DeviceId = deviceId,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToGuid(entityToMapTo.Id),
                Version = entityToMapTo.Version,
                RecordCreationTimeUtc = DateTime.UtcNow
            };
            if (entityToMapTo.BinaryType != null)
            {
                dbBinaryData2.BinaryType = entityToMapTo.BinaryType.ToString();
            }
            return dbBinaryData2;
        }
    }
}
