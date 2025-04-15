using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Condition"/> and <see cref="DbCondition"/> entities.
    /// </summary>
    public class GeotabConditionDbConditionObjectMapper : IGeotabConditionDbConditionObjectMapper
    {
        IList<DbCondition> dbConditions = new List<DbCondition>();

        /// <summary>
        /// Recursively creates a <see cref="DbCondition"/> based on the <paramref name="condition"/>, adds it to the <see cref="dbConditions"/> list and then calls itself (<see cref="CreateDbConditionsRecursively(Condition, Common.DatabaseRecordStatus)"/>) to add any child <see cref="DbCondition"/>s.
        /// </summary>
        /// <param name="condition">The <see cref="Condition"/> for which to create and associated <see cref="DbCondition"/>, add it to the <see cref="dbConditions"/> list and then call <see cref="CreateDbConditionsRecursively(Condition, Common.DatabaseRecordStatus)"/> to repeat recursively for any child <see cref="Condition"/>s of the <paramref name="condition"/>.</param>
        /// <param name="entityStatus">The status to apply to the <see cref="DbCondition"/>(s)</param>
        void CreateDbConditionsRecursively(Condition condition, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbCondition dbCondition = CreateEntity(condition, entityStatus);
            if (condition.Parent != null)
            {
                dbCondition.ParentId = condition.Parent.Id.ToString();
            }
            dbConditions.Add(dbCondition);

            if (condition.Children != null)
            {
                foreach (var childCondition in condition.Children)
                {
                    CreateDbConditionsRecursively(childCondition, entityStatus);
                }
            }
        }

        /// <inheritdoc/>
        public IList<DbCondition> CreateDbConditionEntitiesForRule(Rule rule, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        { 
            dbConditions = new List<DbCondition>();
            if (rule.Condition != null)
            { 
                CreateDbConditionsRecursively(rule.Condition, entityStatus);
            }
            return dbConditions;
        }

        /// <inheritdoc/>
        public DbCondition CreateEntity(Condition entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbCondition dbCondition = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                EntityStatus = (int)entityStatus,
                GeotabId = entityToMapTo.Id.ToString(),
                ParentId = String.Empty,
                RecordLastChangedUtc = DateTime.UtcNow,
                Value = entityToMapTo.Value
            };

            if (entityToMapTo.Rule != null)
            {
                dbCondition.RuleId = entityToMapTo.Rule.Id.ToString();
            }
            if (entityToMapTo.ConditionType != null)
            {
                dbCondition.ConditionType = entityToMapTo.ConditionType.ToString();
            }
            if (entityToMapTo.Device != null)
            {
                dbCondition.DeviceId = entityToMapTo.Device.Id.ToString();
            }
            if (entityToMapTo.Diagnostic != null)
            {
                dbCondition.DiagnosticId = entityToMapTo.Diagnostic.Id.ToString();
            }
            if (entityToMapTo.Driver != null)
            {
                dbCondition.DriverId = entityToMapTo.Driver.Id.ToString();
            }
            if (entityToMapTo.WorkTime != null)
            {
                dbCondition.WorkTimeId = entityToMapTo.WorkTime.Id.ToString();
            }
            if (entityToMapTo.Zone != null)
            {
                dbCondition.ZoneId = entityToMapTo.Zone.Id.ToString();
            }
            return dbCondition;
        }

        /// <summary>
        /// DO NOT USE THIS METHOD. <see cref="DbCondition"/>s should be deleted and re-created (when updating <see cref="DbRule"/>s).
        /// </summary>
        /// <returns><see cref="NotImplementedException"/></returns>
        public bool EntityRequiresUpdate(DbCondition entityToEvaluate, Condition entityToMapTo)
        {
            throw new NotImplementedException($"This method has not been implemented because {nameof(DbCondition)} objects should be deleted and re-created rather than being updated.");
        }

        /// <summary>
        /// DO NOT USE THIS METHOD. <see cref="DbCondition"/>s should be deleted and re-created (when updating <see cref="DbRule"/>s).
        /// </summary>
        /// <returns><see cref="NotImplementedException"/></returns>
        public DbCondition UpdateEntity(DbCondition entityToUpdate, Condition entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            throw new NotImplementedException($"This method has not been implemented because {nameof(DbCondition)} objects should be deleted and re-created rather than being updated.");
        }
    }
}
