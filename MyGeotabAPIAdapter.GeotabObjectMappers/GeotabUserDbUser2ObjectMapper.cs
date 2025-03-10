﻿using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.MyGeotabAPI;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="User"/> and <see cref="DbUser2"/> entities.
    /// </summary>
    public class GeotabUserDbUser2ObjectMapper : IGeotabUserDbUser2ObjectMapper
    {
        readonly IDateTimeHelper dateTimeHelper;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IStringHelper stringHelper;

        public GeotabUserDbUser2ObjectMapper(IDateTimeHelper dateTimeHelper, IGeotabIdConverter geotabIdConverter, IStringHelper stringHelper)
        {
            this.dateTimeHelper = dateTimeHelper;
            this.geotabIdConverter = geotabIdConverter;
            this.stringHelper = stringHelper;
        }

        /// <inheritdoc/>
        public DbUser2 CreateEntity(User entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbUser2 dbUser2 = new()
            {
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                EmployeeNo = entityToMapTo.EmployeeNo,
                EntityStatus = (int)entityStatus,
                FirstName = entityToMapTo.FirstName,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToLong(entityToMapTo.Id),
                IsDriver = entityToMapTo.IsDriver ?? false,
                LastAccessDate = entityToMapTo.LastAccessDate,
                LastName = entityToMapTo.LastName,
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.HosRuleSet != null)
            {
                dbUser2.HosRuleSet = entityToMapTo.HosRuleSet.Value.ToString();
            }
            return dbUser2;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbUser2 entityToEvaluate, User entityToMapTo)
        {
            long entityToMapToId = geotabIdConverter.ToLong(entityToMapTo.Id);

            if (entityToEvaluate.id != entityToMapToId)
            {
                throw new ArgumentException($"Cannot compare {nameof(DbUser2)} '{entityToEvaluate.GeotabId} ({entityToEvaluate.id})' with {nameof(User)} '{entityToMapTo.Id} ({entityToMapToId})' because the IDs do not match.");
            }

            DateTime entityToEvaluateActiveFrom = entityToEvaluate.ActiveFrom.GetValueOrDefault();
            DateTime entityToEvaluateActiveFromUtc = entityToEvaluateActiveFrom.ToUniversalTime();
            DateTime entityToEvaluateActiveTo = entityToEvaluate.ActiveTo.GetValueOrDefault();
            DateTime entityToEvaluateActiveToUtc = entityToEvaluateActiveTo.ToUniversalTime();
            DateTime entityToEvaluateLastAccessDate = entityToEvaluate.LastAccessDate.GetValueOrDefault();
            DateTime entityToEvaluateLastAccessDateUtc = entityToEvaluateLastAccessDate.ToUniversalTime();
            DateTime entityToMapToActiveFrom = entityToMapTo.ActiveFrom.GetValueOrDefault();
            DateTime entityToMapToActiveTo = entityToMapTo.ActiveTo.GetValueOrDefault();
            DateTime entityToMapToLastAccessDate = entityToMapTo.LastAccessDate.GetValueOrDefault();

            // Rounding to milliseconds may occur at the database level, so round accordingly such that equality operation will work as expected.
            DateTime entityToEvaluateActiveFromRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateActiveFrom);
            DateTime entityToEvaluateActiveFromUtcRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateActiveFromUtc);
            DateTime entityToEvaluateActiveToRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateActiveTo);
            DateTime entityToEvaluateActiveToUtcRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateActiveToUtc);
            DateTime entityToEvaluateLastAccessDateRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateLastAccessDate);
            DateTime entityToEvaluateLastAccessDateUtcRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToEvaluateLastAccessDateUtc);
            DateTime entityToMapToActiveFromRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToMapToActiveFrom);
            DateTime entityToMapToActiveToRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToMapToActiveTo);
            DateTime entityToMapToLastAccessDateRoundedToMilliseconds = dateTimeHelper.RoundDateTimeToNearestMillisecond(entityToMapToLastAccessDate);

            if (entityToEvaluateActiveFromRoundedToMilliseconds != entityToMapToActiveFromRoundedToMilliseconds && entityToEvaluateActiveFromUtcRoundedToMilliseconds != entityToMapToActiveFromRoundedToMilliseconds)
            {
                return true;
            }
            if (entityToEvaluateActiveToRoundedToMilliseconds != entityToMapToActiveToRoundedToMilliseconds && entityToEvaluateActiveToUtcRoundedToMilliseconds != entityToMapToActiveToRoundedToMilliseconds)
            {
                return true;
            }
            if (entityToMapTo.IsDriver == null)
            {
                entityToMapTo.IsDriver = false;
            }
            if (stringHelper.AreEqual(entityToEvaluate.FirstName, entityToMapTo.FirstName) == false || entityToEvaluate.IsDriver != entityToMapTo.IsDriver || stringHelper.AreEqual(entityToEvaluate.LastName, entityToMapTo.LastName) == false || stringHelper.AreEqual(entityToEvaluate.Name, entityToMapTo.Name) == false)
            {
                return true;
            }
            if ((entityToEvaluate.EmployeeNo != entityToMapTo.EmployeeNo) && (entityToEvaluate.EmployeeNo != null && entityToMapTo.EmployeeNo != ""))
            {
                return true;
            }
            if ((entityToMapTo.HosRuleSet == null && entityToEvaluate.HosRuleSet != null) || (entityToMapTo.HosRuleSet != null && entityToEvaluate.HosRuleSet != entityToMapTo.HosRuleSet.Value.ToString()))
            {
                return true;
            }
            if ((entityToEvaluateLastAccessDateRoundedToMilliseconds != entityToMapToLastAccessDateRoundedToMilliseconds) && entityToEvaluateLastAccessDateUtcRoundedToMilliseconds != entityToMapToLastAccessDateRoundedToMilliseconds)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public DbUser2 UpdateEntity(DbUser2 entityToUpdate, User entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.Id.ToString())
            {
                 throw new ArgumentException($"Cannot update {nameof(DbUser2)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(User)} '{entityToMapTo.Id}' because the GeotabIds do not match.");
            }

            var updatedDbUser2 = CreateEntity(entityToMapTo);
            updatedDbUser2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbUser2;
        }
    }
}
