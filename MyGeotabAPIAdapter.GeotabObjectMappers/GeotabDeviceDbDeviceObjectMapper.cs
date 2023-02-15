using Geotab.Checkmate.ObjectModel;
using Microsoft.CSharp.RuntimeBinder;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Device"/> and <see cref="DbDevice"/> entities.
    /// </summary>
    public class GeotabDeviceDbDeviceObjectMapper : IGeotabDeviceDbDeviceObjectMapper
    {
        readonly IDateTimeHelper dateTimeHelper;

        public GeotabDeviceDbDeviceObjectMapper(IDateTimeHelper dateTimeHelper)
        { 
            this.dateTimeHelper = dateTimeHelper;
        }

        /// <inheritdoc/>
        public DbDevice CreateEntity(Device entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
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

            DbDevice dbDevice = new()
            {
                ActiveFrom = entityToMapTo.ActiveFrom,
                ActiveTo = entityToMapTo.ActiveTo,
                Comment = deviceComment,
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                DeviceType = deviceTypeString,
                EntityStatus = (int)entityStatus,
                GeotabId = entityToMapTo.Id.ToString(),
                LicensePlate = deviceLicensePlate,
                LicenseState = deviceLicenseState,
                Name = entityToMapTo.Name,
                ProductId = entityToMapTo.ProductId,
                RecordLastChangedUtc = DateTime.UtcNow,
                SerialNumber = entityToMapTo.SerialNumber,
                VIN = deviceVIN
            };
            return dbDevice;
        }

        /// <inheritdoc/>
        public bool EntityRequiresUpdate(DbDevice entityToEvaluate, Device entityToMapTo)
        {
            if (entityToEvaluate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot compare {nameof(DbDevice)} '{entityToEvaluate.id}' with {nameof(Device)} '{entityToMapTo.Id}' because the IDs do not match.");
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
            string entityToMapToDeviceType = entityToMapTo.DeviceType.ToString();
            if (entityToEvaluate.DeviceType != entityToMapToDeviceType || entityToEvaluate.Name != entityToMapTo.Name || entityToEvaluate.SerialNumber != entityToMapTo.SerialNumber || entityToEvaluate.Comment != entityToMapTo.Comment)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public DbDevice UpdateEntity(DbDevice entityToUpdate, Device entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active)
        {
            if (entityToUpdate.GeotabId != entityToMapTo.Id.ToString())
            {
                throw new ArgumentException($"Cannot update {nameof(DbDevice)} '{entityToUpdate.id} (GeotabId {entityToUpdate.GeotabId})' with {nameof(Device)} '{entityToMapTo.Id}' because the GeotabIds do not match.");
            }

            var updatedDbDevice = CreateEntity(entityToMapTo);

            // Update id since id is auto-generated by the database on insert and is therefor not set by the CreateEntity method.
            updatedDbDevice.id = entityToUpdate.id;
            updatedDbDevice.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            return updatedDbDevice;
        }
    }
}
