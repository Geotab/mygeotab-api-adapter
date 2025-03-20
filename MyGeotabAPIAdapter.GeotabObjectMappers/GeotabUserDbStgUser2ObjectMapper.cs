using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;
using System.Text;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="User"/> and <see cref="DbStgUser2"/> entities.
    /// </summary>
    public class GeotabUserDbStgUser2ObjectMapper : IGeotabUserDbStgUser2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabUserDbStgUser2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public List<DbStgUser2> CreateEntities(List<User> entitiesToMapTo)
        {
            var dbStgUser2s = new List<DbStgUser2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbStgUser2 = CreateEntity(entity);
                dbStgUser2s.Add(dbStgUser2);
            }
            return dbStgUser2s;
        }

        /// <inheritdoc/>
        public DbStgUser2 CreateEntity(User entityToMapTo)
        {
            DbStgUser2 dbStgUser2 = new()
            {
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                EmployeeNo = entityToMapTo.EmployeeNo,
                EntityStatus = (int)Common.DatabaseRecordStatus.Active, // Set to Active by default. Database logic will handle setting to inactive if necessary (i.e. for detected deletions on the MYG side).
                FirstName = entityToMapTo.FirstName,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToLong(entityToMapTo.Id),
                IsDriver = entityToMapTo.IsDriver ?? false,
                LastAccessDate = entityToMapTo.LastAccessDate,
                LastName = entityToMapTo.LastName,
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.CompanyGroups != null && entityToMapTo.CompanyGroups.Count > 0)
            {
                dbStgUser2.CompanyGroups = GetUserGroupsJSON(entityToMapTo.CompanyGroups);
            }
            if (entityToMapTo.HosRuleSet != null)
            {
                dbStgUser2.HosRuleSet = entityToMapTo.HosRuleSet.Value.ToString();
            }
            return dbStgUser2;
        }

        /// <inheritdoc/>
        public string GetUserGroupsJSON(IList<Group> userGroups)
        {
            bool userGroupsArrayHasItems = false;
            var userGroupsIds = new StringBuilder();
            userGroupsIds.Append('[');

            for (int i = 0; i < userGroups.Count; i++)
            {
                if (userGroupsArrayHasItems == true)
                {
                    userGroupsIds.Append(',');
                }
                string userGroupsId = userGroups[i].Id.ToString();
                userGroupsIds.Append($"{{\"id\":\"{userGroupsId}\"}}");
                userGroupsArrayHasItems = true;
            }
            userGroupsIds.Append(']');
            return userGroupsIds.ToString();
        }
    }
}
