using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;
using Newtonsoft.Json;
using System.Text;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Zone"/> and <see cref="DbStgZone2"/> entities.
    /// </summary>
    public class GeotabZoneDbStgZone2ObjectMapper : IGeotabZoneDbStgZone2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabZoneDbStgZone2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public List<DbStgZone2> CreateEntities(List<Zone> entitiesToMapTo)
        {
            var dbStgZone2s = new List<DbStgZone2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbStgZone2 = CreateEntity(entity);
                dbStgZone2s.Add(dbStgZone2);
            }
            return dbStgZone2s;
        }

        /// <inheritdoc/>
        public DbStgZone2 CreateEntity(Zone entityToMapTo)
        {
            DbStgZone2 dbStgZone2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                Displayed = entityToMapTo.Displayed ?? false,
                EntityStatus = (int)Common.DatabaseRecordStatus.Active, // Set to Active by default. Database logic will handle setting to inactive if necessary (i.e. for detected deletions on the MYG side).
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToLong(entityToMapTo.Id),
                MustIdentifyStops = entityToMapTo.MustIdentifyStops ?? false,
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow,
                Version = entityToMapTo.Version ?? null
            };
            if (entityToMapTo.ActiveFrom != null)
            {
                dbStgZone2.ActiveFrom = entityToMapTo.ActiveFrom;
            }
            if (entityToMapTo.ActiveTo != null)
            {
                dbStgZone2.ActiveTo = entityToMapTo.ActiveTo;
            }
            if (entityToMapTo.CentroidLatitude != null)
            {
                dbStgZone2.CentroidLatitude = entityToMapTo.CentroidLatitude;
            }
            if (entityToMapTo.CentroidLongitude != null)
            {
                dbStgZone2.CentroidLongitude = entityToMapTo.CentroidLongitude;
            }
            if (entityToMapTo.Comment != null && entityToMapTo.Comment.Length > 0)
            {
                dbStgZone2.Comment = entityToMapTo.Comment;
            }
            if (entityToMapTo.ExternalReference != null && entityToMapTo.ExternalReference.Length > 0)
            {
                dbStgZone2.ExternalReference = entityToMapTo.ExternalReference;
            }
            if (entityToMapTo.Groups != null && entityToMapTo.Groups.Count > 0)
            {
                dbStgZone2.Groups = GetZoneGroupsJSON(entityToMapTo.Groups);
            }
            string zonePoints = JsonConvert.SerializeObject(entityToMapTo.Points);
            dbStgZone2.Points = zonePoints;
            dbStgZone2.ZoneTypeIds = GetZoneTypeIdsJSON(entityToMapTo.ZoneTypes);
            return dbStgZone2;
        }

        /// <inheritdoc/>
        public string GetZoneGroupsJSON(IList<Group> zoneGroups)
        {
            bool zoneGroupsArrayHasItems = false;
            var zoneGroupsIds = new StringBuilder();
            zoneGroupsIds.Append('[');

            for (int i = 0; i < zoneGroups.Count; i++)
            {
                if (zoneGroupsArrayHasItems == true)
                {
                    zoneGroupsIds.Append(',');
                }
                string zoneGroupsId = zoneGroups[i].Id.ToString();
                zoneGroupsIds.Append($"{{\"id\":\"{zoneGroupsId}\"}}");
                zoneGroupsArrayHasItems = true;
            }
            zoneGroupsIds.Append(']');
            return zoneGroupsIds.ToString();
        }

        /// <inheritdoc/>
        public string GetZoneTypeIdsJSON(IList<ZoneType> zoneTypes)
        {
            bool zoneTypeIdsArrayHasItems = false;
            var zoneTypeIds = new StringBuilder();
            zoneTypeIds.Append('[');
            foreach (var zoneType in zoneTypes)
            {
                if (zoneTypeIdsArrayHasItems == true)
                {
                    zoneTypeIds.Append(',');
                }
                zoneTypeIds.Append($"{{\"Id\":\"{zoneType.Id}\"}}");
                zoneTypeIdsArrayHasItems = true;
            }
            zoneTypeIds.Append(']');
            return zoneTypeIds.ToString();
        }
    }
}

