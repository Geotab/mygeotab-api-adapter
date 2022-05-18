using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Geospatial;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.DataOptimizer.EstimatorsAndInterpolators;
using NLog;
using NLog.Extensions.Logging;
using System;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    /// <summary>
    /// The main program. Handles initialization of logging and configuration settings and instantiates worker classes, which are responsible for execution of  application logic. 
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                CreateHostBuilder(args, config).Build().Run();
            }
            catch (Exception ex)
            {
                // NLog: catch any exception and log it.
                logger.Error(ex, $"Stopped application because of exception: \nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]");
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args, IConfiguration config) =>
            Host.CreateDefaultBuilder(args)
                // Configure logging with NLog. 
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    logging.AddNLog(config);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    services.AddTransient<UnitOfWorkContext>()
                            .AddTransient<IEntityPersistenceLogger, EntityPersistenceLogger>()
                            .AddTransient<IDateTimeHelper, DateTimeHelper>()
                            .AddTransient<IGeospatialHelper, GeospatialHelper>()
                            .AddTransient<ILongitudeLatitudeInterpolator, LongitudeLatitudeInterpolator>()
                            .AddTransient<IDbBinaryDataDbBinaryDataTEntityMapper, DbBinaryDataDbBinaryDataTEntityMapper>()
                            .AddTransient<IDbDeviceDbDeviceTEntityMapper, DbDeviceDbDeviceTEntityMapper>()
                            .AddTransient<IDbDiagnosticDbDiagnosticIdTEntityMapper, DbDiagnosticDbDiagnosticIdTEntityMapper>()
                            .AddTransient<IDbDiagnosticDbDiagnosticTEntityMapper, DbDiagnosticDbDiagnosticTEntityMapper>()
                            .AddTransient<IDbDriverChangeDbDriverChangeTEntityMapper, DbDriverChangeDbDriverChangeTEntityMapper>()
                            .AddTransient<IDbFaultDataDbFaultDataTEntityMapper, DbFaultDataDbFaultDataTEntityMapper>()
                            .AddTransient<IDbLogRecordDbLogRecordTEntityMapper, DbLogRecordDbLogRecordTEntityMapper>()
                            .AddTransient<IDbStatusDataDbStatusDataTEntityMapper, DbStatusDataDbStatusDataTEntityMapper>()
                            .AddTransient<IDbUserDbUserTEntityMapper, DbUserDbUserTEntityMapper>()
                            .AddTransient<IConfigurationHelper, ConfigurationHelper>()
                            .AddSingleton<IDataOptimizerConfiguration, DataOptimizerConfiguration>()
                            .AddSingleton<IAdapterDatabaseObjectNames, AdapterDatabaseObjectNames>()
                            .AddSingleton<IOptimizerDatabaseObjectNames, OptimizerDatabaseObjectNames>()
                            .AddSingleton<IGenericEntityPersister<DbOProcessorTracking>, GenericEntityPersister<DbOProcessorTracking>>()
                            .AddTransient<IGenericEntityPersister<DbBinaryData>, GenericEntityPersister<DbBinaryData>>()
                            .AddTransient<IGenericEntityPersister<DbBinaryDataT>, GenericEntityPersister<DbBinaryDataT>>()
                            .AddTransient<IGenericEntityPersister<DbBinaryTypeT>, GenericEntityPersister<DbBinaryTypeT>>()
                            .AddTransient<IGenericEntityPersister<DbControllerT>, GenericEntityPersister<DbControllerT>>()
                            .AddTransient<IGenericEntityPersister<DbDeviceT>, GenericEntityPersister<DbDeviceT>>()
                            .AddTransient<IGenericEntityPersister<DbDiagnosticIdT>, GenericEntityPersister<DbDiagnosticIdT>>()
                            .AddTransient<IGenericEntityPersister<DbDiagnosticT>, GenericEntityPersister<DbDiagnosticT>>()
                            .AddTransient<IGenericEntityPersister<DbDriverChange>, GenericEntityPersister<DbDriverChange>>()
                            .AddTransient<IGenericEntityPersister<DbDriverChangeT>, GenericEntityPersister<DbDriverChangeT>>()
                            .AddTransient<IGenericEntityPersister<DbDriverChangeTypeT>, GenericEntityPersister<DbDriverChangeTypeT>>()
                            .AddTransient<IGenericEntityPersister<DbFaultData>, GenericEntityPersister<DbFaultData>>()
                            .AddTransient<IGenericEntityPersister<DbFaultDataT>, GenericEntityPersister<DbFaultDataT>>()
                            .AddTransient<IGenericEntityPersister<DbFaultDataTDriverIdUpdate>, GenericEntityPersister<DbFaultDataTDriverIdUpdate>>()
                            .AddTransient<IGenericEntityPersister<DbFaultDataTLongLatUpdate>, GenericEntityPersister<DbFaultDataTLongLatUpdate>>()
                            .AddTransient<IGenericEntityPersister<DbLogRecord>, GenericEntityPersister<DbLogRecord>>()
                            .AddTransient<IGenericEntityPersister<DbLogRecordT>, GenericEntityPersister<DbLogRecordT>>()
                            .AddTransient<IGenericEntityPersister<DbStatusData>, GenericEntityPersister<DbStatusData>>()
                            .AddTransient<IGenericEntityPersister<DbStatusDataT>, GenericEntityPersister<DbStatusDataT>>()
                            .AddTransient<IGenericEntityPersister<DbStatusDataTDriverIdUpdate>, GenericEntityPersister<DbStatusDataTDriverIdUpdate>>()
                            .AddTransient<IGenericEntityPersister<DbStatusDataTLongLatUpdate>, GenericEntityPersister<DbStatusDataTLongLatUpdate>>()
                            .AddTransient<IGenericEntityPersister<DbUserT>, GenericEntityPersister<DbUserT>>()
                            .AddTransient<IGenericIdCache<DbDeviceT>, GenericIdCache<DbDeviceT>>()
                            .AddTransient<IGenericIdCache<DbDiagnosticIdT>, GenericIdCache<DbDiagnosticIdT>>()
                            .AddTransient<IGenericIdCache<DbDiagnosticT>, GenericIdCache<DbDiagnosticT>>()
                            .AddTransient<IGenericIdCache<DbDriverChangeTypeT>, GenericIdCache<DbDriverChangeTypeT>>()
                            .AddTransient<IGenericIdCache<DbStatusDataT>, GenericIdCache<DbStatusDataT>>()
                            .AddTransient<IGenericIdCache<DbUserT>, GenericIdCache<DbUserT>>()
                            .AddSingleton<IGenericDbObjectCache<DbOProcessorTracking>, GenericDbObjectCache<DbOProcessorTracking>>()
                            .AddSingleton<IGenericDbObjectCache<DbBinaryTypeT>, GenericDbObjectCache<DbBinaryTypeT>>()
                            .AddSingleton<IGenericDbObjectCache<DbControllerT>, GenericDbObjectCache<DbControllerT>>()
                            .AddSingleton<IGenericDbObjectCache<DbDevice>, GenericDbObjectCache<DbDevice>>()
                            .AddSingleton<IGenericDbObjectCache<DbDeviceT>, GenericDbObjectCache<DbDeviceT>>()
                            .AddSingleton<IGenericGeotabGUIDCacheableDbObjectCache<DbDiagnostic>, GenericGeotabGUIDCacheableDbObjectCache<DbDiagnostic>>()
                            .AddSingleton<IGenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticIdT>, GenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticIdT>>()
                            .AddSingleton<IGenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticT>, GenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticT>>()
                            .AddSingleton<IGenericDbObjectCache<DbDriverChangeTypeT>, GenericDbObjectCache<DbDriverChangeTypeT>>()
                            .AddSingleton<IGenericDbObjectCache<DbUser>, GenericDbObjectCache<DbUser>>()
                            .AddSingleton<IGenericDbObjectCache<DbUserT>, GenericDbObjectCache<DbUserT>>()
                            .AddSingleton<IOptimizerEnvironmentValidator, OptimizerEnvironmentValidator>()
                            .AddSingleton<IOptimizerEnvironment, OptimizerEnvironment>()
                            .AddSingleton<IConnectionInfoContainer, ConnectionInfoContainer>()
                            .AddTransient<IExceptionHelper, ExceptionHelper>()
                            .AddTransient<IMessageLogger, MessageLogger>()
                            .AddTransient<IPrerequisiteProcessorChecker, PrerequisiteProcessorChecker>()
                            .AddSingleton<IStateMachine, StateMachine>()
                            .AddSingleton<IProcessorTracker, ProcessorTracker>()
                            .AddHostedService<Orchestrator>()
                            .AddHostedService<BinaryDataProcessor>()
                            .AddHostedService<DeviceProcessor>()
                            .AddHostedService<DiagnosticProcessor>()
                            .AddHostedService<DriverChangeProcessor>()
                            .AddHostedService<FaultDataProcessor>()
                            .AddHostedService<LogRecordProcessor>()
                            .AddHostedService<StatusDataProcessor>()
                            .AddHostedService<UserProcessor>()
                            .AddHostedService<FaultDataOptimizer>()
                            .AddHostedService<StatusDataOptimizer>();
                });
    }
}
