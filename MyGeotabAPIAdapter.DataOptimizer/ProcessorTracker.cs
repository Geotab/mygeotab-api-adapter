using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// A class that helps manage <see cref="DbOProcessorTracking"/> information for all <see cref="DataOptimizerProcessor"/>.
    /// </summary>
    public class ProcessorTracker : IProcessorTracker
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        bool cacheIsStale = false;
        bool isUpdating = false;
        readonly SemaphoreSlim cacheReloadLock = new(1, 1);

        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbOProcessorTracking> dbOProcessorTrackingEntityPersister;
        readonly IGenericGenericDbObjectCache<DbOProcessorTracking, OptimizerGenericDbObjectCache<DbOProcessorTracking>> dbOProcessorTrackingObjectCache;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context;

        /// <inheritdoc/>
        public bool IsUpdating { get => isUpdating; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessorTracker"/> class.
        /// </summary>
        public ProcessorTracker(IDateTimeHelper dateTimeHelper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbOProcessorTracking> dbOProcessorTrackingEntityPersister, IGenericGenericDbObjectCache<DbOProcessorTracking, OptimizerGenericDbObjectCache<DbOProcessorTracking>> dbOProcessorTrackingObjectCache, IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dateTimeHelper = dateTimeHelper;
            this.exceptionHelper = exceptionHelper;
            this.dbOProcessorTrackingEntityPersister = dbOProcessorTrackingEntityPersister;
            this.dbOProcessorTrackingObjectCache = dbOProcessorTrackingObjectCache;
            this.context = context;

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            InitializeDbOProcessorTrackingListAsync().Wait();

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<PrerequisiteProcessorOperationCheckResult> CheckOperationOfPrerequisiteProcessorsAsync(List<DataOptimizerProcessor> prerequisiteProcessors)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var allPrerequisiteProcessorsRunning = true;
            var processorsNeverRun = new List<DataOptimizerProcessor>();
            var processorsNotRunning = new List<DataOptimizerProcessor>();
            var processorsWithNoDataProcessed = new List<DataOptimizerProcessor>();

            foreach (var prerequisiteProcessor in prerequisiteProcessors)
            {
                var processorHasBeenRun = await ProcessorHasBeenRunAsync(prerequisiteProcessor);
                if (processorHasBeenRun == false)
                {
                    processorsNeverRun.Add(prerequisiteProcessor);
                }

                var processorIsRunning = await ProcessorIsRunningAsync(prerequisiteProcessor);
                if (processorIsRunning == false)
                {
                    allPrerequisiteProcessorsRunning = false;
                    processorsNotRunning.Add(prerequisiteProcessor);
                }

                var processorHasProcessedData = await ProcessorHasProcessedDataAsync(prerequisiteProcessor);
                if (processorHasProcessedData == false)
                {
                    processorsWithNoDataProcessed.Add(prerequisiteProcessor);
                }
            }

            var result = new PrerequisiteProcessorOperationCheckResult(allPrerequisiteProcessorsRunning, processorsNeverRun, processorsNotRunning, processorsWithNoDataProcessed);
            return result;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetBinaryDataProcessorInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.BinaryDataProcessor.ToString());
            return dbOProcessorTracking;
        }

        public async Task<List<DbOProcessorTracking>> GetDbOProcessorTrackingListAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var result = await dbOProcessorTrackingObjectCache.GetObjectsAsync();
            return result;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetDbOProcessorTrackingRecordAsync(DataOptimizerProcessor dataOptimizerProcessor)
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(dataOptimizerProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetDeviceProcessorInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.DeviceProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetDiagnosticProcessorInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.DiagnosticProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetDriverChangeProcessorInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.DriverChangeProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetFaultDataOptimizerInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.FaultDataOptimizer.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetFaultDataProcessorInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.FaultDataProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetLogRecordProcessorInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.LogRecordProcessor.ToString());
            return dbOProcessorTracking;
        }


        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetStatusDataOptimizerInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.StatusDataOptimizer.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetStatusDataProcessorInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.StatusDataProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetUserProcessorInfoAsync()
        {
            await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.UserProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task InitializeDbOProcessorTrackingListAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            await dbOProcessorTrackingObjectCache.InitializeAsync(Databases.OptimizerDatabase);

            // Make sure that a OProcessorTracking record exists for each of the processors. For any that don't have a record (e.g. when the application is run for the first time), create a new record.
            var dbOProcessorTrackingsToPersist = new List<DbOProcessorTracking>();
            var processors = Enum.GetValues(typeof(DataOptimizerProcessor));
            foreach (var processor in processors)
            {
                var existingProcessor = await dbOProcessorTrackingObjectCache.GetObjectAsync(processor.ToString());
                if (existingProcessor == null)
                {
                    DbOProcessorTracking newDbOProcessorTracking = new()
                    {
                        DatabaseWriteOperationType = Database.Common.DatabaseWriteOperationType.Insert,
                        ProcessorId = processor.ToString(),
                        RecordLastChangedUtc = DateTime.UtcNow
                    };
                    dbOProcessorTrackingsToPersist.Add(newDbOProcessorTracking);
                }
            }

            if (dbOProcessorTrackingsToPersist.Any())
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                    {
                        using (var uow = context.CreateUnitOfWork(Databases.OptimizerDatabase))
                        {
                            try
                            {
                                await PersistDbOProcessorTrackingRecordsToDatabaseAsync(context, dbOProcessorTrackingsToPersist);

                                // Commit transactions:
                                await uow.CommitAsync();
                            }
                            catch (Exception ex)
                            {
                                exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                await uow.RollBackAsync();
                                throw;
                            }
                        }
                    }, new Context());
                }
                await ReloadDbOProcessorTrackingObjectCacheIfStaleAsync();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Persists the <paramref name="dbOProcessorTrackingsToPersist"/> to database.
        /// </summary>
        /// <param name="context">The <see cref="OptimizerDatabaseUnitOfWorkContext"/> to use.</param>
        /// <param name="dbOProcessorTrackingsToPersist">The list of <see cref="DbOProcessorTracking"/> entities to persist to database.</param>
        /// <returns></returns>
        async Task PersistDbOProcessorTrackingRecordsToDatabaseAsync(IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context, List<DbOProcessorTracking> dbOProcessorTrackingsToPersist)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (dbOProcessorTrackingsToPersist.Any())
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    await WaitIfUpdatingAsync();
                    isUpdating = true;
                    await dbOProcessorTrackingEntityPersister.PersistEntitiesToDatabaseAsync(context, dbOProcessorTrackingsToPersist, cancellationTokenSource, Logging.LogLevel.Debug);
                    isUpdating = false;
                }
                cacheIsStale = true;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessorHasBeenRunAsync(DataOptimizerProcessor dataOptimizerProcessor)
        {
            var dbOProcessorTracking = await GetDbOProcessorTrackingRecordAsync(dataOptimizerProcessor);
            return dbOProcessorTracking.EntitiesHaveBeenProcessed;
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessorHasProcessedDataAsync(DataOptimizerProcessor dataOptimizerProcessor)
        {
            var dbOProcessorTracking = await GetDbOProcessorTrackingRecordAsync(dataOptimizerProcessor);
            if (dbOProcessorTracking.AdapterDbLastId != null)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessorIsRunningAsync(DataOptimizerProcessor dataOptimizerProcessor)
        {
            const int CutoffDays = 2;
            var dbOProcessorTracking = await GetDbOProcessorTrackingRecordAsync(dataOptimizerProcessor);
            var processorLastProcessedEntitiesUtc = (DateTime)dbOProcessorTracking.EntitiesLastProcessedUtc;
            var processorIsRunning = !dateTimeHelper.TimeIntervalHasElapsed(processorLastProcessedEntitiesUtc, DateTimeIntervalType.Days, CutoffDays);
            return processorIsRunning;
        }

        /// <summary>
        /// Reloads the <see cref="dbOProcessorTrackingObjectCache"/> if it has been flagged as stale.
        /// </summary>
        /// <returns></returns>
        async Task ReloadDbOProcessorTrackingObjectCacheIfStaleAsync()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Abort if cache is current.
            if (cacheIsStale == false)
            {
                return;
            }

            await cacheReloadLock.WaitAsync();
            try
            {
                if (cacheIsStale == true)
                {
                    await dbOProcessorTrackingObjectCache.UpdateAsync(true);
                    cacheIsStale = false;
                }
            }
            finally
            {
                cacheReloadLock.Release();
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

#nullable enable
        /// <inheritdoc/>
        public async Task UpdateDbOProcessorTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context, DataOptimizerProcessor dataOptimizerProcessor, DateTime? entitiesLastProcessedUtc, long? adapterDbLastId = null, DateTime? adapterDbLastRecordCreationTimeUtc = null, string? adapterDbLastGeotabId = null)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            var dbOProcessorTrackingToUpdate = await GetDbOProcessorTrackingRecordAsync(dataOptimizerProcessor);

            if (entitiesLastProcessedUtc != null)
            {
                dbOProcessorTrackingToUpdate.EntitiesLastProcessedUtc = entitiesLastProcessedUtc;
            }

            if (adapterDbLastId != null)
            {
                dbOProcessorTrackingToUpdate.AdapterDbLastId = adapterDbLastId;
            }

            if (adapterDbLastGeotabId != null)
            {
                dbOProcessorTrackingToUpdate.AdapterDbLastGeotabId = adapterDbLastGeotabId;
            }

            if (adapterDbLastRecordCreationTimeUtc != null)
            {
                 dbOProcessorTrackingToUpdate.AdapterDbLastRecordCreationTimeUtc = adapterDbLastRecordCreationTimeUtc;
            }

            dbOProcessorTrackingToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
            dbOProcessorTrackingToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            var dbOProcessorTrackingsToUpdate = new List<DbOProcessorTracking>
            {
                dbOProcessorTrackingToUpdate
            };
            await PersistDbOProcessorTrackingRecordsToDatabaseAsync(context, dbOProcessorTrackingsToUpdate);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
#nullable disable

#nullable enable
        /// <inheritdoc/>
        public async Task UpdateDbOProcessorTrackingRecordAsync(IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext> context, DataOptimizerProcessor dataOptimizerProcessor, string? optimizerVersion, string? optimizerMachineName)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
            var dbOProcessorTrackingToUpdate = await GetDbOProcessorTrackingRecordAsync(dataOptimizerProcessor);

            if (optimizerVersion != null)
            {
                dbOProcessorTrackingToUpdate.OptimizerVersion = optimizerVersion;
            }

            if (optimizerMachineName != null)
            {
                dbOProcessorTrackingToUpdate.OptimizerMachineName = optimizerMachineName;
            }

            dbOProcessorTrackingToUpdate.RecordLastChangedUtc = DateTime.UtcNow;
            dbOProcessorTrackingToUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;

            var dbOProcessorTrackingsToUpdate = new List<DbOProcessorTracking>
            {
                dbOProcessorTrackingToUpdate
            };
            await PersistDbOProcessorTrackingRecordsToDatabaseAsync(context, dbOProcessorTrackingsToUpdate);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
#nullable disable

        /// <inheritdoc/>
        public async Task WaitIfUpdatingAsync()
        {
            while (isUpdating)
            {
                await Task.Delay(25);
            }
        }
    }
}
