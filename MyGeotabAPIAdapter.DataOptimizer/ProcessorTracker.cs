using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using NLog;
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
        bool isUpdating = false;

        readonly IDateTimeHelper dateTimeHelper;
        readonly IGenericEntityPersister<DbOProcessorTracking> dbOProcessorTrackingEntityPersister;
        readonly IGenericDbObjectCache<DbOProcessorTracking> dbOProcessorTrackingObjectCache;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly UnitOfWorkContext context;

        /// <inheritdoc/>
        public bool IsUpdating { get => isUpdating; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessorTracker"/> class.
        /// </summary>
        public ProcessorTracker(IDateTimeHelper dateTimeHelper, IGenericEntityPersister<DbOProcessorTracking> dbOProcessorTrackingEntityPersister, IGenericDbObjectCache<DbOProcessorTracking> dbOProcessorTrackingObjectCache, UnitOfWorkContext context)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.dateTimeHelper = dateTimeHelper;
            this.dbOProcessorTrackingEntityPersister = dbOProcessorTrackingEntityPersister;
            this.dbOProcessorTrackingObjectCache = dbOProcessorTrackingObjectCache;
            this.context = context;

            InitializeDbOProcessorTrackingListAsync().Wait();

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<PrerequisiteProcessorOperationCheckResult> CheckOperationOfPrerequisiteProcessors(List<DataOptimizerProcessor> prerequisiteProcessors)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            var allPrerequisiteProcessorsRunning = true;
            var processorsNeverRun = new List<DataOptimizerProcessor>();
            var processorsNotRunning = new List<DataOptimizerProcessor>();
            var processorsWithNoDataProcessed = new List<DataOptimizerProcessor>();

            foreach (var prerequisiteProcessor in prerequisiteProcessors)
            {
                var processorHasBeenRun = await ProcessorHasBeenRun(prerequisiteProcessor);
                if (processorHasBeenRun == false)
                {
                    processorsNeverRun.Add(prerequisiteProcessor);
                }

                var processorIsRunning = await ProcessorIsRunning(prerequisiteProcessor);
                if (processorIsRunning == false)
                {
                    allPrerequisiteProcessorsRunning = false;
                    processorsNotRunning.Add(prerequisiteProcessor);
                }

                var processorHasProcessedData = await ProcessorHasProcessedData(prerequisiteProcessor);
                if (processorHasProcessedData == false)
                {
                    processorsWithNoDataProcessed.Add(prerequisiteProcessor);
                }
            }

            var result = new PrerequisiteProcessorOperationCheckResult(allPrerequisiteProcessorsRunning, processorsNeverRun, processorsNotRunning, processorsWithNoDataProcessed);
            return result;
        }

        public async Task<List<DbOProcessorTracking>> GetDbOProcessorTrackingListAsync()
        {
            var result = await dbOProcessorTrackingObjectCache.GetObjectsAsync();
            return result;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetDbOProcessorTrackingRecordAsync(DataOptimizerProcessor dataOptimizerProcessor)
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(dataOptimizerProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetBinaryDataProcessorInfoAsync()
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.BinaryDataProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetDeviceProcessorInfoAsync()
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.DeviceProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetDiagnosticProcessorInfoAsync()
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.DiagnosticProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetDriverChangeProcessorInfoAsync()
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.DriverChangeProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetFaultDataOptimizerInfoAsync()
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.FaultDataOptimizer.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetFaultDataProcessorInfoAsync()
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.FaultDataProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetLogRecordProcessorInfoAsync()
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.LogRecordProcessor.ToString());
            return dbOProcessorTracking;
        }


        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetStatusDataOptimizerInfoAsync()
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.StatusDataOptimizer.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetStatusDataProcessorInfoAsync()
        {
            var dbOProcessorTracking = await dbOProcessorTrackingObjectCache.GetObjectAsync(DataOptimizerProcessor.StatusDataProcessor.ToString());
            return dbOProcessorTracking;
        }

        /// <inheritdoc/>
        public async Task<DbOProcessorTracking> GetUserProcessorInfoAsync()
        {
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

            await PersistDbOProcessorTrackingRecordsToDatabase(dbOProcessorTrackingsToPersist);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task PersistDbOProcessorTrackingRecordsToDatabase(List<DbOProcessorTracking> dbOProcessorTrackingsToPersist)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            if (dbOProcessorTrackingsToPersist.Any())
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    await WaitIfUpdatingAsync();
                    isUpdating = true;
                    using (var uow = context.CreateUnitOfWork(Databases.OptimizerDatabase))
                    {
                        try
                        {
                            await dbOProcessorTrackingEntityPersister.PersistEntitiesToDatabaseAsync(context, dbOProcessorTrackingsToPersist, cancellationTokenSource, Logging.LogLevel.Debug);

                            // Commit transactions:
                            await uow.CommitAsync();
                        }
                        catch (Exception)
                        {
                            await uow.RollBackAsync();
                            throw;
                        }
                    }
                    isUpdating = false;
                }
                // Force the DbOProcessorTracking cache to be updated so that the changes are immediately available to other consumers.
                await dbOProcessorTrackingObjectCache.UpdateAsync(true);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessorHasBeenRun(DataOptimizerProcessor dataOptimizerProcessor)
        {
            var dbOProcessorTracking = await GetDbOProcessorTrackingRecordAsync(dataOptimizerProcessor);
            return dbOProcessorTracking.EntitiesHaveBeenProcessed;
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessorHasProcessedData(DataOptimizerProcessor dataOptimizerProcessor)
        {
            var dbOProcessorTracking = await GetDbOProcessorTrackingRecordAsync(dataOptimizerProcessor);
            if (dbOProcessorTracking.AdapterDbLastId != null)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessorIsRunning(DataOptimizerProcessor dataOptimizerProcessor)
        {
            const int CutoffDays = 2;
            var dbOProcessorTracking = await GetDbOProcessorTrackingRecordAsync(dataOptimizerProcessor);
            var processorLastProcessedEntitiesUtc = (DateTime)dbOProcessorTracking.EntitiesLastProcessedUtc;
            var processorIsRunning = !dateTimeHelper.TimeIntervalHasElapsed(processorLastProcessedEntitiesUtc, DateTimeIntervalType.Days, CutoffDays);
            return processorIsRunning;
        }

#nullable enable
        /// <inheritdoc/>
        public async Task UpdateDbOProcessorTrackingRecord(UnitOfWorkContext context, DataOptimizerProcessor dataOptimizerProcessor, DateTime? entitiesLastProcessedUtc, long? adapterDbLastId = null, DateTime? adapterDbLastRecordCreationTimeUtc = null, string? adapterDbLastGeotabId = null)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            MethodBase methodBase = MethodBase.GetCurrentMethod();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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
            await PersistDbOProcessorTrackingRecordsToDatabase(dbOProcessorTrackingsToUpdate);

            // Force the  DbOProcessorTracking cache to be updated so that the changes are immediately available to other consumers.
            await dbOProcessorTrackingObjectCache.UpdateAsync(true);

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
#nullable disable

#nullable enable
        /// <inheritdoc/>
        public async Task UpdateDbOProcessorTrackingRecord(UnitOfWorkContext context, DataOptimizerProcessor dataOptimizerProcessor, string? optimizerVersion, string? optimizerMachineName)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            MethodBase methodBase = MethodBase.GetCurrentMethod();
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.

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
            await PersistDbOProcessorTrackingRecordsToDatabase(dbOProcessorTrackingsToUpdate);

            // Force the  DbOProcessorTracking cache to be updated so that the changes are immediately available to other consumers.
            await dbOProcessorTrackingObjectCache.UpdateAsync(true);

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
