using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.Models;
using MyGeotabAPIAdapter.MyGeotabAPI;
using NLog;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Services
{
    /// <summary>
    /// A <see cref="BackgroundService"/> that propagates <see cref="DVIRLog"/> changes from tables in the adapter database to the associated MyGeotab database. 
    /// </summary>
    class DVIRLogManipulator2 : BackgroundService
    {
        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DVIRLogManipulator2> awaiter;
        readonly IDbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper dbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper;
        readonly IExceptionHelper exceptionHelper;
        readonly IGenericEntityPersister<DbFailDVIRDefectUpdateFailure2> dbFailDVIRDefectUpdateFailure2EntityPersister;
        readonly IGenericEntityPersister<DbUpdDVIRDefectUpdate2> dbUpdDVIRDefectUpdate2EntityPersister;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        const int DVIRDefectUpdateBatchSize = 5000;
        const int ExecutionThrottleEngagingRecordCount = 1;
        const int TimeoutSecondsForMyGeotabSetCalls = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="DVIRLogManipulator2"/> class.
        /// </summary>
        public DVIRLogManipulator2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<DVIRLogManipulator2> awaiter, IDbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper dbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbFailDVIRDefectUpdateFailure2> dbFailDVIRDefectUpdateFailure2EntityPersister, IGenericEntityPersister<DbUpdDVIRDefectUpdate2> dbUpdDVIRDefectUpdate2EntityPersister, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.dbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper = dbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper;
            this.exceptionHelper = exceptionHelper;
            this.dbFailDVIRDefectUpdateFailure2EntityPersister = dbFailDVIRDefectUpdateFailure2EntityPersister;
            this.dbUpdDVIRDefectUpdate2EntityPersister = dbUpdDVIRDefectUpdate2EntityPersister;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);
        }

        /// <summary>
        /// Adds a <see cref="DefectRemark"/> to the <paramref name="dvirDefect"/> using properties of the <paramref name="dbUpdDVIRDefectUpdate2"/>.
        /// </summary>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> to which a <see cref="DefectRemark"/> is to be added.</param>
        /// <param name="dbUpdDVIRDefectUpdate2">The <see cref="DbUpdDVIRDefectUpdate2"/> from which to obtain the property values for the <see cref="DefectRemark"/> that is to be added to the <paramref name="dvirDefect"/>.</param>
        /// <returns></returns>
        bool AddDefectRemark(DVIRDefect dvirDefect, DbUpdDVIRDefectUpdate2 dbUpdDVIRDefectUpdate2)
        {
            // Check whether the DbUpdDVIRDefectUpdate2 includes a remark to be added.
            if (dbUpdDVIRDefectUpdate2.Remark == null && dbUpdDVIRDefectUpdate2.RemarkDateTimeUtc == null && dbUpdDVIRDefectUpdate2.RemarkUserId == null)
            {
                // No remark to add.
                return false;
            }

            // Ensure all remark-related properties have values.
            if (dbUpdDVIRDefectUpdate2.Remark == null || dbUpdDVIRDefectUpdate2.RemarkDateTimeUtc == null || dbUpdDVIRDefectUpdate2.RemarkUserId == null)
            {
                throw new ArgumentException($"Cannot add DefectRemark to DVIRDefect because one or more DefectRemark-related properties (Remark, RemarkDateTimeUtc, or RemarkUserId) of the DbUpdDVIRDefectUpdate2 object are null.");
            }

            // Add the remark.
            DefectRemark defectRemark = new()
            {
                User = new User { Id = Id.Create((long)dbUpdDVIRDefectUpdate2.RemarkUserId) },
                DateTime = dbUpdDVIRDefectUpdate2.RemarkDateTimeUtc,
                Remark = dbUpdDVIRDefectUpdate2.Remark
            };
            if (dvirDefect.DefectRemarks == null)
            {
                dvirDefect.DefectRemarks = new List<DefectRemark>();
            }
            dvirDefect.DefectRemarks.Add(defectRemark);
            return true;
        }

        /// <summary>
        /// Applies updates to a <see cref="DVIRDefect"/> using properties of a <see cref="DbUpdDVIRDefectUpdate2"/>.
        /// </summary>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> to be updated.</param>
        /// <param name="dbUpdDVIRDefectUpdate2">The <see cref="DbUpdDVIRDefectUpdate2"/> to use to update the <paramref name="dvirDefect"/>.</param>
        /// <returns></returns>
        bool ApplyUpdatesToDVIRDefect(DVIRDefect dvirDefect, DbUpdDVIRDefectUpdate2 dbUpdDVIRDefectUpdate2)
        {
            bool defectRemarkAdded = AddDefectRemark(dvirDefect, dbUpdDVIRDefectUpdate2);
            bool repairStatusUpdated = UpdateDVIRDefectRepairStatus(dvirDefect, dbUpdDVIRDefectUpdate2);

            return defectRemarkAdded || repairStatusUpdated;
        }

        /// <summary>
        /// Deletes the record associated with the <paramref name="dbUpdDVIRDefectUpdate2"/> from the adapter database.
        /// </summary>
        /// <param name="dbUpdDVIRDefectUpdate2">The <see cref="DbUpdDVIRDefectUpdate2"/> to be deleted from the adapter database.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task DeleteProcessedDbUpdDVIRDefectUpdate2Async(DbUpdDVIRDefectUpdate2 dbUpdDVIRDefectUpdate2,  CancellationToken cancellationToken)
        {
            dbUpdDVIRDefectUpdate2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;

            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    await dbUpdDVIRDefectUpdate2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, new List<DbUpdDVIRDefectUpdate2> { dbUpdDVIRDefectUpdate2 }, cancellationTokenSource, Logging.LogLevel.Info);
                    await adapterUOW.CommitAsync();
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    await adapterUOW.RollBackAsync();
                    throw;
                }
            }, new Context());
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.DVIRLogManipulatorIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                await PrepareForExecutionAsync(stoppingToken);

                try
                {
                    var result = await ExecuteIterationBusinessLogicAsync(stoppingToken);

                    if (result.ShouldThrottle(ExecutionThrottleEngagingRecordCount))
                    {
                        await awaiter.WaitForConfiguredIntervalAsync(
                        delayTimeSpan, DelayIntervalType.Wait, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.Warn($"{CurrentClassName} process cancelled.");
                    throw;
                }
                catch (AdapterDatabaseConnectionException databaseConnectionException)
                {
                    HandleAdapterDatabaseConnectionException(databaseConnectionException);
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    HandleMyGeotabConnectionException(myGeotabConnectionException);
                }
                catch (Exception ex)
                {
                    HandleGenericException(ex);
                }
            }
        }

        /// <summary>
        /// Executes the business logic for a single iteration of <see cref="ExecuteAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        async Task<BatchProcessingResult> ExecuteIterationBusinessLogicAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Retrieve a batch of DbUpdDVIRDefectUpdate2 entities from the database.
            var dbUpdDVIRDefectUpdate2s = await GetDbUpdDVIRDefectUpdate2BatchAsync(stoppingToken);

            if (!dbUpdDVIRDefectUpdate2s.Any())
            {
                logger.Debug($"No records retrieved from upd_DVIRDefectUpdates2 table.");
                return new BatchProcessingResult {
                    TotalCount = 0,
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            logger.Info($"Retrieved {dbUpdDVIRDefectUpdate2s.Count()} records from upd_DVIRDefectUpdates2 table. Processing...");

            // Process the batch of DbUpdDVIRDefectUpdate2 entities.
            var result = await ProcessDbUpdDVIRDefectUpdate2BatchAsync(dbUpdDVIRDefectUpdate2s, stoppingToken);

            // Update DbOServiceTracking2:
            await UpdateServiceTrackingRecordAsync();

            if (result.FailureCount > 0)
            {
                // Use WARN log level so that anyone monitoring the log will also see the exceptions.
                logger.Warn($"Of the {result.TotalCount} records from the upd_DVIRDefectUpdates2 table, {result.SuccessCount} were successfully processed and {result.FailureCount} failed. Copies of any failed records have been inserted into the fail_DVIRDefectUpdateFailures2 table for reference.");
            }
            else
            {
                logger.Info($"Of the {result.TotalCount} records from the upd_DVIRDefectUpdates2 table, {result.SuccessCount} were successfully processed and {result.FailureCount} failed. Copies of any failed records have been inserted into the fail_DVIRDefectUpdateFailures2 table for reference.");
            }

            logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

            return result;
        }

        /// <summary>
        /// Retrieves a batch of <see cref="DbUpdDVIRDefectUpdate2"/> records from the adapter database.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<IEnumerable<DbUpdDVIRDefectUpdate2>> GetDbUpdDVIRDefectUpdate2BatchAsync(CancellationToken cancellationToken)

        {
            return await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var repo = new BaseRepository<DbUpdDVIRDefectUpdate2>(adapterContext);
                return await repo.GetAllAsync(cancellationTokenSource, DVIRDefectUpdateBatchSize);
            }, new Context());
        }

        /// <summary>
        /// Gets the <see cref="DVIRLog"/> and <see cref="DVIRDefect"/> associated with the <paramref name="dbUpdDVIRDefectUpdate2"/> from the MyGeotab database.
        /// </summary>
        /// <param name="dbUpdDVIRDefectUpdate2">The <see cref="DbUpdDVIRDefectUpdate2"/> that identifies the <see cref="DVIRLog"/> and <see cref="DVIRDefect"/> to retrieve from the MyGeotab database.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        async Task<(DVIRLog dvirLog, DVIRDefect dvirDefect)> GetDVIRLogAndDVIRDefectAsync(DbUpdDVIRDefectUpdate2 dbUpdDVIRDefectUpdate2)
        {
            // Query the MyGeotab database for the target DVIRLog.
            var dvirLogResults = await myGeotabAPIHelper.GetAsync<DVIRLog>(new DVIRLogSearch { Id = Id.Create((Guid)dbUpdDVIRDefectUpdate2.DVIRLogId) }, adapterConfiguration.TimeoutSecondsForMyGeotabTasks);

            if (!dvirLogResults.Any())
            {
                throw new InvalidOperationException(
                    $"No DVIRLog with Id '{dbUpdDVIRDefectUpdate2.DVIRLogId}' exists in the MyGeotab database.");
            }

            // Get the DVIRLog and DVIRDefect from the results retrieved from the MyGeotab database.
            var dvirLog = dvirLogResults.First();
            var dvirDefect = dvirLog.DVIRDefects?
                .FirstOrDefault(dvirDefect => dvirDefect.Id == Id.Create((Guid)dbUpdDVIRDefectUpdate2.DVIRDefectId));

            if (dvirDefect == null)
            {
                throw new InvalidOperationException(
                    $"No DVIRDefect with Id '{dbUpdDVIRDefectUpdate2.DVIRDefectId}' is associated with DVIRLog '{dbUpdDVIRDefectUpdate2.DVIRLogId}' in the MyGeotab database.");
            }

            return (dvirLog, dvirDefect);
        }

        void HandleAdapterDatabaseConnectionException(AdapterDatabaseConnectionException ex)
        {
            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
            stateMachine.HandleException(ex, NLogLogLevelName.Error);
        }

        /// <summary>
        /// Creates a new <see cref="DbFailDVIRDefectUpdateFailure2"/> using information from the <paramref name="exception"/>. Then inserts the DbFailDVIRDefectUpdateFailure2 record and deletes the record associated with the <paramref name="dbUpdDVIRDefectUpdate2"/> from the adapter database.
        /// </summary>
        /// <param name="dbUpdDVIRDefectUpdate2">The <see cref="DbUpdDVIRDefectUpdate2"/> that failed and is to be deleted (and replaced with a <see cref="DbFailDVIRDefectUpdateFailure2"/>).</param>
        /// <param name="exception">The <see cref="Exception"/> indicating the reason why <paramref name="dbUpdDVIRDefectUpdate2"/> failed to be updated.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task HandleFailedDbUpdDVIRDefectUpdate2Async(DbUpdDVIRDefectUpdate2 dbUpdDVIRDefectUpdate2, Exception exception, CancellationToken cancellationToken)
        {
            // Create a DbFailDVIRDefectUpdateFailure2 (failure record).
            var dbFailDVIRDefectUpdateFailure2 = dbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper.CreateEntity(dbUpdDVIRDefectUpdate2, DVIRLogUpdateProcessingResult.CreateFailure(exception).FailureReason);

            // Mark the DbUpdDVIRDefectUpdate2 for deletion.
            dbUpdDVIRDefectUpdate2.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Delete;

            // Persist changes to database.
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                try
                {
                    // DbFailDVIRDefectUpdateFailure2:
                    await dbFailDVIRDefectUpdateFailure2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, new List<DbFailDVIRDefectUpdateFailure2> { dbFailDVIRDefectUpdateFailure2 }, cancellationTokenSource, Logging.LogLevel.Info);

                    // DbUpdDVIRDefectUpdate2:
                    await dbUpdDVIRDefectUpdate2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, new List<DbUpdDVIRDefectUpdate2> { dbUpdDVIRDefectUpdate2 }, cancellationTokenSource, Logging.LogLevel.Info);

                    await adapterUOW.CommitAsync();
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    await adapterUOW.RollBackAsync();
                    throw;
                }
            }, new Context());
        }

        void HandleMyGeotabConnectionException(MyGeotabConnectionException ex)
        {
            exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
            stateMachine.HandleException(ex, NLogLogLevelName.Error);
        }

        void HandleGenericException(Exception ex)
        {
            exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
            stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
        }

        /// <summary>
        /// Handles waiting for prerequisite services and/or database maintenance if required prior to continuing.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        async Task PrepareForExecutionAsync(CancellationToken stoppingToken)
        {
            var prerequisiteServices = new List<AdapterService> 
            {
                AdapterService.DatabaseMaintenanceService2,
                AdapterService.DVIRLogProcessor2 
            };
            await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
            await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
        }

        /// <summary>
        /// Processes a batch of <see cref="DbUpdDVIRDefectUpdate2"/> entities.
        /// </summary>
        /// <param name="dbUpdDVIRDefectUpdate2s">The batch of <see cref="DbUpdDVIRDefectUpdate2"/> entities to be processed.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<BatchProcessingResult> ProcessDbUpdDVIRDefectUpdate2BatchAsync(IEnumerable<DbUpdDVIRDefectUpdate2> dbUpdDVIRDefectUpdate2s, CancellationToken cancellationToken)
        {
            int totalCount = dbUpdDVIRDefectUpdate2s.Count();
            int successCount = 0;
            int failureCount = 0;

            foreach (var dbUpdDVIRDefectUpdate2 in dbUpdDVIRDefectUpdate2s)
            {
                var result = await ProcessSingleDbUpdDVIRDefectUpdate2Async(dbUpdDVIRDefectUpdate2, cancellationToken);

                if (result.Success == true)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                }
            }

            return new BatchProcessingResult
            {
                TotalCount = totalCount,
                SuccessCount = successCount,
                FailureCount = failureCount
            };
        }

        /// <summary>
        /// Processes a single <see cref="DbUpdDVIRDefectUpdate2"/> entity.
        /// </summary>
        /// <param name="dbUpdDVIRDefectUpdate2">The <see cref="DbUpdDVIRDefectUpdate2"/> entity to be processed.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<DVIRLogUpdateProcessingResult> ProcessSingleDbUpdDVIRDefectUpdate2Async(DbUpdDVIRDefectUpdate2 dbUpdDVIRDefectUpdate2, CancellationToken cancellationToken)
        {
            try
            {
                // Get the DVIRLog and DVIRDefect entities to be updated.
                var (dvirLogToUpdate, dvirDefectToUpdate) = await GetDVIRLogAndDVIRDefectAsync(dbUpdDVIRDefectUpdate2);

                // Apply updates to the DVIRDefect.
                var dvirDefectWasUpdated = ApplyUpdatesToDVIRDefect(dvirDefectToUpdate, dbUpdDVIRDefectUpdate2);

                if (!dvirDefectWasUpdated)
                {
                    throw new ArgumentException("No action to take - insufficient data provided to either add a DefectRemark or update the RepairStatus of the subject DVIRDefect.");
                }

                // Update the DVIRLog in the MyGeotab database.
                await UpdateDVIRLogInMyGeotabDatabaseAsync(dvirLogToUpdate);

                // Delete the processed DbUpdDVIRDefectUpdate2 record from the adapter database.
                await DeleteProcessedDbUpdDVIRDefectUpdate2Async(dbUpdDVIRDefectUpdate2, cancellationToken);

                return DVIRLogUpdateProcessingResult.CreateSuccess();
            }
            catch (MyGeotabConnectionException)
            {
                // Re-throw connection exceptions to be handled at a higher level
                throw;
            }
            catch (Exception ex)
            {
                await HandleFailedDbUpdDVIRDefectUpdate2Async(dbUpdDVIRDefectUpdate2, ex, cancellationToken);
                return DVIRLogUpdateProcessingResult.CreateFailure(ex);
            }
        }

        /// <summary>
        /// Starts the current <see cref="DVIRLogManipulator2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DVIRLogManipulator2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DVIRLogManipulator2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
                        await adapterUOW.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                        await adapterUOW.RollBackAsync();
                        throw;
                    }
                }
            }, new Context());

            // Register this service with the StateMachine. Set mustPauseForDatabaseMaintenance to true if the service is enabled or false otherwise.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(DVIRLogManipulator2), adapterConfiguration.EnableDVIRLogManipulator);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableDVIRLogManipulator == true)
            {
                logger.Info($"******** STARTING SERVICE: {CurrentClassName}");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Warn($"******** WARNING - SERVICE DISABLED: The {CurrentClassName} service has not been enabled and will NOT be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="DVIRLogManipulator2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(DVIRLogManipulator2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the repair status properties of the <paramref name="dvirDefect"/> using properties of the <paramref name="dbUpdDVIRDefectUpdate2"/>.
        /// </summary>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> to be updated.</param>
        /// <param name="dbUpdDVIRDefectUpdate2">The <see cref="DbUpdDVIRDefectUpdate2"/> from which to obtain the property values for the <paramref name="dvirDefect"/> update.</param>
        /// <returns></returns>
        bool UpdateDVIRDefectRepairStatus(DVIRDefect dvirDefect, DbUpdDVIRDefectUpdate2 dbUpdDVIRDefectUpdate2)
        {
            // Check whether the DbUpdDVIRDefectUpdate2 repair status properties are populated.
            if (dbUpdDVIRDefectUpdate2.RepairDateTimeUtc == null && dbUpdDVIRDefectUpdate2.RepairStatusId == null && dbUpdDVIRDefectUpdate2.RepairUserId == null)
            {
                // No repair status update.
                return false;
            }

            // Proceed only if the DVIRDefect's RepairStatus has not already been set.
            if (dvirDefect.RepairStatus != null && dvirDefect.RepairStatus != RepairStatusType.NotRepaired)
            {
                throw new ArgumentException($"RepairStatus of the DVIRDefect has already been set and cannot be changed.");
            }

            // Ensure all repair status properties have values.
            if (dbUpdDVIRDefectUpdate2.RepairDateTimeUtc == null || dbUpdDVIRDefectUpdate2.RepairStatusId == null || dbUpdDVIRDefectUpdate2.RepairUserId == null)
            {
                throw new ArgumentException($"One or more related properties (RepairDateTimeUtc, RepairStatusId, or RepairUserId) of the DbUpdDVIRDefectUpdate2 object are null.");
            }

            RepairStatusType updateRepairStatusType = dbUpdDVIRDefectUpdate2.RepairStatusId switch
            {
                (short)RepairStatusType.Repaired => RepairStatusType.Repaired,
                (short)RepairStatusType.NotNecessary => RepairStatusType.NotNecessary,
                _ => throw new ArgumentException(
                    $"Invalid RepairStatusId value '{dbUpdDVIRDefectUpdate2.RepairStatusId}'. RepairStatus must be {RepairStatusType.Repaired} ('Repaired') or {RepairStatusType.NotNecessary} ('NotNecessary').")
            };

            // Update the repair status.
            var updateRepairUser = new User { Id = Id.Create((long)dbUpdDVIRDefectUpdate2.RepairUserId) };
            dvirDefect.RepairDateTime = dbUpdDVIRDefectUpdate2.RepairDateTimeUtc;
            dvirDefect.RepairStatus = updateRepairStatusType;
            dvirDefect.RepairUser = updateRepairUser;
            return true;
        }

        /// <summary>
        /// Updates a <see cref="DVIRLog"/> in the MyGeotab database.
        /// </summary>
        /// <param name="dvirLogToUpdate">The <see cref="DVIRLog"/> to be updated in the MyGeotab database.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if an exception is encountered in the Set<DVIRLog> call.</exception>
        async Task UpdateDVIRLogInMyGeotabDatabaseAsync(DVIRLog dvirLogToUpdate)
        {
            try
            {
                await myGeotabAPIHelper.SetAsync<DVIRLog>(dvirLogToUpdate, TimeoutSecondsForMyGeotabSetCalls);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("MyGeotab Exception encountered on Set<DVIRLog> call.", ex);
            }
        }

        /// <summary>
        /// Updates the <see cref="DbOServiceTracking2"/> record associated with the <see cref="DVIRLogManipulator2"/> service to reflect that entities have last been processed at the current time.
        /// </summary>
        /// <returns></returns>
        async Task UpdateServiceTrackingRecordAsync()
        {
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase);
                try
                {
                    await serviceTracker.UpdateDbOServiceTrackingRecordAsync(
                        adapterContext, AdapterService.DVIRLogManipulator2, DateTime.UtcNow);
                    await adapterUOW.CommitAsync();
                }
                catch (Exception ex)
                {
                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    await adapterUOW.RollBackAsync();
                    throw;
                }
            }, new Context());
        }
    }
}
