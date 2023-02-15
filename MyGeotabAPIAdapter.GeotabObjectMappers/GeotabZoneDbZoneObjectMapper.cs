using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using Newtonsoft.Json;
using System.Text;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Zone"/> and <see cref="DbZone"/> entities.
    /// </summary>
    public class GeotabZoneDbZoneObjectMapper : IGeotabZoneDbZoneObjectMapper
    {
        readonly IDateTimeHelper dateTimeHelper;

        public GeotabZoneDbZoneObjectMapper(IDateTimeHelper dateTimeHelper)
        { 
            this.dateTimeHelper = dateTimeHelper;
        }

        /// <inheritdoc/>
        public DbZone CreateEntity(Zone entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbZone dbZone = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                Displayed = entityToMapTo.Displayed ?? false,
                EntityStatus = (int)entityStatus,
                GeotabId = entityToMapTo.Id.ToString(),
                MustIdentifyStops = entityToMapTo.MustIdentifyStops ?? false,
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow,
                Version = entityToMapTo.Version ?? null
            };
            if (entityToMapTo.ActiveFrom != null)
            {
                dbZone.ActiveFrom = entityToMapTo.ActiveFrom;
            }
            if (entityToMapTo.ActiveTo != null)
            {
                dbZone.ActiveTo = entityToMapTo.ActiveTo;
            }
            if (entityToMapTo.CentroidLatitude != null)
            {
                dbZone.CentroidLatitude = entityToMapTo.CentroidLatitude;
            }
            if (entityToMapTo.CentroidLongitude != null)
            {
                dbZone.CentroidLongitude = entityToMapTo.CentroidLongitude;
            }
            if (entityToMapTo.Comment != null && entityToMapTo.Comment.Length > 0)
            {
                dbZone.Comment = entityToMapTo.Comment;
            }
            if (entityToMapTo.ExternalReference != null && entityToMapTo.ExternalReference.Length > 0)
            {
                dbZone.ExternalReference = entityToMapTo.ExternalReference;
            }
            string zonePoints = JsonConvert.SerializeObject(entityToMapTo.Points);
            dbZone.Points = zonePoints;
            dbZone.ZoneTypeIds = GetZoneTypeIdsJSON(entityToMapTo.ZoneTypes);
            return dbZone;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbZone entityToEvaluate, Zone entityToMapTo)
        {
            if (entityToEvaluate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare {nameof(DbZone)} '{entityToEvaluate.id}' with {nameof(Zone)} '{entityToMapTo.Id}' because the IDs do not match.");
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
            if (entityToMapTo.Displayed == null)
            {
                entityToMapTo.Displayed = false;
            }
            if (entityToMapTo.MustIdentifyStops == null)
            {
                entityToMapTo.MustIdentifyStops = false;
            }
            if (entityToEvaluate.CentroidLatitude != entityToMapTo.CentroidLatitude || entityToEvaluate.CentroidLongitude != entityToMapTo.CentroidLongitude || entityToEvaluate.Displayed != entityToMapTo.Displayed || entityToEvaluate.MustIdentifyStops != entityToMapTo.MustIdentifyStops || entityToEvaluate.Version != entityToMapTo.Version)
            {
                return true;
            }
            if ((entityToEvaluate.Comment != entityToMapTo.Comment) && (entityToEvaluate.Comment != null && entityToMapTo.Comment != ""))
            {
                return true;
            }
            if ((entityToEvaluate.ExternalReference != entityToMapTo.ExternalReference) && (entityToEvaluate.ExternalReference != null && entityToMapTo.ExternalReference != ""))
            {
                return true;
            }
            if ((entityToEvaluate.Name != entityToMapTo.Name) && (entityToEvaluate.Name != null && entityToMapTo.Name != ""))
            {
                return true;
            }
            string zonePoints = JsonConvert.SerializeObject(entityToMapTo.Points);
            if (entityToEvaluate.Points != zonePoints)
            {
                return true;
            }
            string zoneZoneTypeIds = GetZoneTypeIdsJSON(entityToMapTo.ZoneTypes);
            if (entityToEvaluate.ZoneTypeIds != zoneZoneTypeIds)
            {
                return true;
            }
            return false;
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

        /// <inheritdoc/>
        public DbZone UpdateEntity(DbZone entityToUpdate, Zone entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot update {nameof(DbZone)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(Zone)} '{entityToMapTo.Id}' because the GeotabIds do not match.");
            }

            var updatedDbZone = CreateEntity(entityToMapTo);

            // Update id since id is auto-generated by the database on insert and is therefor not set by the CreateEntity method.
            updatedDbZone.id = entityToUpdate.id;
            updatedDbZone.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbZone;
        }
    }
}
