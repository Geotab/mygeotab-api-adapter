using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using Newtonsoft.Json;
using System.Text;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Group"/> and <see cref="DbStgGroup2"/> entities.
    /// </summary>
    public class GeotabGroupDbStgGroup2ObjectMapper : IGeotabGroupDbStgGroup2ObjectMapper
    {

        public GeotabGroupDbStgGroup2ObjectMapper()
        {
        }

        /// <inheritdoc/>
        public List<DbStgGroup2> CreateEntities(List<Group> entitiesToMapTo)
        {
            var dbStgGroup2s = new List<DbStgGroup2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbStgGroup2 = CreateEntity(entity);
                dbStgGroup2s.Add(dbStgGroup2);
            }
            return dbStgGroup2s;
        }

        /// <inheritdoc/>
        public DbStgGroup2 CreateEntity(Group entityToMapTo)
        {
            DbStgGroup2 dbStgGroup2 = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                Comments = entityToMapTo.Comments,
                EntityStatus = (int)Common.DatabaseRecordStatus.Active, // Set to Active by default. Database logic will handle setting to inactive if necessary (i.e. for detected deletions on the MYG side).
                GeotabId = entityToMapTo.Id.ToString(),
                Name = entityToMapTo.Name,
                Reference = entityToMapTo.Reference,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.Children != null && entityToMapTo.Children.Count != 0)
            {
                dbStgGroup2.Children = GetGroupChildrenIdsJSON(entityToMapTo.Children);
            }
            string groupColor = JsonConvert.SerializeObject(entityToMapTo.Color).ToLowerInvariant();
            dbStgGroup2.Color = groupColor;

            return dbStgGroup2;
        }

        /// <inheritdoc/>
        public string GetGroupChildrenIdsJSON(IList<Group> groupChildren)
        {
            bool groupChildrenIdsArrayHasItems = false;
            var groupChildrenIds = new StringBuilder();
            groupChildrenIds.Append('[');

            for (int i = 0; i < groupChildren.Count; i++)
            {
                if (groupChildrenIdsArrayHasItems == true)
                {
                    groupChildrenIds.Append(',');
                }
                string groupChildId = groupChildren[i].Id.ToString();
                groupChildrenIds.Append($"{{\"id\":\"{groupChildId}\"}}");
                groupChildrenIdsArrayHasItems = true;
            }
            groupChildrenIds.Append(']');
            return groupChildrenIds.ToString();
        }
    }
}
