#nullable enable
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that helps manage <see cref="T"/> information for all <see cref="AdapterService"/>s.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDbOServiceTracking"/> implementation to be used.</typeparam>
    internal interface IServiceTracker<T> where T : IDbOServiceTracking
    {
        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates whether the current <see cref="T"/> is in the process of updating the database.
        /// </summary>
        bool IsUpdating { get; }

        /// <summary>
        /// Checks whether each <see cref="AdapterService"/> in <paramref name="prerequisiteServices"/> has been run before and is currently running. Results are returned in a <see cref="PrerequisiteServiceOperationCheckResult"/>. 
        /// </summary>
        /// <param name="prerequisiteServices">The list of prerequisite <see cref="AdapterService"/>s to check.</param>
        /// <param name="includeCheckForWhetherServicesHaveProcessedAnyData">If set to true, an additional check will be done to see whether each service has processed data.</param>
        /// <returns></returns>
        Task<PrerequisiteServiceOperationCheckResult> CheckOperationOfPrerequisiteServicesAsync(List<AdapterService> prerequisiteServices, bool includeCheckForWhetherServicesHaveProcessedAnyData = false);

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.BinaryDataProcessor"/>.
        /// </summary>
        Task<T> GetBinaryDataServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.BinaryDataProcessor2"/>.
        /// </summary>
        Task<T> GetBinaryDataService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.ChargeEventProcessor"/>.
        /// </summary>
        Task<T> GetChargeEventServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.ChargeEventProcessor2"/>.
        /// </summary>
        Task<T> GetChargeEventService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DatabaseMaintenanceService"/>.
        /// </summary>
        Task<T> GetDatabaseMaintenanceService2InfoAsync();

        /// <summary>
        /// The <see cref="T"/> entities associated with all <see cref="AdapterService"/>s.
        /// </summary>
        Task<List<T>> GetDbOServiceTrackingListAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the specified <paramref name="adapterService"/>.
        /// </summary>
        /// <param name="adapterService">The <see cref="AdapterService"/> of the <see cref="T"/> entity to be retrieved.</param>
        /// <returns></returns>
        Task<T> GetDbOServiceTrackingRecordAsync(AdapterService adapterService);

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DebugDataProcessor"/>.
        /// </summary>
        Task<T> GetDebugDataServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DeviceProcessor"/>.
        /// </summary>
        Task<T> GetDeviceServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DeviceProcessor2"/>.
        /// </summary>
        Task<T> GetDeviceService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DeviceStatusInfoProcessor"/>.
        /// </summary>
        Task<T> GetDeviceStatusInfoServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DeviceStatusInfoProcessor2"/>.
        /// </summary>
        Task<T> GetDeviceStatusInfoService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DiagnosticProcessor"/>.
        /// </summary>
        Task<T> GetDiagnosticServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DiagnosticProcessor2"/>.
        /// </summary>
        Task<T> GetDiagnosticService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DriverChangeProcessor"/>.
        /// </summary>
        Task<T> GetDriverChangeServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DriverChangeProcessor2"/>.
        /// </summary>
        Task<T> GetDriverChangeService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DutyStatusAvailabilityProcessor"/>.
        /// </summary>
        Task<T> GetDutyStatusAvailabilityServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DutyStatusAvailabilityProcessor2"/>.
        /// </summary>
        Task<T> GetDutyStatusAvailabilityService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DutyStatusLogProcessor"/>.
        /// </summary>
        Task<T> GetDutyStatusLogServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DutyStatusLogProcessor2"/>.
        /// </summary>
        Task<T> GetDutyStatusLogService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DVIRLogProcessor"/>.
        /// </summary>
        Task<T> GetDVIRLogServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.DVIRLogProcessor2"/>.
        /// </summary>
        Task<T> GetDVIRLogService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.ExceptionEventProcessor"/>.
        /// </summary>
        Task<T> GetExceptionEventServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.ExceptionEventProcessor2"/>.
        /// </summary>
        Task<T> GetExceptionEventService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.FaultDataLocationService2"/>.
        /// </summary>
        Task<T> GetFaultDataLocationService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.FaultDataProcessor"/>.
        /// </summary>
        Task<T> GetFaultDataServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.FaultDataProcessor2"/>.
        /// </summary>
        Task<T> GetFaultDataService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.FuelAndEnergyUsedProcessor2"/>.
        /// </summary>
        /// <returns></returns>
        Task<T> GetFuelAndEnergyUsedService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.GroupProcessor"/>.
        /// </summary>
        Task<T> GetGroupServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.GroupProcessor2"/>.
        /// </summary>
        Task<T> GetGroupService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.LogRecordProcessor"/>.
        /// </summary>
        Task<T> GetLogRecordServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.LogRecordProcessor2"/>.
        /// </summary>
        Task<T> GetLogRecordService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.RuleProcessor"/>.
        /// </summary>
        Task<T> GetRuleServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.RuleProcessor2"/>.
        /// </summary>
        Task<T> GetRuleService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.StatusDataLocationService2"/>.
        /// </summary>
        Task<T> GetStatusDataLocationService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.StatusDataProcessor"/>.
        /// </summary>
        Task<T> GetStatusDataServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.StatusDataProcessor2"/>.
        /// </summary>
        Task<T> GetStatusDataService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.TripProcessor"/>.
        /// </summary>
        Task<T> GetTripServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.TripProcessor2"/>.
        /// </summary>
        Task<T> GetTripService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.UserProcessor"/>.
        /// </summary>
        Task<T> GetUserServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.UserProcessor2"/>.
        /// </summary>
        Task<T> GetUserService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.ZoneProcessor"/>.
        /// </summary>
        Task<T> GetZoneServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.ZoneProcessor2"/>.
        /// </summary>
        Task<T> GetZoneService2InfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.ZoneTypeProcessor"/>.
        /// </summary>
        Task<T> GetZoneTypeServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the <see cref="AdapterService.ZoneTypeProcessor2"/>.
        /// </summary>
        Task<T> GetZoneTypeService2InfoAsync();

        /// <summary>
        /// Retrieves all existing <see cref="T"/> entities from the database. For any <see cref="AdapterService"/> not included in the retrieved list, new entities will be created and persisted to the database.
        /// </summary>
        /// <returns></returns>
        Task InitializeDbOServiceTrackingListAsync();

        /// <summary>
        /// Indicates whether the subject <see cref="AdapterService"/> has ever been run.
        /// </summary>
        /// <param name="adapterService">The <see cref="AdapterService"/> to check.</param>
        /// <returns></returns>
        Task<bool> ServiceHasBeenRunAsync(AdapterService adapterService);

        /// <summary>
        /// Indicates whether the subject <see cref="AdapterService"/> has processed data.
        /// </summary>
        /// <param name="adapterService">The <see cref="AdapterService"/> to check.</param>
        /// <returns></returns>
        Task<bool> ServiceHasProcessedDataAsync(AdapterService adapterService);

        /// <summary>
        /// Indicates whether the subject <see cref="AdapterService"/> has processed data since <paramref name="sinceDateTime"/>.
        /// </summary>
        /// <param name="adapterService">The <see cref="AdapterService"/> to check.</param>
        /// <param name="sinceDateTime">The UTC DateTime to check against for more recent activity.</param>
        /// <returns></returns>
        Task<bool> ServiceHasProcessedDataSinceAsync(AdapterService adapterService, DateTime sinceDateTime);

        /// <summary>
        /// Indicates whether the subject <see cref="AdapterService"/> is currently running. Since services can be distributed across multiple machines, this is a guess based on whether the subject <see cref="AdapterService"/> has processed any data within the past two days.
        /// </summary>
        /// <param name="adapterService">The <see cref="AdapterService"/> to check.</param>
        /// <returns></returns>
        Task<bool> ServiceIsRunningAsync(AdapterService adapterService);

        /// <summary>
        /// Updates the <see cref="T"/> entity associated with the specified <paramref name="adapterService"/> setting its properties with the associated supplied parameter values. Any properties for which null values are supplied will not be updated. 
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="adapterService">The <see cref="AdapterService"/> of the <see cref="T"/> entity to be updated.</param>
        /// <param name="entitiesLastProcessedUtc">The new <see cref="T.EntitiesLastProcessedUtc"/> value to use.</param>
        /// <param name="lastProcessedFeedVersion">The new <see cref="T.LastProcessedFeedVersion"/> value to use.</param>
        /// <returns></returns>
        Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, AdapterService adapterService, DateTime? entitiesLastProcessedUtc = null, long? lastProcessedFeedVersion = null);

        /// <summary>
        /// Updates the <see cref="T"/> entity associated with the specified <paramref name="adapterService"/> setting its properties with the associated supplied parameter values. Any properties for which null values are supplied will not be updated.
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="adapterService">The <see cref="AdapterService"/> of the <see cref="T"/> entity to be updated.</param>
        /// <param name="adapterVersion">The new <see cref="T.AdapterVersion"/> value to use.</param>
        /// <param name="adapterMachineName">The new <see cref="T.AdapterMachineName"/> value to use.</param>
        /// <returns></returns>
        Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, AdapterService adapterService, string? adapterVersion = null, string? adapterMachineName = null);

        /// <summary>
        /// Waits until <see cref="IsUpdating"/> is <c>false</c>. Intended for use by methods that enumerate and retrieve cached objects.
        /// </summary>
        /// <returns></returns>
        Task WaitIfUpdatingAsync();
    }
}
