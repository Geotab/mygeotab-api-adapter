﻿using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using Newtonsoft.Json;
using System.Text;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Group"/> and <see cref="DbGroup"/> entities.
    /// </summary>
    public class GeotabGroupDbGroupObjectMapper : IGeotabGroupDbGroupObjectMapper
    {
        readonly IDateTimeHelper dateTimeHelper;
        readonly IStringHelper stringHelper;

        public GeotabGroupDbGroupObjectMapper(IDateTimeHelper dateTimeHelper, IStringHelper stringHelper)
        {
            this.dateTimeHelper = dateTimeHelper;
            this.stringHelper = stringHelper;
        }

        /// <inheritdoc/>
        public DbGroup CreateEntity(Group entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbGroup dbGroup = new()
            {
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                Comments = entityToMapTo.Comments,
                EntityStatus = (int)entityStatus,
                GeotabId = entityToMapTo.Id.ToString(),
                Name = entityToMapTo.Name,
                Reference = entityToMapTo.Reference,              
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.Children != null && entityToMapTo.Children.Count != 0)
            {
                dbGroup.Children = GetGroupChildrenIdsJSON(entityToMapTo.Children);
            }
            string groupColor = JsonConvert.SerializeObject(entityToMapTo.Color).ToLowerInvariant();
            dbGroup.Color = groupColor;

            return dbGroup;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbGroup entityToEvaluate, Group entityToMapTo)
        {
            if (entityToEvaluate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare {nameof(DbGroup)} '{entityToEvaluate.id}' with {nameof(Group)} '{entityToMapTo.Id}' because the IDs do not match.");
            }

            string groupChildrenIds = GetGroupChildrenIdsJSON(entityToMapTo.Children);
            if (entityToMapTo.Children.Count == 0) 
            {
                groupChildrenIds = null;
            }
            if (stringHelper.AreEqual(entityToEvaluate.Children, groupChildrenIds) == false)
            {
                return true;
            }

            if (stringHelper.AreEqual(entityToEvaluate.Color, JsonConvert.SerializeObject(entityToMapTo.Color).ToLowerInvariant()) == false)
            {
                return true;
            }

            if (stringHelper.AreEqual(entityToEvaluate.Comments, entityToMapTo.Comments) == false)
            {
                return true;
            }
            if (stringHelper.AreEqual(entityToEvaluate.Name, entityToMapTo.Name) == false)
            {
                return true;
            }
            if (stringHelper.AreEqual(entityToEvaluate.Reference, entityToMapTo.Reference) == false)
            {
                return true;
            }
            return false;
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

        /// <inheritdoc/>
        public DbGroup UpdateEntity(DbGroup entityToUpdate, Group entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot update {nameof(DbGroup)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(Group)} '{entityToMapTo.Id}' because the GeotabIds do not match.");
            }

            var updatedDbGroup = CreateEntity(entityToMapTo);

            // Update id since id is auto-generated by the database on insert and is therefor not set by the CreateEntity method.
            updatedDbGroup.id = entityToUpdate.id;
            updatedDbGroup.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbGroup;
        }
    }
}
