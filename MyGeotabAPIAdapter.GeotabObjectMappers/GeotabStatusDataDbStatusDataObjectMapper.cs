using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="StatusData"/> and <see cref="DbStatusData"/> entities.
    /// </summary>
    public class GeotabStatusDataDbStatusDataObjectMapper : IGeotabStatusDataDbStatusDataObjectMapper
    {
        /// <inheritdoc/>
        public List<DbStatusData> CreateEntities(List<StatusData> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbStatusDatas = new List<DbStatusData>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbStatusData = CreateEntity(entity);
                dbStatusData.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbStatusDatas.Add(dbStatusData);
            }
            return dbStatusDatas;
        }

        /// <inheritdoc/>
        public DbStatusData CreateEntity(StatusData entityToMapTo)
        {
            Device statusDataDevice = entityToMapTo.Device;
            Diagnostic statusDataDiagnostic = entityToMapTo.Diagnostic;

            DbStatusData dbStatusData = new()
            {
                GeotabId = entityToMapTo.Id.ToString(),
                Data = entityToMapTo.Data,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DateTime = entityToMapTo.DateTime,
                DeviceId = statusDataDevice.Id.ToString(),
                DiagnosticId = statusDataDiagnostic.Id.ToString(),
                RecordCreationTimeUtc = DateTime.UtcNow
            };
            return dbStatusData;
        }
    }
}
