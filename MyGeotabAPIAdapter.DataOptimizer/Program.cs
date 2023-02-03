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
using MyGeotabAPIAdapter.DataOptimizer.Services;
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
                    services
                        // Miscellaneous:
                        .AddTransient<IDateTimeHelper, DateTimeHelper>()
                        .AddTransient<IEntityPersistenceLogger, EntityPersistenceLogger>()
                        .AddTransient<IExceptionHelper, ExceptionHelper>()
                        .AddTransient<IGeospatialHelper, GeospatialHelper>()
                        .AddTransient<ILongitudeLatitudeInterpolator, LongitudeLatitudeInterpolator>()
                        .AddTransient<IMessageLogger, MessageLogger>()

                        // Models for Dependency Injection:
                        .AddTransient<DbBinaryTypeT>()
                        .AddTransient<DbControllerT>()
                        .AddTransient<DbDevice>()
                        .AddTransient<DbDeviceT>()
                        .AddTransient<DbDiagnostic>()
                        .AddTransient<DbDriverChangeTypeT>()
                        .AddTransient<DbOProcessorTracking>()
                        .AddTransient<DbUser>()
                        .AddTransient<DbUserT>()

                        // Unit of Work:
                        .AddTransient<AdapterDatabaseUnitOfWorkContext>()
                        .AddTransient<OptimizerDatabaseUnitOfWorkContext>()
                        .AddTransient<IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>, GenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>>()
                        .AddTransient<IGenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext>, GenericDatabaseUnitOfWorkContext<OptimizerDatabaseUnitOfWorkContext>>()

                        // Environment:
                        .AddSingleton<IOptimizerEnvironment, OptimizerEnvironment>()
                        .AddSingleton<IOptimizerEnvironmentValidator, OptimizerEnvironmentValidator>()
                        .AddSingleton<IOrchestratorServiceTracker, OrchestratorServiceTracker>()
                        .AddTransient<IPrerequisiteProcessorChecker, PrerequisiteProcessorChecker>()
                        .AddSingleton<IProcessorTracker, ProcessorTracker>()
                        .AddSingleton<IStateMachine, StateMachine>()

                        // Configuration:
                        .AddTransient<IAdapterDatabaseConnectionInfoContainer, AdapterDatabaseOptimizerConnectionInfoContainer>()
                        .AddSingleton<IAdapterDatabaseObjectNames, AdapterDatabaseObjectNames>()
                        .AddTransient<IConfigurationHelper, ConfigurationHelper>()
                        .AddSingleton<IDataOptimizerConfiguration, DataOptimizerConfiguration>()
                        .AddSingleton<IDataOptimizerDatabaseConnectionInfoContainer, DataOptimizerDatabaseConnectionInfoContainer>()
                        .AddSingleton<IOptimizerDatabaseObjectNames, OptimizerDatabaseObjectNames>()

                        // Database Entity to Database Entity Mappers:
                        .AddTransient<IDbBinaryDataDbBinaryDataTEntityMapper, DbBinaryDataDbBinaryDataTEntityMapper>()
                        .AddTransient<IDbDeviceDbDeviceTEntityMapper, DbDeviceDbDeviceTEntityMapper>()
                        .AddTransient<IDbDiagnosticDbDiagnosticIdTEntityMapper, DbDiagnosticDbDiagnosticIdTEntityMapper>()
                        .AddTransient<IDbDiagnosticDbDiagnosticTEntityMapper, DbDiagnosticDbDiagnosticTEntityMapper>()
                        .AddTransient<IDbDriverChangeDbDriverChangeTEntityMapper, DbDriverChangeDbDriverChangeTEntityMapper>()
                        .AddTransient<IDbFaultDataDbFaultDataTEntityMapper, DbFaultDataDbFaultDataTEntityMapper>()
                        .AddTransient<IDbLogRecordDbLogRecordTEntityMapper, DbLogRecordDbLogRecordTEntityMapper>()
                        .AddTransient<IDbStatusDataDbStatusDataTEntityMapper, DbStatusDataDbStatusDataTEntityMapper>()
                        .AddTransient<IDbUserDbUserTEntityMapper, DbUserDbUserTEntityMapper>()

                        // Database Entity Persisters:
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

                        // Database Object Caches:
                        .AddSingleton<OptimizerGenericDbObjectCache<DbBinaryTypeT>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbBinaryTypeT, OptimizerGenericDbObjectCache<DbBinaryTypeT>>, GenericGenericDbObjectCache<DbBinaryTypeT, OptimizerGenericDbObjectCache<DbBinaryTypeT>>>()
                        .AddSingleton<OptimizerGenericDbObjectCache<DbControllerT>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbControllerT, OptimizerGenericDbObjectCache<DbControllerT>>, GenericGenericDbObjectCache<DbControllerT, OptimizerGenericDbObjectCache<DbControllerT>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDevice>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDevice, AdapterGenericDbObjectCache<DbDevice>>, GenericGenericDbObjectCache<DbDevice, AdapterGenericDbObjectCache<DbDevice>>>()
                        .AddSingleton<OptimizerGenericDbObjectCache<DbDeviceT>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDeviceT, OptimizerGenericDbObjectCache<DbDeviceT>>, GenericGenericDbObjectCache<DbDeviceT, OptimizerGenericDbObjectCache<DbDeviceT>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDiagnostic>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDiagnostic, AdapterGenericDbObjectCache<DbDiagnostic>>, GenericGenericDbObjectCache<DbDiagnostic, AdapterGenericDbObjectCache<DbDiagnostic>>>()
                        .AddSingleton<OptimizerGenericDbObjectCache<DbDriverChangeTypeT>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDriverChangeTypeT, OptimizerGenericDbObjectCache<DbDriverChangeTypeT>>, GenericGenericDbObjectCache<DbDriverChangeTypeT, OptimizerGenericDbObjectCache<DbDriverChangeTypeT>>>()
                        .AddSingleton<OptimizerGenericDbObjectCache<DbOProcessorTracking>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbOProcessorTracking, OptimizerGenericDbObjectCache<DbOProcessorTracking>>, GenericGenericDbObjectCache<DbOProcessorTracking, OptimizerGenericDbObjectCache<DbOProcessorTracking>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbUser>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbUser, AdapterGenericDbObjectCache<DbUser>>, GenericGenericDbObjectCache<DbUser, AdapterGenericDbObjectCache<DbUser>>>()
                        .AddSingleton<OptimizerGenericDbObjectCache<DbUserT>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbUserT, OptimizerGenericDbObjectCache<DbUserT>>, GenericGenericDbObjectCache<DbUserT, OptimizerGenericDbObjectCache<DbUserT>>>()

                        // Database Id and Object Caches:
                        .AddSingleton<IGenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticIdT>, GenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticIdT>>()
                        .AddSingleton<IGenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticT>, GenericGeotabGUIDCacheableDbObjectCache<DbDiagnosticT>>()
                        .AddTransient<IGenericIdCache<DbDeviceT>, GenericIdCache<DbDeviceT>>()
                        .AddTransient<IGenericIdCache<DbDiagnosticIdT>, GenericIdCache<DbDiagnosticIdT>>()
                        .AddTransient<IGenericIdCache<DbDiagnosticT>, GenericIdCache<DbDiagnosticT>>()
                        .AddTransient<IGenericIdCache<DbDriverChangeTypeT>, GenericIdCache<DbDriverChangeTypeT>>()
                        .AddTransient<IGenericIdCache<DbStatusDataT>, GenericIdCache<DbStatusDataT>>()
                        .AddTransient<IGenericIdCache<DbUserT>, GenericIdCache<DbUserT>>()

                        // Services:
                        .AddHostedService<Orchestrator>()
                        .AddHostedService<BinaryDataProcessor>()
                        .AddHostedService<DeviceProcessor>()
                        .AddHostedService<DiagnosticProcessor>()
                        .AddHostedService<DriverChangeProcessor>()
                        .AddHostedService<FaultDataOptimizer>()
                        .AddHostedService<FaultDataProcessor>()
                        .AddHostedService<LogRecordProcessor>()
                        .AddHostedService<StatusDataOptimizer>()
                        .AddHostedService<StatusDataProcessor>()
                        .AddHostedService<UserProcessor>()
                        ;
                });
    }
}
