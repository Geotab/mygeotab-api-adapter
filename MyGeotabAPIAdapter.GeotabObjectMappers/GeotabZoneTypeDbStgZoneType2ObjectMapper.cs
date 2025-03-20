using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="ZoneType"/> and <see cref="DbStgZoneType2"/> entities.
    /// </summary>
    public class GeotabZoneTypeDbStgZoneType2ObjectMapper : IGeotabZoneTypeDbStgZoneType2ObjectMapper
    {

        public GeotabZoneTypeDbStgZoneType2ObjectMapper()
        {
        }

        /// <inheritdoc/>
        public List<DbStgZoneType2> CreateEntities(List<ZoneType> entitiesToMapTo)
        {
            var dbStgZoneType2s = new List<DbStgZoneType2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbStgZoneType2 = CreateEntity(entity);
                dbStgZoneType2s.Add(dbStgZoneType2);
            }
            return dbStgZoneType2s;
        }

        /// <inheritdoc/>
        public DbStgZoneType2 CreateEntity(ZoneType entityToMapTo)
        {
            DbStgZoneType2 dbZoneType2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                EntityStatus = (int)Common.DatabaseRecordStatus.Active, // Set to Active by default. Database logic will handle setting to inactive if necessary (i.e. for detected deletions on the MYG side).
                GeotabId = entityToMapTo.Id.ToString(),
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.Comment != null && entityToMapTo.Comment.Length > 0)
            {
                dbZoneType2.Comment = entityToMapTo.Comment;
            }
            return dbZoneType2;
        }
    }
}
