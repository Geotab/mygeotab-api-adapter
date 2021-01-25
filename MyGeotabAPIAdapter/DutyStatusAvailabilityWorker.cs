using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Engine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Logic;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A worker service that acts as a data "feed" to populate the DutyStatusAvailability table in the adapter database. This service runs in parallel to the main <see cref="Worker"/> service if <see cref="ConfigurationManager.enableDutyStatusAvailabilityDataFeed"/> is set to <c>true</c>. 
    /// 
    /// The Geotab API's GetFeed method does not support the <see cref="Geotab.Checkmate.ObjectModel.DutyStatusAvailability"/> entity type and the results for DutyStatusAvailability Get requests are calculated dynamically, resulting in longer response times than are typical for pre-calculated data. It is also necessary to retrieve DutyStatusAvailability on a per-driver basis using batches of Get requests wrapped in <see cref="Geotab.Checkmate.API.MultiCallAsync(object[])"/> requests (in order to support larger fleets where the number of Get<DutyStatusAvailability> requests required to cover all drivers could not be made in a single MultiCall request). The result of the combination of these factors is that it can take some time for DutyStatusAvailability to be retrieved for all drivers in a fleet. In order to avoid slowing the flow of data for other feeds, DutyStatusAvailability is handled in this separate worker service running in parallel to the main <see cref="Worker"/> service.
    /// </summary>
    class DutyStatusAvailabilityWorker : BackgroundService
    {
        const int MyGeotabAPIAuthenticationCheckIntervalMilliseconds = 1000;
        const string HosRuleSetNoneValue = "None";
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IConfiguration configuration;
        ConnectionInfo connectionInfo;
        IDictionary<Id, DbDutyStatusAvailability> dbDutyStatusAvailabilityDictionary;
        bool initializationCompleted;
        DateTime lastDutyStatusAvailabilityDataRetrievalStartTimeUtc;

        /// <summary>
        /// Instantiates a new instance of the <see cref="DutyStatusAvailabilityWorker"/> class.
        /// </summary>
        public DutyStatusAvailabilityWorker(IConfiguration configuration)
        {
            this.configuration = configuration;
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Disposes the current <see cref="DutyStatusAvailabilityWorker"/> instance.
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

                    // Only proceed with data retrieval if the DutyStatusAvailabilityFeedIntervalSeconds has elapsed since data retrieval was last initiated.
                    if (Globals.TimeIntervalHasElapsed(lastDutyStatusAvailabilityDataRetrievalStartTimeUtc, Globals.DateTimeIntervalType.Seconds, Globals.ConfigurationManager.DutyStatusAvailabilityFeedIntervalSeconds))
                    {
                        lastDutyStatusAvailabilityDataRetrievalStartTimeUtc = DateTime.UtcNow;

                        using (var cancellationTokenSource = new CancellationTokenSource())
                        {
                            try
                            {
                                // Get list of active users that are drivers who have accessed the MyGeotab system within the last 30 days and have HosRuleSets assigned.
                                var dbUsers = await DbUserService.GetAllAsync(connectionInfo, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                                var dutyStatusAvailabilityFeedLastAccessDateCutoffDays = TimeSpan.FromDays(Globals.ConfigurationManager.DutyStatusAvailabilityFeedLastAccessDateCutoffDays);
                                DateTime cutoffLastAccessedTime = DateTime.UtcNow.Subtract(dutyStatusAvailabilityFeedLastAccessDateCutoffDays);
                                var driverDbUsers = dbUsers.Where(dbUser => dbUser.IsDriver == true && dbUser.ActiveTo >= DateTime.UtcNow && dbUser.LastAccessDate >= cutoffLastAccessedTime && dbUser.HosRuleSet != HosRuleSetNoneValue).ToList();

                                const int maxBatchSize = 100;
                                int currentBatchSize = 0;
                                int driverDbUserCount = driverDbUsers.Count();
                                var calls = new List<object>();
                                for (int driverDbUserListIndex = 0; driverDbUserListIndex < driverDbUserCount + 1; driverDbUserListIndex++)
                                {
                                    if (currentBatchSize == maxBatchSize || driverDbUserListIndex == driverDbUserCount)
                                    {
                                        DateTime recordChangedTimestampUtc = DateTime.UtcNow;
                                        var dbDutyStatusAvailabilityEntitiesToInsert = new List<DbDutyStatusAvailability>();
                                        var dbDutyStatusAvailabilityEntitiesToUpdate = new List<DbDutyStatusAvailability>();

                                        // Execute MultiCall.
                                        var results = await Globals.MyGeotabAPI.MultiCallAsync(calls.ToArray());

                                        // Iterate through the returned DutyStatusAvailability entities.
                                        foreach (var result in results)
                                        {
                                            if (result is List<DutyStatusAvailability> resultDutyStatusAvailabilityList && resultDutyStatusAvailabilityList.Count > 0)
                                            {
                                                var dutyStatusAvailability = resultDutyStatusAvailabilityList[0];
                                                var dutyStatusAvailabilityDriver = dutyStatusAvailability.Driver;
                                                // Try to find the existing database record for DutyStatusAvailability associated with the subject Driver.
                                                if (dbDutyStatusAvailabilityDictionary.TryGetValue(dutyStatusAvailabilityDriver.Id, out var existingDbDutyStatusAvailability))
                                                {
                                                    // The database already contains a DutyStatusAvailability record for the subject Driver.
                                                    DbDutyStatusAvailability updatedDbDutyStatusAvailability = ObjectMapper.GetDbDutyStatusAvailability(dutyStatusAvailability);
                                                    updatedDbDutyStatusAvailability.id = existingDbDutyStatusAvailability.id;
                                                    updatedDbDutyStatusAvailability.RecordLastChangedUtc = recordChangedTimestampUtc;
                                                    updatedDbDutyStatusAvailability.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Update;
                                                    dbDutyStatusAvailabilityDictionary[Id.Create(updatedDbDutyStatusAvailability.DriverId)] = updatedDbDutyStatusAvailability;
                                                    dbDutyStatusAvailabilityEntitiesToUpdate.Add(updatedDbDutyStatusAvailability);
                                                }
                                                else
                                                {
                                                    // A DutyStatusAvailability record associated with the subject Driver has not yet been added to the database. Create a DbDutyStatusAvailability, set its properties and add it to the cache.
                                                    DbDutyStatusAvailability newDbDutyStatusAvailability = ObjectMapper.GetDbDutyStatusAvailability(dutyStatusAvailability);
                                                    newDbDutyStatusAvailability.RecordLastChangedUtc = recordChangedTimestampUtc;
                                                    newDbDutyStatusAvailability.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;
                                                    dbDutyStatusAvailabilityDictionary.Add(Id.Create(newDbDutyStatusAvailability.DriverId), newDbDutyStatusAvailability);
                                                    dbDutyStatusAvailabilityEntitiesToInsert.Add(newDbDutyStatusAvailability);
                                                }
                                            }
                                        }

                                        // Send any inserts to the database.
                                        if (dbDutyStatusAvailabilityEntitiesToInsert.Any())
                                        {
                                            try
                                            {
                                                DateTime startTimeUTC = DateTime.UtcNow;
                                                long dbDutyStatusAvailabilityEntitiesInserted = await DbDutyStatusAvailabilityService.InsertAsync(connectionInfo, dbDutyStatusAvailabilityEntitiesToInsert, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                                                TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                                                double recordsProcessedPerSecond = (double)dbDutyStatusAvailabilityEntitiesInserted / (double)elapsedTime.TotalSeconds;
                                                logger.Info($"Completed insertion of {dbDutyStatusAvailabilityEntitiesInserted} records into {Globals.ConfigurationManager.DbDutyStatusAvailabilityTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                                            }
                                            catch (Exception)
                                            {
                                                cancellationTokenSource.Cancel();
                                                throw;
                                            }
                                        }

                                        // Send any updates/deletes to the database.
                                        if (dbDutyStatusAvailabilityEntitiesToUpdate.Any())
                                        {
                                            try
                                            {
                                                DateTime startTimeUTC = DateTime.UtcNow;
                                                long dbDutyStatusAvailabilityEntitiesUpdated = await DbDutyStatusAvailabilityService.UpdateAsync(connectionInfo, dbDutyStatusAvailabilityEntitiesToUpdate, cancellationTokenSource, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);
                                                TimeSpan elapsedTime = DateTime.UtcNow.Subtract(startTimeUTC);
                                                double recordsProcessedPerSecond = (double)dbDutyStatusAvailabilityEntitiesUpdated / (double)elapsedTime.TotalSeconds;
                                                logger.Info($"Completed updating of {dbDutyStatusAvailabilityEntitiesUpdated} records in {Globals.ConfigurationManager.DbDutyStatusAvailabilityTableName} table in {elapsedTime.TotalSeconds} seconds ({recordsProcessedPerSecond} per second throughput).");
                                            }
                                            catch (Exception)
                                            {
                                                cancellationTokenSource.Cancel();
                                                throw;
                                            }
                                        }


                                        // Clear calls list and reset counter.
                                        calls = new List<object>();
                                        currentBatchSize = 0;
                                    }
                                    if (driverDbUserListIndex == driverDbUserCount)
                                    {
                                        // All drivers have been processed.
                                        break;
                                    }
                                    // Generate Get<DutyStatusAvailability> call for current driver and add to list.
                                    var driverDbUserId = Id.Create(driverDbUsers[driverDbUserListIndex].GeotabId);
                                    var userSearch = new UserSearch
                                    {
                                        Id = driverDbUserId
                                    };
                                    calls.Add(new object[] { "Get", typeof(DutyStatusAvailability), new { search = new DutyStatusAvailabilitySearch { UserSearch = new UserSearch { Id = driverDbUserId } } }, typeof(List<DutyStatusAvailability>) });

                                    currentBatchSize++;
                                }
                            }
                            catch (TaskCanceledException taskCanceledException)
                            {
                                string errorMessage = $"Task was cancelled. TaskCanceledException: \nMESSAGE [{taskCanceledException.Message}]; \nSOURCE [{taskCanceledException.Source}]; \nSTACK TRACE [{taskCanceledException.StackTrace}]";
                                logger.Warn(errorMessage);
                            }
                        }
                    }
                    else
                    {
                        logger.Debug($"DutyStatusAvailability data retrieval not initiated; {Globals.ConfigurationManager.DutyStatusAvailabilityFeedIntervalSeconds} seconds have not passed since DutyStatusAvailability data retrieval was last initiated.");
                    }

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"{nameof(DutyStatusAvailabilityWorker)} process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (DatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLog.LogLevel.Error, $"{nameof(DutyStatusAvailabilityWorker)} process caught an exception");
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLog.LogLevel.Fatal, $"******** {nameof(DutyStatusAvailabilityWorker)} process caught an unhandled exception and will self-terminate now.");
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>. Does NOT implement connectivity restoration logic like <see cref="Worker.WaitForConnectivityRestorationAsync(StateReason)"/> since the <see cref="Worker"/> will already be taking care of it.
        /// /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="errorMessageLogLevel">The <see cref="NLog.LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        void HandleException(Exception exception, NLog.LogLevel errorMessageLogLevel, string errorMessagePrefix = "An exception was encountered")
        {
            Globals.LogException(exception, errorMessageLogLevel, errorMessagePrefix);
        }

        /// <summary>
        /// Retrieves lists of existing database entities of various types. Later, during data processing, MyGeotab objects are compared against these lists rather than executing select queries on a per-object basis - thereby reducing chattiness of the application and boosting performance. 
        /// </summary>
        /// <returns></returns>
        void InitializeListsOfExistingDatabaseEntities()
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    var getAllDbDutyStatusAvailabilityRecordsTask = DbDutyStatusAvailabilityService.GetAllAsync(connectionInfo, Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks);

                    Task[] tasks = { getAllDbDutyStatusAvailabilityRecordsTask };

                    Task.WaitAll(tasks);

                    // Sort lists on Id.
                    dbDutyStatusAvailabilityDictionary = getAllDbDutyStatusAvailabilityRecordsTask.Result.ToDictionary(dutyStatusAvailability => Id.Create(dutyStatusAvailability.DriverId));
                }
                catch (AggregateException aggregateException)
                {
                    Globals.HandleConnectivityRelatedAggregateException(aggregateException, Globals.ConnectivityIssueType.Database, "One or more exceptions were encountered during retrieval of lists of existing database entities due to an apparent loss of connectivity with the database.");
                }
                catch (TaskCanceledException taskCanceledException)
                {
                    string errorMessage = $"Task was cancelled. TaskCanceledException: \nMESSAGE [{taskCanceledException.Message}]; \nSOURCE [{taskCanceledException.Source}]; \nSTACK TRACE [{taskCanceledException.StackTrace}]";
                    logger.Warn(errorMessage);
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
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
                connectionInfo = new ConnectionInfo(Globals.ConfigurationManager.DatabaseConnectionString, Globals.ConfigurationManager.DatabaseProviderType);

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

                InitializeListsOfExistingDatabaseEntities();

                initializationCompleted = true;
                logger.Info("Initialization completed.");
            }
            catch (DatabaseConnectionException databaseConnectionException)
            {
                HandleException(databaseConnectionException, NLog.LogLevel.Error, $"{nameof(DutyStatusAvailabilityWorker)} process caught an exception");
            }
            catch (Exception ex)
            {
                string errorMessage = $"{nameof(DutyStatusAvailabilityWorker)} process caught an exception: \nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
                logger.Error(errorMessage);
                throw new Exception(errorMessage, ex);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Starts the current <see cref="DutyStatusAvailabilityWorker"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Only start if the DutyStatusAvailability data feed is enabled.
            if (Globals.ConfigurationManager.EnableDutyStatusAvailabilityDataFeed == true)
            {
                logger.Info($"Starting {nameof(DutyStatusAvailabilityWorker)}.");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Info($"DutyStatusAvailability data feed has not been enabled, so {nameof(DutyStatusAvailabilityWorker)} will not be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="DutyStatusAvailabilityWorker"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info($"{nameof(DutyStatusAvailabilityWorker)} stopped.");
            return base.StopAsync(cancellationToken);
        }
    }
}
