#nullable enable
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class that helps manage <see cref="DbOServiceTracking"/> information for all <see cref="AdapterService"/>s.
    /// </summary>
    internal interface IServiceTracker
    {
        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates whether the current <see cref="DbOServiceTracking<T>"/> is in the process of updating the database.
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
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.BinaryDataService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetBinaryDataServiceInfoAsync();

        /// <summary>
        /// The <see cref="DbOServiceTracking"/> entities associated with all <see cref="AdapterService"/>s.
        /// </summary>
        Task<List<DbOServiceTracking>> GetDbOServiceTrackingListAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the specified <paramref name="adapterService"/>.
        /// </summary>
        /// <param name="adapterService">The <see cref="AdapterService"/> of the <see cref="DbOServiceTracking"/> entity to be retrieved.</param>
        /// <returns></returns>
        Task<DbOServiceTracking> GetDbOServiceTrackingRecordAsync(AdapterService adapterService);

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.DebugDataService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetDebugDataServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.DeviceService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetDeviceServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.DeviceStatusInfoService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetDeviceStatusInfoServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.DiagnosticService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetDiagnosticServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.DriverChangeService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetDriverChangeServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.DutyStatusAvailabilityService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetDutyStatusAvailabilityServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.DVIRLogService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetDVIRLogServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.ExceptionEventService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetExceptionEventServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.FaultDataService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetFaultDataServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.LogRecordService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetLogRecordServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.RuleService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetRuleServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.StatusDataService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetStatusDataServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.TripService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetTripServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.UserService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetUserServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.ZoneService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetZoneServiceInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOServiceTracking"/> entity associated with the <see cref="AdapterService.ZoneTypeService"/>.
        /// </summary>
        Task<DbOServiceTracking> GetZoneTypeServiceInfoAsync();

        /// <summary>
        /// Retrieves all existing <see cref="DbOServiceTracking"/> entities from the database. For any <see cref="AdapterService"/> not included in the retrieved list, new entities will be created and persisted to the database.
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
        /// Indicates whether the subject <see cref="AdapterService"/> is currently running. Since services can be distributed across multiple machines, this is a guess based on whether the subject <see cref="AdapterService"/> has processed any data within the past two days.
        /// </summary>
        /// <param name="adapterService">The <see cref="AdapterService"/> to check.</param>
        /// <returns></returns>
        Task<bool> ServiceIsRunningAsync(AdapterService adapterService);

        /// <summary>
        /// Updates the <see cref="DbOServiceTracking"/> entity associated with the specified <paramref name="adapterService"/> setting its properties with the associated supplied parameter values. Any properties for which null values are supplied will not be updated. 
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="adapterService">The <see cref="AdapterService"/> of the <see cref="DbOServiceTracking"/> entity to be updated.</param>
        /// <param name="entitiesLastProcessedUtc">The new <see cref="DbOServiceTracking.EntitiesLastProcessedUtc"/> value to use.</param>
        /// <param name="lastProcessedFeedVersion">The new <see cref="DbOServiceTracking.LastProcessedFeedVersion"/> value to use.</param>
        /// <returns></returns>
        Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, AdapterService adapterService, DateTime? entitiesLastProcessedUtc = null, long? lastProcessedFeedVersion = null);

        /// <summary>
        /// Updates the <see cref="DbOServiceTracking"/> entity associated with the specified <paramref name="adapterService"/> setting its properties with the associated supplied parameter values. Any properties for which null values are supplied will not be updated.
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="adapterService">The <see cref="AdapterService"/> of the <see cref="DbOServiceTracking"/> entity to be updated.</param>
        /// <param name="adapterVersion">The new <see cref="DbOServiceTracking.AdapterVersion"/> value to use.</param>
        /// <param name="adapterMachineName">The new <see cref="DbOServiceTracking.AdapterMachineName"/> value to use.</param>
        /// <returns></returns>
        Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, AdapterService adapterService, string? adapterVersion = null, string? adapterMachineName = null);

        /// <summary>
        /// Waits until <see cref="IsUpdating"/> is <c>false</c>. Intended for use by methods that enumerate and retrieve cached objects.
        /// </summary>
        /// <returns></returns>
        Task WaitIfUpdatingAsync();
    }
}
