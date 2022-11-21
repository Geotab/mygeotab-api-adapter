﻿using Geotab.Checkmate.ObjectModel;
using Microsoft.CSharp.RuntimeBinder;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Device"/> and <see cref="DbDevice"/> entities.
    /// </summary>
    public class GeotabDeviceDbDeviceObjectMapper : IGeotabDeviceDbDeviceObjectMapper
    {
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

            DateTime entityToEvaluateActiveFromUtc = entityToEvaluate.ActiveFrom.GetValueOrDefault().ToUniversalTime();
            DateTime entityToEvaluateActiveToUtc = entityToEvaluate.ActiveTo.GetValueOrDefault().ToUniversalTime();
            if (entityToEvaluate.ActiveFrom != entityToMapTo.ActiveFrom && entityToEvaluateActiveFromUtc != entityToMapTo.ActiveFrom)
            {
                return true;
            }
            if (entityToEvaluate.ActiveTo != entityToMapTo.ActiveTo && entityToEvaluateActiveToUtc != entityToMapTo.ActiveTo)
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