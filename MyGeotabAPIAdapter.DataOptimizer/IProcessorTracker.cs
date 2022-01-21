#nullable enable
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// Interface for a class that helps manage <see cref="DbOProcessorTracking"/> information for all <see cref="DataOptimizerProcessor"/>.
    /// </summary>
    public interface IProcessorTracker
    {
        /// <summary>
        /// Indicates whether the current <see cref="DbOProcessorTracking<T>"/> is in the process of updating the database.
        /// </summary>
        bool IsUpdating { get; }

        /// <summary>
        /// Checks whether each <see cref="DataOptimizerProcessor"/> in <paramref name="prerequisiteProcessors"/> has been run before and is currently running. Results are returned in a <see cref="PrerequisiteProcessorOperationCheckResult"/>. 
        /// </summary>
        /// <param name="prerequisiteProcessors">The list of prerequisite <see cref="DataOptimizerProcessor"/>s to check.</param>
        /// <returns></returns>
        Task<PrerequisiteProcessorOperationCheckResult> CheckOperationOfPrerequisiteProcessors(List<DataOptimizerProcessor> prerequisiteProcessors);

        /// <summary>
        /// The <see cref="DbOProcessorTracking"/> entities associated with all <see cref="DataOptimizerProcessor"/>.
        /// </summary>
        Task<List<DbOProcessorTracking>> GetDbOProcessorTrackingListAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the specified <paramref name="dataOptimizerProcessor"/>.
        /// </summary>
        /// <param name="dataOptimizerProcessor">The <see cref="DataOptimizerProcessor"/> of the <see cref="DbOProcessorTracking"/> entity to be retrieved.</param>
        /// <returns></returns>
        Task<DbOProcessorTracking> GetDbOProcessorTrackingRecordAsync(DataOptimizerProcessor dataOptimizerProcessor);

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.BinaryDataProcessor"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetBinaryDataProcessorInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.DeviceProcessor"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetDeviceProcessorInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.DiagnosticProcessor"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetDiagnosticProcessorInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.DriverChangeProcessor"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetDriverChangeProcessorInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.FaultDataOptimizer"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetFaultDataOptimizerInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.FaultDataProcessor"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetFaultDataProcessorInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.LogRecordProcessor"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetLogRecordProcessorInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.StatusDataOptimizer"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetStatusDataOptimizerInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.StatusDataProcessor"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetStatusDataProcessorInfoAsync();

        /// <summary>
        /// Retrieves the <see cref="DbOProcessorTracking"/> entity associated with the <see cref="DataOptimizerProcessor.UserProcessor"/>.
        /// </summary>
        Task<DbOProcessorTracking> GetUserProcessorInfoAsync();

        /// <summary>
        /// Retrieves all existing <see cref="DbOProcessorTracking"/> entities from the database. For any <see cref="DataOptimizerProcessor"/> not included in the retrieved list, new entities will be created and persisted to the database.
        /// </summary>
        /// <returns></returns>
        Task InitializeDbOProcessorTrackingListAsync();

        /// <summary>
        /// Persists the <paramref name="dbOProcessorTrackingsToPersist"/> to database.
        /// </summary>
        /// <param name="dbOProcessorTrackingsToPersist">The list of <see cref="DbOProcessorTracking"/> entities to persist to database.</param>
        /// <returns></returns>
        Task PersistDbOProcessorTrackingRecordsToDatabase(List<DbOProcessorTracking> dbOProcessorTrackingsToPersist);

        /// <summary>
        /// Indicates whether the subject <see cref="DataOptimizerProcessor"/> has ever been run.
        /// </summary>
        /// <param name="dataOptimizerProcessor">The <see cref="DataOptimizerProcessor"/> to check.</param>
        /// <returns></returns>
        Task<bool> ProcessorHasBeenRun(DataOptimizerProcessor dataOptimizerProcessor);

        /// <summary>
        /// Indicates whether the subject <see cref="DataOptimizerProcessor"/> is currently running. Since processors can be distributed across multiple machines, this is a guess based on whether the subject <see cref="DataOptimizerProcessor"/> has processed any data within the past two days.
        /// </summary>
        /// <param name="dataOptimizerProcessor">The <see cref="DataOptimizerProcessor"/> to check.</param>
        /// <returns></returns>
        Task<bool> ProcessorIsRunning(DataOptimizerProcessor dataOptimizerProcessor);

        /// <summary>
        /// Updates the <see cref="DbOProcessorTracking"/> entity associated with the specified <paramref name="dataOptimizerProcessor"/> setting its properties with the associated supplied parameter values. Any properties for which null values are supplied will not be updated. 
        /// </summary>
        /// <param name="context">The <see cref="UnitOfWorkContext"/> to use.</param>
        /// <param name="dataOptimizerProcessor">The <see cref="DataOptimizerProcessor"/> of the <see cref="DbOProcessorTracking"/> entity to be updated.</param>
        /// <param name="entitiesLastProcessedUtc">The new <see cref="DbOProcessorTracking.EntitiesLastProcessedUtc"/> value to use.</param>
        /// <param name="adapterDbLastId">The new <see cref="DbOProcessorTracking.AdapterDbLastId"/> value to use.</param>
        /// <param name="adapterDbLastRecordCreationTimeUtc">The new <see cref="DbOProcessorTracking.AdapterDbLastRecordCreationTimeUtc"/> value to use.</param>
        /// <param name="adapterDbLastGeotabId">The new <see cref="DbOProcessorTracking.AdapterDbLastGeotabId"/> value to use.</param>
        /// <returns></returns>
        Task UpdateDbOProcessorTrackingRecord(UnitOfWorkContext context, DataOptimizerProcessor dataOptimizerProcessor, DateTime? entitiesLastProcessedUtc, long? adapterDbLastId, DateTime? adapterDbLastRecordCreationTimeUtc, string? adapterDbLastGeotabId);

        /// <summary>
        /// Updates the <see cref="DbOProcessorTracking"/> entity associated with the specified <paramref name="dataOptimizerProcessor"/> setting its properties with the associated supplied parameter values. Any properties for which null values are supplied will not be updated.
        /// </summary>
        /// <param name="context">The <see cref="UnitOfWorkContext"/> to use.</param>
        /// <param name="dataOptimizerProcessor">The <see cref="DataOptimizerProcessor"/> of the <see cref="DbOProcessorTracking"/> entity to be updated.</param>
        /// <param name="optimizerVersion">The new <see cref="DbOProcessorTracking.OptimizerVersion"/> value to use.</param>
        /// <param name="optimizerMachineName">The new <see cref="DbOProcessorTracking.OptimizerMachineName"/> value to use.</param>
        /// <returns></returns>
        Task UpdateDbOProcessorTrackingRecord(UnitOfWorkContext context, DataOptimizerProcessor dataOptimizerProcessor, string? optimizerVersion, string? optimizerMachineName);

        /// <summary>
        /// Waits until <see cref="IsUpdating"/> is <c>false</c>. Intended for use by methods that enumerate and retrieve cached objects.
        /// </summary>
        /// <returns></returns>
        Task WaitIfUpdatingAsync();
    }
}
