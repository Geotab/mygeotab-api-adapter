using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Audit"/> and <see cref="DbAuditLog2"/> entities.
    /// </summary>
    public class GeotabAuditDbAuditLog2ObjectMapper : IGeotabAuditDbAuditLog2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabAuditDbAuditLog2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public List<DbAuditLog2> CreateEntities(List<Audit> entitiesToMapTo)
        {
            DateTime recordCreationTimeUtc = DateTime.UtcNow;
            var dbAuditLog2s = new List<DbAuditLog2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbAuditLog2 = CreateEntity(entity);
                dbAuditLog2.RecordCreationTimeUtc = recordCreationTimeUtc;
                dbAuditLog2s.Add(dbAuditLog2);
            }
            return dbAuditLog2s;
        }

        /// <inheritdoc/>
        public DbAuditLog2 CreateEntity(Audit entityToMapTo)
        {
            Guid id = geotabIdConverter.ToGuid(entityToMapTo.Id);

            DbAuditLog2 dbAuditLog2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                Comment = entityToMapTo.Comment,
                DateTime = entityToMapTo.DateTime.GetValueOrDefault(),
                GeotabId = entityToMapTo.Id.ToString(),
                id = id,
                Name = entityToMapTo.Name,
                RecordCreationTimeUtc = DateTime.UtcNow,
                UserName = entityToMapTo.UserName,
                Version = entityToMapTo.Version
            };
            return dbAuditLog2;
        }
    }
}
