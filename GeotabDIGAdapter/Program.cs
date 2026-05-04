using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Charging;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using Geotab.Checkmate.ObjectModel.Fuel;
using GeotabDIGAdapter;
using GeotabDIGAdapter.Services;
using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyGeotabAPIAdapter;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
using MyGeotabAPIAdapter.Services;
using NLog;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using MyGeotabAPIAdapter.MyAdminAPI;
using MyGeotabAPIAdapter.DIGAPI;


namespace GeotabDIGAdapter
{
    /// <summary>
    /// The main program. Handles initialization of logging and configuration settings and instantiates the application's services.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public static async Task Main(string[] args)
        {
            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                var hostBuilder = Host.CreateDefaultBuilder(args);

                // Conditionally configure the host for Windows Service or Linux Systemd.
                if (OperatingSystem.IsWindows())
                {
                    hostBuilder.UseWindowsService(options =>
                    {
                        // Set the name for the Windows service.
                        options.ServiceName = "GeotabDIGAdapter";
                    });
                }
                else if (OperatingSystem.IsLinux())
                {
                    hostBuilder.UseSystemd();
                }

                hostBuilder.ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    // NLog configuration is loaded from appsettings.json by the default builder.
                    logging.AddNLog();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Dependency injection registrations:
                    IConfiguration configuration = hostContext.Configuration;
                    HttpClient httpClient = new();
                    services
                        // Miscellaneous:
                        .AddSingleton(httpClient)
                        .AddTransient<IDateTimeHelper, DateTimeHelper>()
                        .AddTransient<IEntityPersistenceLogger, EntityPersistenceLogger>()
                        .AddTransient<IForeignKeyServiceDependencyMap, ForeignKeyServiceDependencyMap>()
                        .AddTransient<IGeotabIdConverter, GeotabIdConverter>()
                        .AddTransient<IHttpHelper, HttpHelper>()
                        .AddTransient<IMessageLogger, MessageLogger>()
                        .AddSingleton<IMyAdminAPIRateLimiter, MyAdminAPIRateLimiter>()
                        .AddSingleton<IMyAdminAPIHelper, MyAdminAPIHelper>()
                        .AddTransient<IExceptionHelper, ExceptionHelper>()
                        .AddTransient<IMyAdminExceptionHelper, MyAdminExceptionHelper>()
                        .AddTransient<IDIGExceptionHelper, DIGExceptionHelper>()
                        .AddSingleton<IDIGAPIRateLimiter, DIGAPIRateLimiter>()
                        .AddSingleton<IDIGAPIHelper, DIGAPIHelper>()
                        .AddSingleton<IProvisionedDeviceCache, ProvisionedDeviceCache>()
                        .AddSingleton<IMyGeotabAPIHelper, MyGeotabAPIHelper>()
                        .AddTransient<IStringHelper, StringHelper>()

                        // Models for Dependency Injection:
                        .AddTransient<DbDevice2>()
                        .AddTransient<DbGdaOServiceTracking>()

                        // Unit of Work:
                        .AddTransient<AdapterDatabaseUnitOfWorkContext>()
                        .AddTransient<IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>, GenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>>()

                        // Environment:
                        .AddSingleton<IAdapterEnvironment<DbGdaOServiceTracking>, AdapterEnvironment<DbGdaOServiceTracking>>()
                        .AddSingleton<IAdapterEnvironmentValidator<DbGdaOServiceTracking>, AdapterEnvironmentValidator<DbGdaOServiceTracking>>()
                        .AddSingleton<IOrchestratorServiceTracker, OrchestratorServiceTracker>()
                        .AddTransient<IPrerequisiteServiceChecker<DbGdaOServiceTracking>, PrerequisiteServiceChecker<DbGdaOServiceTracking>>()
                        .AddSingleton<IServiceTracker<DbGdaOServiceTracking>, ServiceTracker<DbGdaOServiceTracking>>()
                        .AddSingleton<IStateMachine<DbGdaMiddlewareVersionInfo>, StateMachine<DbGdaMiddlewareVersionInfo>>()

                        // Configuration:
                        .AddSingleton<IDIGAdapterConfiguration, DIGAdapterConfiguration>()
                        .AddSingleton<IDatabaseConfiguration>(sp => sp.GetRequiredService<IDIGAdapterConfiguration>())
                        .AddTransient<IAdapterDatabaseConnectionInfoContainer, AdapterDatabaseConnectionInfoContainer>()
                        .AddSingleton<IDIGAdapterDatabaseObjectNames, DIGAdapterDatabaseObjectNames>()
                        .AddTransient<IConfigurationHelper, ConfigurationHelper>()
                        

                        // Database Validator:
                        .AddSingleton<IDIGAdapterDatabaseValidator, DIGAdapterDatabaseValidator>()

                        // Database Entity to Database Entity Mappers:
                        .AddTransient<IDbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper, DbGdaQProvisionDeviceDbGdaProvisionedDeviceEntityMapper>()
                        .AddTransient<IDbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper, DbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper>()

                        // Telemetry Record Queue to DIG Record Mappers:
                        .AddTransient<IDbGdaQGpsRecordDIGGpsRecordEntityMapper, DbGdaQGpsRecordDIGGpsRecordEntityMapper>()
                        .AddTransient<IDbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper, DbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper>()
                        .AddTransient<IDbGdaQBinaryRecordDIGBinaryRecordEntityMapper, DbGdaQBinaryRecordDIGBinaryRecordEntityMapper>()
                        .AddTransient<IDbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper, DbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper>()
                        .AddTransient<IDbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper, DbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper>()
                        .AddTransient<IDbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper, DbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper>()
                        .AddTransient<IDbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper, DbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper>()
                        .AddTransient<IDbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper, DbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper>()
                        .AddTransient<IDbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper, DbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper>()
                        .AddTransient<IDbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper, DbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper>()
                        .AddTransient<IDbGdaQVinRecordDIGVinRecordEntityMapper, DbGdaQVinRecordDIGVinRecordEntityMapper>()

                        // Telemetry Record Queue to Fail Record Mappers:
                        .AddTransient<IDbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper, DbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper, DbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper, DbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper, DbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper, DbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper, DbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper, DbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper, DbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper, DbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper, DbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper>()
                        .AddTransient<IDbGdaQVinRecordDbGdaQVinRecordFailEntityMapper, DbGdaQVinRecordDbGdaQVinRecordFailEntityMapper>()

                        // Geotab Object to Database Entity Mappers:
                        /*
                        .AddTransient<IGeotabDeviceDbStgDevice2ObjectMapper, GeotabDeviceDbStgDevice2ObjectMapper>()
                        */

                        // Database Entity Persisters:
                        .AddTransient<IGenericEntityPersister<DbGdaMiddlewareVersionInfo>, GenericEntityPersister<DbGdaMiddlewareVersionInfo>>()
                        .AddSingleton<IGenericEntityPersister<DbGdaOServiceTracking>, GenericEntityPersister<DbGdaOServiceTracking>>()
                        .AddTransient<IGenericEntityPersister<DbGdaProvisionedDevice>, GenericEntityPersister<DbGdaProvisionedDevice>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQProvisionDevice>, GenericEntityPersister<DbGdaQProvisionDevice>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQProvisionDeviceFail>, GenericEntityPersister<DbGdaQProvisionDeviceFail>>()

                        // Telemetry Record Queue Entity Persisters:
                        .AddTransient<IGenericEntityPersister<DbGdaQGpsRecord>, GenericEntityPersister<DbGdaQGpsRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQGpsRecordFail>, GenericEntityPersister<DbGdaQGpsRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQAccelerationRecord>, GenericEntityPersister<DbGdaQAccelerationRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQAccelerationRecordFail>, GenericEntityPersister<DbGdaQAccelerationRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQBinaryRecord>, GenericEntityPersister<DbGdaQBinaryRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQBinaryRecordFail>, GenericEntityPersister<DbGdaQBinaryRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQBluetoothRecord>, GenericEntityPersister<DbGdaQBluetoothRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQBluetoothRecordFail>, GenericEntityPersister<DbGdaQBluetoothRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQDriverChangeRecord>, GenericEntityPersister<DbGdaQDriverChangeRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQDriverChangeRecordFail>, GenericEntityPersister<DbGdaQDriverChangeRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQGenericFaultRecord>, GenericEntityPersister<DbGdaQGenericFaultRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQGenericFaultRecordFail>, GenericEntityPersister<DbGdaQGenericFaultRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQGenericStatusRecord>, GenericEntityPersister<DbGdaQGenericStatusRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQGenericStatusRecordFail>, GenericEntityPersister<DbGdaQGenericStatusRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQJ1708FaultRecord>, GenericEntityPersister<DbGdaQJ1708FaultRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQJ1708FaultRecordFail>, GenericEntityPersister<DbGdaQJ1708FaultRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQJ1939FaultRecord>, GenericEntityPersister<DbGdaQJ1939FaultRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQJ1939FaultRecordFail>, GenericEntityPersister<DbGdaQJ1939FaultRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQObdiiFaultRecord>, GenericEntityPersister<DbGdaQObdiiFaultRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQObdiiFaultRecordFail>, GenericEntityPersister<DbGdaQObdiiFaultRecordFail>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQVinRecord>, GenericEntityPersister<DbGdaQVinRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaQVinRecordFail>, GenericEntityPersister<DbGdaQVinRecordFail>>()

                        // Invalid Record Retrieval Entity Persisters:
                        .AddTransient<IGenericEntityPersister<DbGdaDIGInvalidRecord>, GenericEntityPersister<DbGdaDIGInvalidRecord>>()
                        .AddTransient<IGenericEntityPersister<DbGdaDIGInvalidRecordsCursor>, GenericEntityPersister<DbGdaDIGInvalidRecordsCursor>>()

                        // Database Object Caches:
                        .AddSingleton<AdapterGenericDbObjectCache<DbGdaOServiceTracking>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbGdaOServiceTracking, AdapterGenericDbObjectCache<DbGdaOServiceTracking>>, GenericGenericDbObjectCache<DbGdaOServiceTracking, AdapterGenericDbObjectCache<DbGdaOServiceTracking>>>()

                        /*
                        // Geotab Object Cachers:
                        .AddSingleton<IGenericGeotabObjectCacher<Device>, GenericGeotabObjectCacher<Device>>()

                        // Geotab Object Feeders:
                        .AddSingleton<IGenericGeotabObjectFeeder<Geotab.Checkmate.ObjectModel.BinaryData>, GenericGeotabObjectFeeder<Geotab.Checkmate.ObjectModel.BinaryData>>()

                        // Geotab Object Filterers:
                        .AddSingleton<IGenericGeotabObjectFiltererBase<Device>, GenericGeotabObjectFiltererBase<Device>>()
                        .AddSingleton<IGeotabDeviceFilterer, GeotabDeviceFilterer>()

                        // Geotab Object Hydrators:
                        .AddSingleton<IGenericGeotabObjectHydrator<Device>, GenericGeotabObjectHydrator<Device>>()
                        */
                        ;

                    // Resolve IDIGAdapterConfiguration early to use it for conditional service registration.
                    var serviceProvider = services.BuildServiceProvider();
                    var adapterConfig = serviceProvider.GetRequiredService<IDIGAdapterConfiguration>();

                    // Run the DatabaseValidator to ensure the adapter database version is correct.
                    var databaseValidator = serviceProvider.GetRequiredService<IDIGAdapterDatabaseValidator>();
                    databaseValidator.ValidateDatabaseVersion();

                    // Configure options for the services. This is necessary because the services are registered as hosted services and the options are used to determine whether the individual services should pause for database maintenance windows wherein operations such as reindexing could potentially cause exceptions.
                    var serviceNames = new string[] { nameof(Orchestrator), nameof(DeviceDIGReadinessService), nameof(DeviceProvisioningService), nameof(InvalidRecordRetrievalService), nameof(TelemetryDataService) };

                    // Register the ServiceOptionsProvider.
                    // Register the service names in the ServiceOptionsProvider. All services should be tracked (even if they don't need to e paused for database maintenance) so that it is possible to know when they have all been loaded on startup before initiating any database maintenance.
                    services.AddSingleton<IServiceOptionsProvider>(sp =>
                    {
                        // Resolve the required IOptionsMonitor<ServiceOptions> from the service provider.
                        var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<ServiceOptions>>();

                        // Create an instance of ServiceOptionsProvider with the resolved dependency.
                        var serviceOptionsProvider = new ServiceOptionsProvider(optionsMonitor);

                        // Track all service names.
                        foreach (var serviceName in serviceNames)
                        {
                            serviceOptionsProvider.TrackService(serviceName);
                        }

                        return serviceOptionsProvider;
                    });

                    // Configure the ServiceOptions for each service.
                    foreach (var serviceName in serviceNames)
                    {
                        services.Configure<ServiceOptions>(serviceName, options =>
                        {
                            options.ServiceName = serviceName;

                            // Any services that don't interact with the adapter database should not pause for database maintenance and can be added to the to the line below. The rest should pause for database maintenance.
                            if (serviceName == nameof(Orchestrator))
                            {
                                options.PauseForDatabaseMaintenance = false;
                            }
                            else
                            {
                                options.PauseForDatabaseMaintenance = true;
                            }
                        });
                    }

                    // Register the StateMachine
                    services.AddSingleton<IStateMachine<DbGdaMiddlewareVersionInfo>>(sp =>
                    {
                        var serviceOptionsProvider = sp.GetRequiredService<IServiceOptionsProvider>();

                        // Get all service names from configured ServiceOptions.
                        var serviceNames = serviceOptionsProvider.GetAllServiceNames();

                        // Retrieve all configured ServiceOptions using the service names.
                        var serviceOptions = serviceOptionsProvider.GetAllServiceOptions(serviceNames);

                        var digAdapterConfiguration = sp.GetRequiredService<IDIGAdapterConfiguration>();

                        // Pass the service options and other dependencies to the StateMachine constructor.
                        return new StateMachine<DbGdaMiddlewareVersionInfo>(digAdapterConfiguration, serviceOptions);
                    });


                    // Register the services.
                    services
                    .AddHostedService<Orchestrator>()
                    .AddHostedService<DeviceDIGReadinessService>()
                    .AddHostedService<DeviceProvisioningService>()
                    .AddHostedService<InvalidRecordRetrievalService>()
                    .AddHostedService<TelemetryDataService>()
                    ;

                    // Register a BackgroundServiceAwaiter for each service.
                    services
                    .AddSingleton<IBackgroundServiceAwaiter<Orchestrator>, BackgroundServiceAwaiter<Orchestrator>>()
                    .AddSingleton<IBackgroundServiceAwaiter<DeviceDIGReadinessService>, BackgroundServiceAwaiter<DeviceDIGReadinessService>>()
                    .AddSingleton<IBackgroundServiceAwaiter<DeviceProvisioningService>, BackgroundServiceAwaiter<DeviceProvisioningService>>()
                    .AddSingleton<IBackgroundServiceAwaiter<InvalidRecordRetrievalService>, BackgroundServiceAwaiter<InvalidRecordRetrievalService>>()
                    .AddSingleton<IBackgroundServiceAwaiter<TelemetryDataService>, BackgroundServiceAwaiter<TelemetryDataService>>()
                    ;
                });

                var host = hostBuilder.Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                // NLog: catch any exception and log it.
                logger.Error(ex, $"Stopped application because of exception: \nMESSAGE [{ex.Message}]; \nSOURCE [{ex.Source}]; \nSTACK TRACE [{ex.StackTrace}]");
                throw; // Re-throw the exception to ensure the process terminates with an error code.
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
        }
    }
}