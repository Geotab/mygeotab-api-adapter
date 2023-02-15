﻿using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="User"/> and <see cref="DbUser"/> entities.
    /// </summary>
    public class GeotabUserDbUserObjectMapper : IGeotabUserDbUserObjectMapper
    {
        readonly IDateTimeHelper dateTimeHelper;

        public GeotabUserDbUserObjectMapper(IDateTimeHelper dateTimeHelper)
        {
            this.dateTimeHelper = dateTimeHelper;
        }

        /// <inheritdoc/>
        public DbUser CreateEntity(User entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbUser dbUser = new()
            {
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                EmployeeNo = entityToMapTo.EmployeeNo,
                EntityStatus = (int)entityStatus,
                FirstName = entityToMapTo.FirstName,
                GeotabId = entityToMapTo.Id.ToString(),
                IsDriver = entityToMapTo.IsDriver ?? false,
                LastAccessDate = entityToMapTo.LastAccessDate,
                LastName = entityToMapTo.LastName,
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow
            };
            if (entityToMapTo.HosRuleSet != null)
            {
                dbUser.HosRuleSet = entityToMapTo.HosRuleSet.Value.ToString();
            }
            return dbUser;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbUser entityToEvaluate, User entityToMapTo)
        {
            if (entityToEvaluate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare {nameof(DbUser)} '{entityToEvaluate.id}' with {nameof(User)} '{entityToMapTo.Id}' because the IDs do not match.");
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
            if (entityToEvaluate.FirstName != entityToMapTo.FirstName || entityToEvaluate.IsDriver != entityToMapTo.IsDriver || entityToEvaluate.LastName != entityToMapTo.LastName || entityToEvaluate.Name != entityToMapTo.Name)
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
        public DbUser UpdateEntity(DbUser entityToUpdate, User entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot update {nameof(DbUser)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(User)} '{entityToMapTo.Id}' because the GeotabIds do not match.");
            }

            var updatedDbUser = CreateEntity(entityToMapTo);

            // Update id since id is auto-generated by the database on insert and is therefor not set by the CreateEntity method.
            updatedDbUser.id = entityToUpdate.id;
            updatedDbUser.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbUser;
        }
    }
}
