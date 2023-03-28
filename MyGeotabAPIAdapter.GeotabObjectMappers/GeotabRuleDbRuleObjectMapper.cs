using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Rule"/> and <see cref="DbRule"/> entities.
    /// </summary>
    public class GeotabRuleDbRuleObjectMapper : IGeotabRuleDbRuleObjectMapper
    {
        readonly IDateTimeHelper dateTimeHelper;
        readonly IStringHelper stringHelper;

        public GeotabRuleDbRuleObjectMapper(IDateTimeHelper dateTimeHelper, IStringHelper stringHelper)
        { 
            this.dateTimeHelper = dateTimeHelper;
            this.stringHelper = stringHelper;
        }

        /// <inheritdoc/>
        public DbRule CreateEntity(Rule entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            string ruleComment = entityToMapTo.Comment;

            if (ruleComment != null && ruleComment.Length == 0)
            {
                ruleComment = String.Empty;
            }

            DbRule dbRule = new()
            {
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                BaseType = entityToMapTo.BaseType.ToString(),
                Comment = ruleComment,
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                EntityStatus = (int)entityStatus,
                GeotabId = entityToMapTo.Id.ToString(),
                Name = entityToMapTo.Name.ToString(),
                RecordLastChangedUtc = DateTime.UtcNow,
                Version = entityToMapTo.Version
            };
            return dbRule;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbRule entityToEvaluate, Rule entityToMapTo)
        {
            if (entityToEvaluate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare {nameof(DbRule)} '{entityToEvaluate.id}' with {nameof(Rule)} '{entityToMapTo.Id}' because the IDs do not match.");
            }

            DateTime entityToEvaluateActiveFrom = entityToEvaluate.ActiveFrom.GetValueOrDefault();
            DateTime entityToEvaluateActiveFromUtc = entityToEvaluateActiveFrom.ToUniversalTime();
            DateTime entityToEvaluateActiveTo = entityToEvaluate.ActiveTo.GetValueOrDefault();
            DateTime entityToEvaluateActiveToUtc = entityToEvaluateActiveTo.ToUniversalTime();
            DateTime entityToMapToActiveFrom = entityToMapTo.ActiveFrom.GetValueOrDefault();
            DateTime entityToMapToActiveTo = entityToMapTo.ActiveTo.GetValueOrDefault();

            // Rounding to milliseconds may occur at the database level, so round accordingly such that equality operation will work as expected.
            DateTime entityToEvaluateActiveFromRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateActiveFrom);
            DateTime entityToEvaluateActiveFromUtcRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateActiveFromUtc);
            DateTime entityToEvaluateActiveToRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateActiveTo);
            DateTime entityToEvaluateActiveToUtcRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateActiveToUtc);
            DateTime entityToMapToActiveFromRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToMapToActiveFrom);
            DateTime entityToMapToActiveToRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToMapToActiveTo);

            if (entityToEvaluateActiveFromRoundedToMilliseconds != entityToMapToActiveFromRoundedToMilliseconds && entityToEvaluateActiveFromUtcRoundedToMilliseconds != entityToMapToActiveFromRoundedToMilliseconds)
            {
                return true;
            }
            if (entityToEvaluateActiveToRoundedToMilliseconds != entityToMapToActiveToRoundedToMilliseconds && entityToEvaluateActiveToUtcRoundedToMilliseconds != entityToMapToActiveToRoundedToMilliseconds)
            {
                return true;
            }

            string entityToMapToBaseType = entityToMapTo.BaseType.ToString();
            if (entityToEvaluate.BaseType != entityToMapToBaseType || stringHelper.AreEqual(entityToEvaluate.Comment, entityToMapTo.Comment) == false || stringHelper.AreEqual(entityToEvaluate.Name, entityToMapTo.Name) == false || entityToEvaluate.Version != entityToMapTo.Version)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public DbRule UpdateEntity(DbRule entityToUpdate, Rule entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot update {nameof(DbRule)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(Rule)} '{entityToMapTo.Id}' because the GeotabIds do not match.");
            }

            var updatedDbRule = CreateEntity(entityToMapTo);

            // Update id since id is auto-generated by the database on insert and is therefor not set by the CreateEntity method.
            updatedDbRule.id = entityToUpdate.id;
            updatedDbRule.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbRule;
        }
    }
}
