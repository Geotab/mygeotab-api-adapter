using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Logic;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// A worker service that iteratively retrieves <see cref="DbOVDSServerCommand"/> entities from the database and sends them to an Open Vehicle Data Set (OVDS) server via HTTP POST.
    /// </summary>
    class OVDSClientWorker : BackgroundService
    {
        const string URLJSONFileExtension = ".json";
        const string URLVSSVersionDelimiter = "-v";
        const string VSSPathMapFileName = "VSSPathMaps.json";
        const string VSSPathMapTempFileName = "DOWNLOAD-VSSPathMaps.json";

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IConfiguration configuration;
        ConnectionInfo connectionInfo;
        static readonly HttpClient httpClient = new();
        bool initializationCompleted;
        DateTime lastVSSConfigurationRefreshTime = DateTime.MinValue;
        DateTime lastIterationStartTimeUtc;
        int commandTimeout = 0;

        /// <summary>
        /// Instantiates a new instance of the <see cref="OVDSClientWorker"/> class.
        /// </summary>
        public OVDSClientWorker(IConfiguration configuration)
        {
            this.configuration = configuration;
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Disposes the current <see cref="OVDSClientWorker"/> instance.
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

                    UpdateVSSPathMaps();

                    // Only proceed if the OVDSClientWorkerIntervalSeconds has elapsed since the last iteration was initiated.
                    if (Globals.TimeIntervalHasElapsed(lastIterationStartTimeUtc, Globals.DateTimeIntervalType.Seconds, Globals.ConfigurationManager.VSSConfiguration.OVDSClientWorkerIntervalSeconds))
                    {
                        lastIterationStartTimeUtc = DateTime.UtcNow;

                        using (var cancellationTokenSource = new CancellationTokenSource())
                        {
                            try
                            {
                                // Retrieve a batch of DbOVDSServerCommand entities from the database.
                                var dbOVDSServerCommands = await DbOVDSServerCommandService.GetAllAsync(connectionInfo, commandTimeout, VSSConfiguration.OVDSServerCommandBatchSize);
                                int dbOVDSServerCommandCount = dbOVDSServerCommands.Count();
                                int processedDbOVDSServerCommandCount = 0;
                                int failedDbOVDSServerCommandCount = 0;

                                if (dbOVDSServerCommands.Any())
                                {
                                    logger.Info($"Retrieved {dbOVDSServerCommands.Count()} records from OVDSServerCommands table. Processing...");
                                }
                                else
                                {
                                    logger.Debug($"No records retrieved from OVDSServerCommands table.");
                                    continue;
                                }
                                
                                // Process each of the retrieved DbOVDSServerCommands.
                                foreach (var dbOVDSServerCommand in dbOVDSServerCommands)
                                {
                                    var deleteCurrentDbOVDSServerCommandRecord = false;
                                    try
                                    {
                                        // Post the command to the OVDS server.
                                        HttpContent httpContent = new StringContent(dbOVDSServerCommand.Command);
                                        httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                                        HttpResponseMessage response = await httpClient.PostAsync(Globals.ConfigurationManager.VSSConfiguration.OVDSServerURL, httpContent, stoppingToken);
                                        response.EnsureSuccessStatusCode();
                                        string responseBody = await response.Content.ReadAsStringAsync(stoppingToken);
                                        logger.Debug($"Command successfully POSTed to OVDS server. Command Id: '{dbOVDSServerCommand.id}'. Response body: ['{responseBody}']");
                                        deleteCurrentDbOVDSServerCommandRecord = true;
                                        processedDbOVDSServerCommandCount += 1;
                                    }
                                    catch (Exception exception)
                                    {
                                        try
                                        {
                                            failedDbOVDSServerCommandCount += 1;
                                            var failureMessage = GenerateMessageForOVDSClientWorkerException(exception);

                                            // Insert a record into the FailedOVDSServerCommands table.
                                            DbFailedOVDSServerCommand dbFailedOVDSServerCommand = VSSObjectMapper.GetDbFailedOVDSServerCommand(dbOVDSServerCommand, failureMessage);
                                            dbFailedOVDSServerCommand.DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert;
                                            dbFailedOVDSServerCommand.RecordCreationTimeUtc = DateTime.UtcNow;
                                            await DbFailedOVDSServerCommandService.InsertAsync(connectionInfo, dbFailedOVDSServerCommand, commandTimeout);

                                            // Delete the current DbOVDSServerCommand from the OVDSServerCommands table.
                                            await DbOVDSServerCommandService.DeleteAsync(connectionInfo, dbOVDSServerCommand, commandTimeout);
                                        }
                                        catch (Exception ex)
                                        {
                                            throw new Exception($"Exception encountered while writing to {ConfigurationManager.DbFailedDbOVDSServerCommandsTableName} table or deleting from {ConfigurationManager.DbOVDSServerCommandTableName} table.", ex);
                                        }
                                    }
                                    finally
                                    {
                                        if (deleteCurrentDbOVDSServerCommandRecord == true)
                                        {
                                            // Delete the current DbOVDSServerCommand from the OVDSServerCommands table.
                                            await DbOVDSServerCommandService.DeleteAsync(connectionInfo, dbOVDSServerCommand, commandTimeout);
                                        }
                                    }
                                }

                                logger.Info($"Of the {dbOVDSServerCommandCount} records from OVDSServerCommands table, {processedDbOVDSServerCommandCount} were successfully processed and {failedDbOVDSServerCommandCount} failed. Copies of any failed records have been inserted into the FailedOVDSServerCommands table for reference.");
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
                        logger.Debug($"No OVDS server commands will be processed on this iteration; {Globals.ConfigurationManager.VSSConfiguration.OVDSClientWorkerIntervalSeconds} seconds have not passed since the process was last initiated.");
                    }

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"{nameof(OVDSClientWorker)} process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (DatabaseConnectionException databaseConnectionException)
                {
                    HandleException(databaseConnectionException, NLog.LogLevel.Error, $"{nameof(OVDSClientWorker)} process caught an exception");
                }
                catch (Exception ex)
                {
                    // If an exception hasn't been handled to this point, log it and kill the process.
                    HandleException(ex, NLog.LogLevel.Fatal, $"******** {nameof(OVDSClientWorker)} process caught an unhandled exception and will self-terminate now.");
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                }
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Generates the message for an <see cref="Exception"/> occurring while attempting to POST an OVDS server command. 
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <returns>The generated error message.</returns>
        static string GenerateMessageForOVDSClientWorkerException(Exception exception)
        {
            string exceptionTypeName = exception.GetType().Name;
            StringBuilder messageBuilder = new();
            messageBuilder.AppendLine($"TYPE: [{exceptionTypeName}];");
            messageBuilder.AppendLine($"MESSAGE [{exception.Message}];");

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                exceptionTypeName = exception.GetType().Name;
                messageBuilder.AppendLine($"---------- INNER EXCEPTION ----------");
                messageBuilder.AppendLine($"TYPE: [{exceptionTypeName}];");
                messageBuilder.AppendLine($"MESSAGE [{exception.Message}];");
            }

            return messageBuilder.ToString();
        }

        /// <summary>
        /// Generates and logs an error message for the supplied <paramref name="exception"/>. Does NOT implement connectivity restoration logic like <see cref="Worker.WaitForConnectivityRestorationAsync(StateReason)"/> since the <see cref="Worker"/> will already be taking care of it.
        /// /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="errorMessageLogLevel">The <see cref="NLog.LogLevel"/> to be used when logging the error message.</param>
        /// <param name="errorMessagePrefix">The start of the error message, which will be followed by the <see cref="Exception.Message"/>, <see cref="Exception.Source"/> and <see cref="Exception.StackTrace"/>.</param>
        /// <returns></returns>
        static void HandleException(Exception exception, NLog.LogLevel errorMessageLogLevel, string errorMessagePrefix = "An exception was encountered")
        {
            Globals.LogException(exception, errorMessageLogLevel, errorMessagePrefix);
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

                // Allow longer database command timeout because the main Worker process may be inserting a batch of 150K records at the same time as one of these queries are being executed and that may exceed the timeout.
                commandTimeout = Globals.ConfigurationManager.TimeoutSecondsForDatabaseTasks * 2;

                initializationCompleted = true;
                logger.Info("Initialization completed.");
            }
            catch (DatabaseConnectionException databaseConnectionException)
            {
                HandleException(databaseConnectionException, NLog.LogLevel.Error, $"{nameof(OVDSClientWorker)} process caught an exception");
            }
            catch (Exception ex)
            {
                string errorMessage = $"{nameof(OVDSClientWorker)} process caught an exception: \nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]";
                logger.Error(errorMessage);
                throw new Exception(errorMessage, ex);
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <summary>
        /// Reloads the LogRecordVSSPathMaps and StatusDataVSSPathMaps from appsettings.json and updates <see cref="ConfigurationManager.vssConfiguration"/> accordingly so that changes can be made to the VSS path maps while the application is running. Only performs this activity if <see cref="Globals.ConfigurationManager.VSSConfiguration.VSSPathMapUpdateIntervalMinutes"/> has elapsed since the last time this activity was performed.
        /// </summary>
        void UpdateVSSPathMaps()
        {
            if (Globals.TimeIntervalHasElapsed(lastVSSConfigurationRefreshTime, Globals.DateTimeIntervalType.Minutes, Globals.ConfigurationManager.VSSConfiguration.VSSPathMapUpdateIntervalMinutes))
            {
                // Construct URL of VSS path map file - incorporating the VSS version.
                StringBuilder urlBuilder = new();
                urlBuilder.Append(Globals.ConfigurationManager.VSSConfiguration.VSSPathMapFileURL);
                urlBuilder.Replace(URLJSONFileExtension, null);
                urlBuilder.Append(URLVSSVersionDelimiter);
                urlBuilder.Append(Globals.ConfigurationManager.VSSConfiguration.VSSVersion);
                urlBuilder.Append(URLJSONFileExtension);

                // Validate the URL.
                bool urlIsValid = Uri.TryCreate(urlBuilder.ToString(), UriKind.Absolute, out Uri uri);
                if (urlIsValid == false)
                {
                    throw new Exception($"Unable to construct valid URL using '{VSSConfiguration.ArgNameVSSPathMapFileURL}' and '{VSSConfiguration.ArgNameVSSVersion}' values from appsettings.json.");
                }

                // Download the VSS path map file.
                var vssFilePath = $"{AppContext.BaseDirectory}{VSSPathMapFileName}";
                var downloadTempFilePath = $"{AppContext.BaseDirectory}{VSSPathMapTempFileName}";
                try
                {
                    using (WebClient webClient = new())
                    {
                        webClient.DownloadFile(uri.AbsoluteUri, downloadTempFilePath);
                    }
                    File.Move(downloadTempFilePath, vssFilePath, true);
                }
                catch (Exception exception)
                {
                    // Log the exception. If a previously-downloaded copy of the VSS path map file exists, use that one; otherwise, throw the exception.
                    var exceptionMessage = GenerateMessageForOVDSClientWorkerException(exception);
                    StringBuilder exceptionMessageBuilder = new();
                    exceptionMessageBuilder.Append($"An exception was encountered while attempting to download the VSS path map file '{uri.AbsoluteUri}' to '{vssFilePath}'.");
                    if (File.Exists(vssFilePath))
                    {
                        exceptionMessageBuilder.Append($"The previously-downloaded copy of the VSS path map file will be used this time. Exception details: {exceptionMessage}");
                        logger.Warn(exceptionMessageBuilder.ToString());
                    }
                    else
                    {
                        exceptionMessageBuilder.Append($"Exception details: {exceptionMessage}");
                        logger.Warn(exceptionMessageBuilder.ToString());
                        throw;
                    }
                }

                // Load the VSS path map file and process its contents.
                var vssConfig = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile(VSSPathMapFileName, optional: false)
                    .Build();
                Globals.ConfigurationManager.VSSConfiguration.LoadLogRecordVSSPathMaps(vssConfig);
                Globals.ConfigurationManager.VSSConfiguration.LoadStatusDataVSSPathMaps(vssConfig);
                Globals.ConfigurationManager.VSSConfiguration.LoadAttributeVSSPathMaps(vssConfig);

                lastVSSConfigurationRefreshTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Starts the current <see cref="OVDSClientWorker"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            // Only start if the VSS Add-On is enabled.
            if (Globals.ConfigurationManager.VSSConfiguration.EnableVSSAddOn == true)
            {
                logger.Info($"Starting {nameof(OVDSClientWorker)}.");
                await base.StartAsync(cancellationToken);
            }
            else
            {
                logger.Info($"VSS Add-On has not been enabled, so {nameof(OVDSClientWorker)} will not be started.");
            }
        }

        /// <summary>
        /// Stops the current <see cref="OVDSClientWorker"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            logger.Info($"{nameof(OVDSClientWorker)} stopped.");
            return base.StopAsync(cancellationToken);
        }
    }
}
