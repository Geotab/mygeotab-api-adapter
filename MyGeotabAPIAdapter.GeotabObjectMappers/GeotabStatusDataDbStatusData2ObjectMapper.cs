using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="StatusData"/> and <see cref="DbStatusData2"/> entities.
    /// </summary>
    public class GeotabStatusDataDbStatusData2ObjectMapper : IGeotabStatusDataDbStatusData2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabStatusDataDbStatusData2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public DbStatusData2 CreateEntity(StatusData entityToMapTo, long deviceId, long diagnosticId)
        {
            long id = geotabIdConverter.ToLong(entityToMapTo.Id);

            DbStatusData2 dbStatusData2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                Data = entityToMapTo.Data,
                DateTime = (DateTime)entityToMapTo.DateTime,
                DeviceId = deviceId,
                DiagnosticId = diagnosticId,
                GeotabId = entityToMapTo.Id.ToString(),
                id = id,
                RecordCreationTimeUtc = DateTime.UtcNow
            };
            return dbStatusData2;
        }
    }
}
