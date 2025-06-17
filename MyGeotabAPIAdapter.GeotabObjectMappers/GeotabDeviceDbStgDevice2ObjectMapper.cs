using Geotab.Checkmate.ObjectModel;
using Microsoft.CSharp.RuntimeBinder;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;
using System.Text;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// A class with methods involving mapping between <see cref="Device"/> and <see cref="DbStgDevice2"/> entities.
    /// </summary>
    public class GeotabDeviceDbStgDevice2ObjectMapper : IGeotabDeviceDbStgDevice2ObjectMapper
    {
        readonly IGeotabIdConverter geotabIdConverter;

        public GeotabDeviceDbStgDevice2ObjectMapper(IGeotabIdConverter geotabIdConverter)
        {
            this.geotabIdConverter = geotabIdConverter;
        }

        /// <inheritdoc/>
        public List<DbStgDevice2> CreateEntities(List<Device> entitiesToMapTo)
        {
            var dbStgDevice2s = new List<DbStgDevice2>();
            foreach (var entity in entitiesToMapTo)
            {
                var dbStgDevice2 = CreateEntity(entity);
                dbStgDevice2s.Add(dbStgDevice2);
            }
            return dbStgDevice2s;
        }

        /// <inheritdoc/>
        public DbStgDevice2 CreateEntity(Device entityToMapTo)
        {
            string deviceLicensePlate = String.Empty;
            string deviceLicenseState = String.Empty;
            var deviceType = entityToMapTo.DeviceType;
            string deviceTypeString = deviceType.ToString() ?? String.Empty;
            string deviceVIN = String.Empty;
            dynamic convertedDevice = Convert.ChangeType(entityToMapTo, entityToMapTo.GetType());

            string deviceComment = entityToMapTo.Comment;

            try
            {
                deviceLicensePlate = convertedDevice.LicensePlate;
            }
            catch (RuntimeBinderException)
            {
                // Property does not exist for the subject Device type.
            }

            try
            {
                deviceLicenseState = convertedDevice.LicenseState;
            }
            catch (RuntimeBinderException)
            {
                // Property does not exist for the subject Device type.
            }

            try
            {
                deviceVIN = convertedDevice.VehicleIdentificationNumber;
            }
            catch (RuntimeBinderException)
            {
                // Property does not exist for the subject Device type.
            }

            if (deviceLicensePlate != null && deviceLicensePlate.Length == 0)
            {
                deviceLicensePlate = String.Empty;
            }
            if (deviceLicenseState != null && deviceLicenseState.Length == 0)
            {
                deviceLicenseState = String.Empty;
            }
            if (deviceVIN != null && deviceVIN.Length == 0)
            {
                deviceVIN = String.Empty;
            }
            if (deviceComment != null && deviceComment.Length == 0)
            {
                deviceComment = String.Empty;
            }

            DbStgDevice2 dbStgDevice2 = new()
            {
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                Comment = deviceComment,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DeviceType = deviceTypeString,
                EntityStatus = (int)Common.DatabaseRecordStatus.Active, // Set to Active by default. Database logic will handle setting to inactive if necessary (i.e. for detected deletions on the MYG side).
                GeotabId = entityToMapTo.Id.ToString(),
                id = geotabIdConverter.ToLong(entityToMapTo.Id),
                LicensePlate = deviceLicensePlate,
                LicenseState = deviceLicenseState,
                Name = entityToMapTo.Name,
                ProductId = entityToMapTo.ProductId,
                RecordLastChangedUtc = DateTime.UtcNow,
                SerialNumber = entityToMapTo.SerialNumber,
                VIN = deviceVIN
            };
            if (entityToMapTo.Groups != null && entityToMapTo.Groups.Count > 0)
            {
                dbStgDevice2.Groups = GetDeviceGroupsJSON(entityToMapTo.Groups);
            }
            // Until Trailer Ids have been fully migrated to Device Ids from the perspective of other enity types such as DVIRLog, it will be be necessary to use TmpTrailerId as the only possible way to associate Traler Id with Device Id. 
            if (entityToMapTo.TmpTrailerId != null)
            {
                dbStgDevice2.TmpTrailerGeotabId = entityToMapTo.TmpTrailerId.ToString();
                dbStgDevice2.TmpTrailerId = geotabIdConverter.ToGuid(entityToMapTo.TmpTrailerId);
            }
            return dbStgDevice2;
        }

        /// <inheritdoc/>
        public string GetDeviceGroupsJSON(IList<Group> deviceGroups)
        {
            bool deviceGroupsArrayHasItems = false;
            var deviceGroupsIds = new StringBuilder();
            deviceGroupsIds.Append('[');

            for (int i = 0; i < deviceGroups.Count; i++)
            {
                if (deviceGroupsArrayHasItems == true)
                {
                    deviceGroupsIds.Append(',');
                }
                string deviceGroupsId = deviceGroups[i].Id.ToString();
                deviceGroupsIds.Append($"{{\"id\":\"{deviceGroupsId}\"}}");
                deviceGroupsArrayHasItems = true;
            }
            deviceGroupsIds.Append(']');
            return deviceGroupsIds.ToString();
        }
    }
}
