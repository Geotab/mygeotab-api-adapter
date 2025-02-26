using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.MyGeotabAPI;
using Newtonsoft.Json;
using System.Text;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Zone"/> and <see cref="DbZone2"/> entities.
    /// </summary>
    public class GeotabZoneDbZone2ObjectMapper : IGeotabZoneDbZone2ObjectMapper
    {
        readonly IDateTimeHelper dateTimeHelper;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IStringHelper stringHelper;

        public GeotabZoneDbZone2ObjectMapper(IDateTimeHelper dateTimeHelper, IGeotabIdConverter geotabIdConverter, IStringHelper stringHelper)
        { 
            this.dateTimeHelper = dateTimeHelper;
            this.geotabIdConverter = geotabIdConverter;
            this.stringHelper = stringHelper;
        }

        /// <inheritdoc/>
        public DbZone2 CreateEntity(Zone entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            DbZone2 dbZone2 = new()
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                Displayed = entityToMapTo.Displayed ?? false,
                EntityStatus = (int)entityStatus,
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToLong(entityToMapTo.Id),
                MustIdentifyStops = entityToMapTo.MustIdentifyStops ?? false,
                Name = entityToMapTo.Name,
                RecordLastChangedUtc = DateTime.UtcNow,
                Version = entityToMapTo.Version ?? null
            };
            if (entityToMapTo.ActiveFrom != null)
            {
                dbZone2.ActiveFrom = entityToMapTo.ActiveFrom;
            }
            if (entityToMapTo.ActiveTo != null)
            {
                dbZone2.ActiveTo = entityToMapTo.ActiveTo;
            }
            if (entityToMapTo.CentroidLatitude != null)
            {
                dbZone2.CentroidLatitude = entityToMapTo.CentroidLatitude;
            }
            if (entityToMapTo.CentroidLongitude != null)
            {
                dbZone2.CentroidLongitude = entityToMapTo.CentroidLongitude;
            }
            if (entityToMapTo.Comment != null && entityToMapTo.Comment.Length > 0)
            {
                dbZone2.Comment = entityToMapTo.Comment;
            }
            if (entityToMapTo.ExternalReference != null && entityToMapTo.ExternalReference.Length > 0)
            {
                dbZone2.ExternalReference = entityToMapTo.ExternalReference;
            }
            string zonePoints = JsonConvert.SerializeObject(entityToMapTo.Points);
            dbZone2.Points = zonePoints;
            dbZone2.ZoneTypeIds = GetZoneTypeIdsJSON(entityToMapTo.ZoneTypes);
            return dbZone2;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbZone2 entityToEvaluate, Zone entityToMapTo)
        {
            long entityToMapToId = geotabIdConverter.ToLong(entityToMapTo.Id);

            if (entityToEvaluate.id != entityToMapToId)
            {
                throw new ArgumentException($"Cannot compare {nameof(DbZone2)} '{entityToEvaluate.GeotabId} {entityToEvaluate.id}' with {nameof(Zone)} '{entityToMapTo.Id} ({entityToMapToId})' because the IDs do not match.");
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
            if (stringHelper.AreEqual(entityToEvaluate.Comment, entityToMapTo.Comment) == false)
            {
                return true;
            }
            if (stringHelper.AreEqual(entityToEvaluate.ExternalReference, entityToMapTo.ExternalReference) == false)
            {
                return true;
            }
            if (stringHelper.AreEqual(entityToEvaluate.Name, entityToMapTo.Name) == false)
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
        public DbZone2 UpdateEntity(DbZone2 entityToUpdate, Zone entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot update {nameof(DbZone2)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(Zone)} '{entityToMapTo.Id}' because the GeotabIds do not match.");
            }

            var updatedDbZone2 = CreateEntity(entityToMapTo);
            updatedDbZone2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbZone2;
        }
    }
}
