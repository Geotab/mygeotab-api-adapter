using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Logic;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Utilities;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A worker service that propagates <see cref="DVIRLog"/> changes from tables in the adapter database to the associated MyGeotab database. This service runs in parallel to the main <see cref="Worker"/> service if <see cref="ConfigurationManager.EnableDVIRLogManipulator"/> is set to <c>true</c>. 
    /// </summary>
    class DVIRLogManipulator : BackgroundService
    {
        const int ConnectivityRestorationCheckIntervalMilliseconds = 10000;
        const int DVIRDefectUpdateBatchSize = 5000;
        const int MyGeotabAPIAuthenticationCheckIntervalMilliseconds = 1000;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IConfiguration configuration;
        ConnectionInfo connectionInfo;
        bool initializationCompleted;
        DateTime lastIterationStartTimeUtc;
        int commandTimeout = 0;

        /// <summary>
        /// Instantiates a new instance of the <see cref="DVIRLogManipulator"/> class.
        /// </summary>
        public DVIRLogManipulator(IConfiguration configuration)
        {
            this.configuration = configuration;
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Adds a <see cref="DefectRemark"/> to the <paramref name="dvirDefect"/> using properties of the <paramref name="dbDVIRDefectUpdate"/>.
        /// </summary>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> to which a <see cref="DefectRemark"/> is to be added.</param>
        /// <param name="dbDVIRDefectUpdate">The <see cref="DbDVIRDefectUpdate"/> from which to obtain the property values for the <see cref="DefectRemark"/> that is to be added to the <paramref name="dvirDefect"/>.</param>
        /// <returns></returns>
        bool AddDefectRemark(DVIRDefect dvirDefect, DbDVIRDefectUpdate dbDVIRDefectUpdate)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Check whether the DbDVIRDefectUpdate includes a remark to be added.
            if (dbDVIRDefectUpdate.Remark == null && dbDVIRDefectUpdate.RemarkDateTime == null && dbDVIRDefectUpdate.RemarkUserId == null)
            {
                // No remark to add.
                return false;
            }

            // Ensure all remark-related properties have values.
            if (dbDVIRDefectUpdate.Remark == null || dbDVIRDefectUpdate.RemarkDateTime == null || dbDVIRDefectUpdate.RemarkUserId == null)
            {
                throw new ArgumentException($"Cannot add DefectRemark to DVIRDefect because one or more DefectRemark-related properties (Remark, RemarkDateTime, or RemarkUserId) of the DbDVIRDefectUpdate object are null.");
            }

            // Add the remark.
            DefectRemark defectRemark = new()
            {
                User = new User { Id = Id.Create(dbDVIRDefectUpdate.RemarkUserId) },
                DateTime = dbDVIRDefectUpdate.RemarkDateTime,
                Remark = dbDVIRDefectUpdate.Remark
            };
            if (dvirDefect.DefectRemarks == null)
            {
                dvirDefect.DefectRemarks = new List<DefectRemark>();
            }
            dvirDefect.DefectRemarks.Add(defectRemark);
            
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return true;
        }

        /// <summary>
        /// Disposes the current <see cref="DVIRLogManipulator"/> instance.
        /// </summary>
        public override void Dispose()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            base.Dispose();
        }

        /// <summary>
        /// Iteratively executes the business logic until the application is stopped.
        /// </summary>
        /// <param name="stoppingToken">The <see cref="CancellationToken"/> that can be used to stop execution of the application.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // This is the loop containing all of the business logic that is executed iteratively throughout the lifeime of the application.
            while (!stoppingToken.IsCancellationRequested)
            {
                // Abort if waiting for restoration of connectivity to the MyGeotab server or to the database.
                if (StateMachine.CurrentState == State.Waiting)
                {
                    continue;
                }

                if (initializationCompleted == false)
                {
                    PerformInitializationTasks();
                    continue;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    // Only proceed if the DVIRLogManipulatorIntervalSeconds has elapsed since the last iteration was initiated.
                    if (Globals.TimeIntervalHasElapsed(lastIterationStartTimeUtc, Globals.DateTimeIntervalType.Seconds, Globals.ConfigurationManager.DVIRLogManipulatorIntervalSeconds))
                    {
                        lastIterationStartTimeUtc = DateTime.UtcNow;

                        using (var cancellationTokenSource = new CancellationTokenSource())
                        {
                            try
                            {
                                // Retrieve a batch of DbDVIRDefectUpdate entities from the database.
                                var dbDVIRDefectUpdates = await DbDVIRDefectUpdateService.GetAllAsync(connectionInfo, commandTimeout, DVIRDefectUpdateBatchSize);
                                int dbDVIRDefectUpdateCount = dbDVIRDefectUpdates.Count();
                                int processedDbDVIRDefectUpdateCount = 0;
                                int failedDbDVIRDefectUpdateCount = 0;

                                if (dbDVIRDefectUpdates.Any())
                                {
                                    logger.Info($"Retrieved {dbDVIRDefectUpdateCount} records from DVIRDefectUpdates table. Processing...");
                                }
                                else
                                {
                                    logger.Debug($"No records retrieved from DVIRDefectUpdates table.");
                                    continue;
                                }

                                // Process each of the retrieved DbDVIRDefectUpdate entities.
                                foreach (var dbDVIRDefectUpdate in dbDVIRDefectUpdates)
                                {
                                    try
                                    {
                                        // Get the subject DVIRLog from the MyGeotab database.
                                        var dvirLogResultList = await MyGeotabApiUtility.GetAsync<DVIRLog>(Globals.MyGeotabAPI, new DVIRLogSearch { Id = Id.Create(dbDVIRDefectUpdate.DVIRLogId) });
                                        if (dvirLogResultList.Any())
                                        {
                                            var dvirLogToUpdate = dvirLogResultList.First();
                                            // Get the subject DVIRDefect to be updated.
                                            var dvirDefectToUpdate = dvirLogToUpdate.DVIRDefects.Where(dvirDefect => dvirDefect.Id == Id.Create(dbDVIRDefectUpdate.DVIRDefectId)).FirstOrDefault();
                                            if (dvirDefectToUpdate == null)
                                            {
                                                throw new ArgumentException($"No DVIRDefect with the sepecified Id is associated with the subject DVIRLog in the MyGeotab database.");
                                            }
                                            else
                                            {
                                                bool defectRemarkAddedToDVIRDefectToUpdate = AddDefectRemark(dvirDefectToUpdate, dbDVIRDefectUpdate);
                                                bool repairStatusUpdatedOnDVIRDefectToUpdate = UpdateDVIRDefectRepairStatus(dvirDefectToUpdate, dbDVIRDefectUpdate);

                                                if (defectRemarkAddedToDVIRDefectToUpdate == false && repairStatusUpdatedOnDVIRDefectToUpdate == false)
                                                {
                                                    throw new ArgumentException($"No action to take because insufficent data was provided to either add a DefectRemark or update the RepairStatus of the subject DVIRDefect.");
                                                }

                                                var deleteCurrentDbDVIRDefectUpdateRecord = false;
                                                try
                                                {
                                                    // Update the DVIRLog in the MyGeotab database.
                                                    var result = await MyGeotabApiUtility.SetAsync<DVIRLog>(Globals.MyGeotabAPI, dvirLogToUpdate);
                                                    deleteCurrentDbDVIRDefectUpdateRecord = true;
                                                    processedDbDVIRDefectUpdateCount += 1;
                                                }
                                                catch (MyGeotabConnectionException)
                                                {
                                                    // Pass the MyGeotabConnectionException along so that the connectivity loss can be addressed and the subject DbDVIRDefectUpdate can be re-tried once connectivity is restored.
                                                    throw;
                                                }
                                                catch (Exception exception)
                                                {
                                                    // Flag the current DbDVIRDefectUpdateRecord for deletion since this is not a connectivity-related exception and the same exception could occur repeatedly if the adapter is restarted and this record remains in the adapter database.
                                                    deleteCurrentDbDVIRDefectUpdateRecord = true;
                                                    throw new Exception($"MyGeotab Exception encountered on Set<DVIRLog> call.", exception);
                                                }
                                                finally
                                                {
                                                    if (deleteCurrentDbDVIRDefectUpdateRecord == true)
                                                    {
                                                        // Delete the current DbDVIRDefectUpdate from the DVIRDefectUpdates table.
                                                        await DbDVIRDefectUpdateService.DeleteAsync(connectionInfo, dbDVIRDefectUpdate, commandTimeout);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            throw new Exception($"No DVIRLog with the specified Id exists in the MyGeotab database.");
                                        }
                                    }
                                    catch (Exception dvirLogUpdateException)
                                    {
                                        try
                                        {
                                            failedDbDVIRDefectUpdateCount += 1;
                                            var failureMessage = GenerateMessageForDVIRLogUpdateException(dvirLogUpdateException);

                                            // Insert a record into the FailedDVIRDefectUpdates table.
                                            DbFailedDVIRDefectUpdate dbFailedDVIRDefectUpdate = ObjectMapper.GetDbFailedDVIRDefectUpdate(dbDVIRDefectUpdate, failureMessage);
                                            dbFailedDVIRDefectUpdate.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;
                                            dbFailedDVIRDefectUpdate.RecordCreationTimeUtc = DateTime.UtcNow;
                                            await DbFailedDVIRDefectUpdateService.InsertAsync(connectionInfo, dbFailedDVIRDefectUpdate, commandTimeout);
                                            // Delete the current DbDVIRDefectUpdate from the DVIRDefectUpdates table.
                                            await DbDVIRDefectUpdateService.DeleteAsync(connectionInfo, dbDVIRDefectUpdate, commandTimeout);
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new Exception($"Exception encountered while writing to {ConfigurationManager.DbFailedDVIRDefectUpdatesTableName} table or deleting from {ConfigurationManager.DbDVIRDefectUpdatesTableName} table.", ex);
                                        }
                                    }
                                }

                                logger.Info($"Of the {dbDVIRDefectUpdateCount} records from DVIRDefectUpdates table, {processedDbDVIRDefectUpdateCount} were successfully processed and {failedDbDVIRDefectUpdateCount} failed. Copies of any failed records have been inserted into the FailedDVIRDefectUpdates table for reference.");
                            }
                            catch (TaskCanceledException taskCanceledException)
                            {
                                string errorMessage = $"Task was cancelled. TaskCanceledException: \nMESSAGE [{taskCanceledException.Message}]; \nSOURCE [{taskCanceledException.Source}]; \nSTACK TRACE [{taskCanceledException.StackTrace}]";
                                logger.Warn(errorMessage);
                            }
                            catch (Exception)
                            {
                                cancellationTokenSource.Cancel();
                                throw;
                            }
                        }
                    }
                    else
                    {
                        logger.Debug($"Propagation of DVIRLog changes from tables in the adapter database to the associated MyGeotab database has not been initiated on this iteration; {Globals.ConfigurationManager.DVIRLogManipulatorIntervalSeconds} seconds have not passed since the process was last initiated.");
                    }

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"{nameof(DVIRLogManipulator)} process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (DatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLog.LogLevel.Error, $"{nameof(DVIRLogManipulator)} process caught an exception");
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    HandleException(myGeotabConnectionException, NLog.LogLevel.Error, $"{nameof(DVIRLogManipulator)} process caught an exception");
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLog.LogLevel.Fatal, $"******** {nameof(DVIRLogManipulator)} process caught an unhandled exception and will self-terminate now.");
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Generates the message for an <see cref="Exception"/> related to <see cref="DVIRLog"/> updates. 
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <returns>The generated error message.</returns>
        static string GenerateMessageForDVIRLogUpdateException(Exception exception)
        {
            string errorMessage;
            string innerExceptionMessage = "";
            if (exception.InnerException != null)
            {
                Exception innerException = exception.InnerException;
                innerExceptionMessage = $" MESSAGE: [{innerException.Message}]";
            }

            errorMessage = $"{exception.Message}{innerExceptionMessage}";
            return errorMessage;
        }

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>. If the <paramref name="exception"/> is a <see cref="MyGeotabConnectionException"/> or a <see cref="DatabaseConnectionException"/>, the <see cref="WaitForConnectivityRestorationAsync(StateReason)"/> method will be executed after logging the error message.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="errorMessageLogLevel">The <see cref="NLog.LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        void HandleException(Exception exception, NLog.LogLevel errorMessageLogLevel, string errorMessagePrefix = "An exception was encountered")
        {
            Globals.LogException(exception, errorMessageLogLevel, errorMessagePrefix);

            if (exception is MyGeotabConnectionException)
            {
                _ = WaitForConnectivityRestorationAsync(StateReason.MyGeotabNotAvailable);
            }
            if (exception is DatabaseConnectionException)
            {
                _ = WaitForConnectivityRestorationAsync(StateReason.DatabaseNotAvailable);
            }
        }

        /// <summary>
        /// Performs startup tasks.
        /// </summary>
        /// <returns></returns>
        void PerformInitializationTasks()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            try
            {
                // Setup the ConfigurationManager Globals reference
                connectionInfo = new ConnectionInfo(Globals.ConfigurationManager.DatabaseConnectionString, Globals.ConfigurationManager.DatabaseProviderType, Databases.AdapterDatabase);

                // Allow longer database command timeout because another process could be writing a batch of records to the DVIRDefectUpdates table and this could cause a bit of delay.
                commandTimeout = Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks * 2;

                // Wait for the main Worker to complete authentication of the MyGeotab API.
                bool myGeotabAPIAuthenticated = false;
                do
                {
                    Task.Delay(MyGeotabAPIAuthenticationCheckIntervalMilliseconds);
                    if (Globals.MyGeotabAPI != null && Globals.MyGeotabAPI.SessionId != null)
                    {
                        myGeotabAPIAuthenticated = true;
                    }
                } while (myGeotabAPIAuthenticated == false);

                initializationCompleted = true;
                logger.Info("Initialization completed.");
            }
            catch (DatabaseConnectionException databaseConnectionException)
            {
                HandleException(databaseConnectionException, NLog.LogLevel.Error, $"{nameof(DVIRLogManipulator)} process caught an exception");
            }
            catch (Exception ex)
            {
                string errorMessage = $"{nameof(DVIRLogManipulator)} process caught an exception: \nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
                logger.Error(errorMessage);
                throw new Exception(errorMessage, ex);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Starts the current <see cref="DVIRLogManipulator"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Only start if the DVIRLogManipulator is enabled.
            if (Globals.ConfigurationManager.EnableDVIRLogManipulator == true)
            {
                logger.Info($"Starting {nameof(DVIRLogManipulator)}.");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Info($"DVIRLogManipulator has not been enabled, so {nameof(DVIRLogManipulator)} will not be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="DVIRLogManipulator"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info($"{nameof(DVIRLogManipulator)} stopped.");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the repair status properties of the <paramref name="dvirDefect"/> using properties of the <paramref name="dbDVIRDefectUpdate"/>.
        /// </summary>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> to be updated.</param>
        /// <param name="dbDVIRDefectUpdate">The <see cref="DbDVIRDefectUpdate"/> from which to obtain the property values for the <paramref name="dvirDefect"/> update.</param>
        /// <returns></returns>
        bool UpdateDVIRDefectRepairStatus(DVIRDefect dvirDefect, DbDVIRDefectUpdate dbDVIRDefectUpdate)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Check whether the DbDVIRDefectUpdate repair status properties are populated.
            if (dbDVIRDefectUpdate.RepairDateTime == null && dbDVIRDefectUpdate.RepairStatus == null && dbDVIRDefectUpdate.RepairUserId == null)
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
            if (dbDVIRDefectUpdate.RepairDateTime == null || dbDVIRDefectUpdate.RepairStatus == null || dbDVIRDefectUpdate.RepairUserId == null)
            {
                throw new ArgumentException($"One or more related properties (RepairDateTime, RepairStatus, or RepairUserId) of the DbDVIRDefectUpdate object are null.");
            }

            var updateRepairStatusType = dbDVIRDefectUpdate.RepairStatus switch
            {
                nameof(RepairStatusType.NotNecessary) => RepairStatusType.NotNecessary,
                nameof(RepairStatusType.Repaired) => RepairStatusType.Repaired,
                _ => throw new ArgumentException($"Invalid RepairStatus value. RepairStatus must be set to either 'Repaired' or 'NotNecessary'."),
            };

            // Update the repair status.
            var updateRepairUser = new User { Id = Id.Create(dbDVIRDefectUpdate.RepairUserId) };
            dvirDefect.RepairDateTime = dbDVIRDefectUpdate.RepairDateTime;
            dvirDefect.RepairStatus = updateRepairStatusType;
            dvirDefect.RepairUser = updateRepairUser;

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
            return true;
        }

        /// <summary>
        /// Iteratively tests MyGeotab or database connectivity, depending on which was lost, until connectivity is restored.
        /// </summary>
        /// <param name="reasonForConnectivityLoss">The reason for loss of connectivity.</param>
        /// <returns></returns>        
        async Task WaitForConnectivityRestorationAsync(StateReason reasonForConnectivityLoss)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            StateMachine.CurrentState = State.Waiting;
            StateMachine.Reason = reasonForConnectivityLoss;
            logger.Warn($"******** CONNECTIVITY LOST. REASON: '{StateMachine.Reason}'. WAITING FOR RESTORATION OF CONNECTIVITY...");
            while (StateMachine.CurrentState == State.Waiting)
            {
                // Wait for the prescribed interval between connectivity checks.
                await Task.Delay(ConnectivityRestorationCheckIntervalMilliseconds);

                logger.Warn($"{StateMachine.Reason}; continuing to wait for restoration of connectivity...");
                switch (StateMachine.Reason)
                {
                    case StateReason.DatabaseNotAvailable:
                        if (await StateMachine.IsDatabaseAccessibleAsync(connectionInfo) == true)
                        {
                            logger.Warn("******** CONNECTIVITY RESTORED.");
                            StateMachine.CurrentState = State.Normal;
                            continue;
                        }
                        break;
                    case StateReason.MyGeotabNotAvailable:
                        if (await StateMachine.IsMyGeotabAccessibleAsync() == true)
                        {
                            logger.Warn("******** CONNECTIVITY RESTORED.");
                            StateMachine.CurrentState = State.Normal;
                            continue;
                        }
                        break;
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
