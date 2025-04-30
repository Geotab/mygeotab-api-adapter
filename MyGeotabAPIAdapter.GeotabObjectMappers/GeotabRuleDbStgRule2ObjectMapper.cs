using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.MyGeotabAPI;
using Newtonsoft.Json;
using System.Text;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Rule"/> and <see cref="DbStgRule2"/> entities.
    /// </summary>
    public class GeotabRuleDbStgRule2ObjectMapper : IGeotabRuleDbStgRule2ObjectMapper
    {
        readonly IDateTimeHelper dateTimeHelper;
        readonly IStringHelper stringHelper;

        public GeotabRuleDbStgRule2ObjectMapper(IDateTimeHelper dateTimeHelper, IStringHelper stringHelper)
        {
            this.dateTimeHelper = dateTimeHelper;
            this.stringHelper = stringHelper;
        }

        /// <inheritdoc/>
        public List<DbStgRule2> CreateEntities(List<Rule> entitiesToMapTo)
        {
            var dbStgRule2s = new List<DbStgRule2>();
            foreach (var entity in entitiesToMapTo)
															   
            {
                var dbStgRule2 = CreateEntity(entity);
                dbStgRule2s.Add(dbStgRule2);
            }
            return dbStgRule2s;
        }
		
        /// <inheritdoc/>
        public DbStgRule2 CreateEntity(Rule entityToMapTo)
        {
            string ruleComment = entityToMapTo.Comment;

            if (ruleComment != null && ruleComment.Length == 0)
            {
                ruleComment = String.Empty;
            }

            DbStgRule2 dbStgRule2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                BaseType = entityToMapTo.BaseType.ToString(),
                Comment = ruleComment,
				EntityStatus = (int)Common.DatabaseRecordStatus.Active, // Set to Active by default. Database logic will handle setting to inactive if necessary (i.e. for detected deletions on the MYG side).
                GeotabId = entityToMapTo.Id.ToString(),
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow,
                Version = entityToMapTo.Version
            };

            if (entityToMapTo.Condition != null)
            {
                var settings = new JsonSerializerSettings
                {
                    // Handle general loops elsewhere.
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,

                    // Explicitly tell JsonConvert how to handle the Id type.
                    Converters = new List<JsonConverter> { new GeotabIdJsonConverter() },

                    Formatting = Formatting.Indented
                };
                string ruleConditionJson = JsonConvert.SerializeObject(entityToMapTo.Condition, settings);
                dbStgRule2.Condition = ruleConditionJson;
            }

            if (entityToMapTo.Groups != null && entityToMapTo.Groups.Count > 0)
            {
                dbStgRule2.Groups = GetRuleGroupsJSON(entityToMapTo.Groups);
            }

            return dbStgRule2;
        }

        /// <inheritdoc/>
        public string GetRuleGroupsJSON(IList<Group> ruleGroups)
        {
            bool ruleGroupsArrayHasItems = false;
            var ruleGroupsIds = new StringBuilder();
            ruleGroupsIds.Append('[');

            for (int i = 0; i < ruleGroups.Count; i++)
            {
                if (ruleGroupsArrayHasItems == true)
                {
                    ruleGroupsIds.Append(',');
                }
                string ruleGroupsId = ruleGroups[i].Id.ToString();
                ruleGroupsIds.Append($"{{\"id\":\"{ruleGroupsId}\"}}");
                ruleGroupsArrayHasItems = true;
            }
            ruleGroupsIds.Append(']');
            return ruleGroupsIds.ToString();
        }
    }
}
