using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;

namespace GeotabDIGAdapter
{
    /// <summary>
    /// Interface for a class that helps manage <see cref="T"/> information for all <see cref="DIGAdapterService"/>s.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDbOServiceTracking"/> implementation to be used.</typeparam>
    public interface IServiceTracker<T> where T : IDbOServiceTracking
    {
        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Indicates whether the current <see cref="IServiceTracker{T}"/> is in the process of updating the database.
        /// </summary>
        bool IsUpdating { get; }

        /// <summary>
        /// Checks whether each <see cref="DIGAdapterService"/> in <paramref name="prerequisiteServices"/> has been run before and is currently running. Results are returned in a <see cref="PrerequisiteServiceOperationCheckResult"/>. 
        /// </summary>
        /// <param name="prerequisiteServices">The list of prerequisite <see cref="DIGAdapterService"/>s to check.</param>
        /// <param name="includeCheckForWhetherServicesHaveProcessedAnyData">If set to true, an additional check will be done to see whether each service has processed data.</param>
        /// <returns></returns>
        Task<PrerequisiteServiceOperationCheckResult> CheckOperationOfPrerequisiteServicesAsync(List<DIGAdapterService> prerequisiteServices, bool includeCheckForWhetherServicesHaveProcessedAnyData = false);

        /// <summary>
        /// The <see cref="T"/> entities associated with all <see cref="DIGAdapterService"/>s.
        /// </summary>
        Task<List<T>> GetDbOServiceTrackingListAsync();

        /// <summary>
        /// Retrieves the <see cref="T"/> entity associated with the specified <paramref name="digAdapterService"/>.
        /// </summary>
        /// <param name="digAdapterService">The <see cref="DIGAdapterService"/> of the <see cref="T"/> entity to be retrieved.</param>
        /// <returns></returns>
        Task<T> GetDbOServiceTrackingRecordAsync(DIGAdapterService digAdapterService);

        /// <summary>
        /// Retrieves all existing <see cref="T"/> entities from the database. For any <see cref="DIGAdapterService"/> not included in the retrieved list, new entities will be created and persisted to the database.
        /// </summary>
        /// <returns></returns>
        Task InitializeDbOServiceTrackingListAsync();

        /// <summary>
        /// Indicates whether the subject <see cref="DIGAdapterService"/> has ever been run.
        /// </summary>
        /// <param name="digAdapterService">The <see cref="DIGAdapterService"/> to check.</param>
        /// <returns></returns>
        Task<bool> ServiceHasBeenRunAsync(DIGAdapterService digAdapterService);

        /// <summary>
        /// Indicates whether the subject <see cref="DIGAdapterService"/> has processed data.
        /// </summary>
        /// <param name="digAdapterService">The <see cref="DIGAdapterService"/> to check.</param>
        /// <returns></returns>
        Task<bool> ServiceHasProcessedDataAsync(DIGAdapterService digAdapterService);

        /// <summary>
        /// Indicates whether the subject <see cref="DIGAdapterService"/> has processed data since <paramref name="sinceDateTime"/>.
        /// </summary>
        /// <param name="digAdapterService">The <see cref="DIGAdapterService"/> to check.</param>
        /// <param name="sinceDateTime">The UTC DateTime to check against for more recent activity.</param>
        /// <returns></returns>
        Task<bool> ServiceHasProcessedDataSinceAsync(DIGAdapterService digAdapterService, DateTime sinceDateTime);

        /// <summary>
        /// Indicates whether the subject <see cref="DIGAdapterService"/> is currently running. Since services can be distributed across multiple machines, this is a guess based on whether the subject <see cref="DIGAdapterService"/> has processed any data within the past two days.
        /// </summary>
        /// <param name="digAdapterService">The <see cref="DIGAdapterService"/> to check.</param>
        /// <returns></returns>
        Task<bool> ServiceIsRunningAsync(DIGAdapterService digAdapterService);

        /// <summary>
        /// Updates the <see cref="T"/> entity associated with the specified <paramref name="digAdapterService"/> setting its properties with the associated supplied parameter values. Any properties for which null values are supplied will not be updated. 
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="digAdapterService">The <see cref="DIGAdapterService"/> of the <see cref="T"/> entity to be updated.</param>
        /// <param name="entitiesLastProcessedUtc">The new EntitiesLastProcessedUtc value to use.</param>
        /// <param name="lastProcessedFeedVersion">The new LastProcessedFeedVersion value to use.</param>
        /// <returns></returns>
        Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, DIGAdapterService digAdapterService, DateTime? entitiesLastProcessedUtc = null, long? lastProcessedFeedVersion = null);

        /// <summary>
        /// Updates the <see cref="T"/> entity associated with the specified <paramref name="digAdapterService"/> setting its properties with the associated supplied parameter values. Any properties for which null values are supplied will not be updated.
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="digAdapterService">The <see cref="DIGAdapterService"/> of the <see cref="T"/> entity to be updated.</param>
        /// <param name="adapterVersion">The new AdapterVersion value to use.</param>
        /// <param name="adapterMachineName">The new AdapterMachineName value to use.</param>
        /// <returns></returns>
        Task UpdateDbOServiceTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, DIGAdapterService digAdapterService, string? adapterVersion = null, string? adapterMachineName = null);

        /// <summary>
        /// Updates the <see cref="T"/> entity associated with the specified <paramref name="digAdapterService"/> with batch processing metrics.
        /// </summary>
        /// <param name="context">The <see cref="AdapterDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="digAdapterService">The <see cref="DIGAdapterService"/> of the <see cref="T"/> entity to be updated.</param>
        /// <param name="entitiesLastProcessedUtc">The timestamp when entities were last processed.</param>
        /// <param name="lastBatchSize">The size of the last processed batch.</param>
        /// <param name="successCount">The number of successfully processed records in the batch (added to cumulative total).</param>
        /// <param name="failureCount">The number of failed records in the batch (added to cumulative total).</param>
        /// <returns></returns>
        Task UpdateDbOServiceTrackingRecordWithMetricsAsync(IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context, DIGAdapterService digAdapterService, DateTime entitiesLastProcessedUtc, int lastBatchSize, int successCount, int failureCount);

        /// <summary>
        /// Waits until <see cref="IsUpdating"/> is <c>false</c>. Intended for use by methods that enumerate and retrieve cached objects.
        /// </summary>
        /// <returns></returns>
        Task WaitIfUpdatingAsync();
    }
}
