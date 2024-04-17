using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Charging;
using Geotab.Checkmate.ObjectModel.Engine;
using Geotab.Checkmate.ObjectModel.Exceptions;
using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyGeotabAPIAdapter.Add_Ons.VSS;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Configuration.Add_Ons.VSS;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Database.Models.Add_Ons.VSS;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
using MyGeotabAPIAdapter.Services;
using NLog;
using NLog.Extensions.Logging;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// The main program.  Handles initialization of logging and configuration settings and instantiates the <see cref="Worker"/> class, which is responsible for execution of  application logic. 
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
                    HttpClient httpClient = new();
                    services
                        // Miscellaneous:
                        .AddSingleton(httpClient)
                        .AddTransient<IDateTimeHelper, DateTimeHelper>()
                        .AddTransient<IEntityPersistenceLogger, EntityPersistenceLogger>()
                        .AddTransient<IExceptionHelper, ExceptionHelper>()
                        .AddTransient<IHttpHelper, HttpHelper>()
                        .AddTransient<IMessageLogger, MessageLogger>()
                        .AddSingleton<IMyGeotabAPIHelper, MyGeotabAPIHelper>()
                        .AddTransient<IStringHelper, StringHelper>()
                        .AddSingleton<IUnmappedDiagnosticManager, UnmappedDiagnosticManager>()
                        .AddTransient<IVSSObjectMapper, VSSObjectMapper>()

                        // Models for Dependency Injection:
                        .AddTransient<DbCondition>()
                        .AddTransient<DbDevice>()
                        .AddTransient<DbDeviceStatusInfo>()
                        .AddTransient<DbDiagnostic>()
                        .AddTransient<DbDutyStatusAvailability>()
                        .AddTransient<DbOServiceTracking>()
                        .AddTransient<DbRule>()
                        .AddTransient<DbUser>()
                        .AddTransient<DbZone>()
                        .AddTransient<DbZoneType>()

                        // Unit of Work:
                        .AddTransient<AdapterDatabaseUnitOfWorkContext>()
                        .AddTransient<IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>, GenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>>()

                        // Environment:
                        .AddSingleton<IAdapterEnvironment, AdapterEnvironment>()
                        .AddSingleton<IAdapterEnvironmentValidator, AdapterEnvironmentValidator>()
                        .AddSingleton<IOrchestratorServiceTracker, OrchestratorServiceTracker>()
                        .AddTransient<IPrerequisiteServiceChecker, PrerequisiteServiceChecker>()
                        .AddSingleton<IServiceTracker, ServiceTracker>()
                        .AddSingleton<IStateMachine, StateMachine>()

                        // Configuration:
                        .AddSingleton<IAdapterConfiguration, AdapterConfiguration>()
                        .AddTransient<IAdapterDatabaseConnectionInfoContainer, AdapterDatabaseConnectionInfoContainer>()
                        .AddSingleton<IAdapterDatabaseObjectNames, AdapterDatabaseObjectNames>()
                        .AddTransient<IConfigurationHelper, ConfigurationHelper>()
                        .AddSingleton<IVSSConfiguration, VSSConfiguration>()

                        // Database Entity to Database Entity Mappers:
                        .AddTransient<IDbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper, DbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper>()

                        // Geotab Object to Database Entity Mappers:
                        .AddTransient<IGeotabBinaryDataDbBinaryDataObjectMapper, GeotabBinaryDataDbBinaryDataObjectMapper>()
                        .AddTransient<IGeotabChargeEventDbChargeEventObjectMapper, GeotabChargeEventDbChargeEventObjectMapper>()
                        .AddTransient<IGeotabDebugDataDbDebugDataObjectMapper, GeotabDebugDataDbDebugDataObjectMapper>()
                        .AddTransient<IGeotabConditionDbConditionObjectMapper, GeotabConditionDbConditionObjectMapper>()
                        .AddTransient<IGeotabDeviceDbDeviceObjectMapper, GeotabDeviceDbDeviceObjectMapper>()
                        .AddTransient<IGeotabDeviceStatusInfoDbDeviceStatusInfoObjectMapper, GeotabDeviceStatusInfoDbDeviceStatusInfoObjectMapper>()
                        .AddTransient<IGeotabDiagnosticDbDiagnosticObjectMapper, GeotabDiagnosticDbDiagnosticObjectMapper>()
                        .AddTransient<IGeotabDriverChangeDbDriverChangeObjectMapper, GeotabDriverChangeDbDriverChangeObjectMapper>()
                        .AddTransient<IGeotabDVIRDefectDbDVIRDefectObjectMapper, GeotabDVIRDefectDbDVIRDefectObjectMapper>()
                        .AddTransient<IGeotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper, GeotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper>()
                        .AddTransient<IGeotabDutyStatusLogDbDutyStatusLogObjectMapper, GeotabDutyStatusLogDbDutyStatusLogObjectMapper>()
                        .AddTransient<IGeotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper, GeotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper>()
                        .AddTransient<IGeotabDVIRLogDbDVIRLogObjectMapper, GeotabDVIRLogDbDVIRLogObjectMapper>()
                        .AddTransient<IGeotabExceptionEventDbExceptionEventObjectMapper, GeotabExceptionEventDbExceptionEventObjectMapper>()
                        .AddTransient<IGeotabFaultDataDbFaultDataObjectMapper, GeotabFaultDataDbFaultDataObjectMapper>()
                        .AddTransient<IGeotabLogRecordDbLogRecordObjectMapper, GeotabLogRecordDbLogRecordObjectMapper>()
                        .AddTransient<IGeotabRuleDbRuleObjectMapper, GeotabRuleDbRuleObjectMapper>()
                        .AddTransient<IGeotabStatusDataDbStatusDataObjectMapper, GeotabStatusDataDbStatusDataObjectMapper>()
                        .AddTransient<IGeotabTripDbTripObjectMapper, GeotabTripDbTripObjectMapper>()
                        .AddTransient<IGeotabUserDbUserObjectMapper, GeotabUserDbUserObjectMapper>()
                        .AddTransient<IGeotabZoneDbZoneObjectMapper, GeotabZoneDbZoneObjectMapper>()
                        .AddTransient<IGeotabZoneTypeDbZoneTypeObjectMapper, GeotabZoneTypeDbZoneTypeObjectMapper>()

                        // Database Entity Persisters:
                        .AddTransient<IGenericEntityPersister<DbBinaryData>, GenericEntityPersister<DbBinaryData>>()
                        .AddTransient<IGenericEntityPersister<DbChargeEvent>, GenericEntityPersister<DbChargeEvent>>()
                        .AddTransient<IGenericEntityPersister<DbCondition>, GenericEntityPersister<DbCondition>>()
                        .AddTransient<IGenericEntityPersister<DbDebugData>, GenericEntityPersister<DbDebugData>>()
                        .AddTransient<IGenericEntityPersister<DbDevice>, GenericEntityPersister<DbDevice>>()
                        .AddTransient<IGenericEntityPersister<DbDeviceStatusInfo>, GenericEntityPersister<DbDeviceStatusInfo>>()
                        .AddTransient<IGenericEntityPersister<DbDiagnostic>, GenericEntityPersister<DbDiagnostic>>()
                        .AddTransient<IGenericEntityPersister<DbDriverChange>, GenericEntityPersister<DbDriverChange>>()
                        .AddTransient<IGenericEntityPersister<DbDutyStatusAvailability>, GenericEntityPersister<DbDutyStatusAvailability>>()
                        .AddTransient<IGenericEntityPersister<DbDutyStatusLog>, GenericEntityPersister<DbDutyStatusLog>>()
                        .AddTransient<IGenericEntityPersister<DbDVIRLog>, GenericEntityPersister<DbDVIRLog>>()
                        .AddTransient<IGenericEntityPersister<DbDVIRDefect>, GenericEntityPersister<DbDVIRDefect>>()
                        .AddTransient<IGenericEntityPersister<DbDVIRDefectRemark>, GenericEntityPersister<DbDVIRDefectRemark>>()
                        .AddTransient<IGenericEntityPersister<DbDVIRDefectUpdate>, GenericEntityPersister<DbDVIRDefectUpdate>>()
                        .AddTransient<IGenericEntityPersister<DbExceptionEvent>, GenericEntityPersister<DbExceptionEvent>>()
                        .AddTransient<IGenericEntityPersister<DbFailedDVIRDefectUpdate>, GenericEntityPersister<DbFailedDVIRDefectUpdate>>()
                        .AddTransient<IGenericEntityPersister<DbFailedOVDSServerCommand>, GenericEntityPersister<DbFailedOVDSServerCommand>>()
                        .AddTransient<IGenericEntityPersister<DbFaultData>, GenericEntityPersister<DbFaultData>>()
                        .AddTransient<IGenericEntityPersister<DbLogRecord>, GenericEntityPersister<DbLogRecord>>()
                        .AddTransient<IGenericEntityPersister<DbMyGeotabVersionInfo>, GenericEntityPersister<DbMyGeotabVersionInfo>>()
                        .AddSingleton<IGenericEntityPersister<DbOServiceTracking>, GenericEntityPersister<DbOServiceTracking>>()
                        .AddTransient<IGenericEntityPersister<DbOVDSServerCommand>, GenericEntityPersister<DbOVDSServerCommand>>()
                        .AddTransient<IGenericEntityPersister<DbRule>, GenericEntityPersister<DbRule>>()
                        .AddTransient<IGenericEntityPersister<DbStatusData>, GenericEntityPersister<DbStatusData>>()
                        .AddTransient<IGenericEntityPersister<DbTrip>, GenericEntityPersister<DbTrip>>()
                        .AddTransient<IGenericEntityPersister<DbUser>, GenericEntityPersister<DbUser>>()
                        .AddTransient<IGenericEntityPersister<DbZone>, GenericEntityPersister<DbZone>>()
                        .AddTransient<IGenericEntityPersister<DbZoneType>, GenericEntityPersister<DbZoneType>>()

                        // Database Object Caches:
                        .AddSingleton<AdapterGenericDbObjectCache<DbCondition>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbCondition, AdapterGenericDbObjectCache<DbCondition>>, GenericGenericDbObjectCache<DbCondition, AdapterGenericDbObjectCache<DbCondition>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDevice>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDevice, AdapterGenericDbObjectCache<DbDevice>>, GenericGenericDbObjectCache<DbDevice, AdapterGenericDbObjectCache<DbDevice>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDeviceStatusInfo>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDeviceStatusInfo, AdapterGenericDbObjectCache<DbDeviceStatusInfo>>, GenericGenericDbObjectCache<DbDeviceStatusInfo, AdapterGenericDbObjectCache<DbDeviceStatusInfo>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDiagnostic>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDiagnostic, AdapterGenericDbObjectCache<DbDiagnostic>>, GenericGenericDbObjectCache<DbDiagnostic, AdapterGenericDbObjectCache<DbDiagnostic>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDutyStatusAvailability>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDutyStatusAvailability, AdapterGenericDbObjectCache<DbDutyStatusAvailability>>, GenericGenericDbObjectCache<DbDutyStatusAvailability, AdapterGenericDbObjectCache<DbDutyStatusAvailability>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbOServiceTracking>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbOServiceTracking, AdapterGenericDbObjectCache<DbOServiceTracking>>, GenericGenericDbObjectCache<DbOServiceTracking, AdapterGenericDbObjectCache<DbOServiceTracking>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbRule>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbRule, AdapterGenericDbObjectCache<DbRule>>, GenericGenericDbObjectCache<DbRule, AdapterGenericDbObjectCache<DbRule>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbUser>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbUser, AdapterGenericDbObjectCache<DbUser>>, GenericGenericDbObjectCache<DbUser, AdapterGenericDbObjectCache<DbUser>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbZone>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbZone, AdapterGenericDbObjectCache<DbZone>>, GenericGenericDbObjectCache<DbZone, AdapterGenericDbObjectCache<DbZone>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbZoneType>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbZoneType, AdapterGenericDbObjectCache<DbZoneType>>, GenericGenericDbObjectCache<DbZoneType, AdapterGenericDbObjectCache<DbZoneType>>>()

                        // Geotab Object Cachers:
                        .AddSingleton<IGenericGeotabObjectCacher<Controller>, GenericGeotabObjectCacher<Controller>>()
                        .AddSingleton<IGenericGeotabObjectCacher<Device>, GenericGeotabObjectCacher<Device>>()
                        .AddSingleton<IGenericGeotabObjectCacher<Diagnostic>, GenericGeotabObjectCacher<Diagnostic>>()
                        .AddSingleton<IGenericGeotabObjectCacher<FailureMode>, GenericGeotabObjectCacher<FailureMode>>()
                        .AddSingleton<IGenericGeotabObjectCacher<Group>, GenericGeotabObjectCacher<Group>>()
                        .AddSingleton<IGenericGeotabObjectCacher<Rule>, GenericGeotabObjectCacher<Rule>>()
                        .AddSingleton<IGenericGeotabObjectCacher<UnitOfMeasure>, GenericGeotabObjectCacher<UnitOfMeasure>>()
                        .AddSingleton<IGenericGeotabObjectCacher<User>, GenericGeotabObjectCacher<User>>()
                        .AddSingleton<IGenericGeotabObjectCacher<Zone>, GenericGeotabObjectCacher<Zone>>()
                        .AddSingleton<IGenericGeotabObjectCacher<ZoneType>, GenericGeotabObjectCacher<ZoneType>>()

                        // Geotab Object Feeders:
                        .AddSingleton<IGenericGeotabObjectFeeder<Geotab.Checkmate.ObjectModel.BinaryData>, GenericGeotabObjectFeeder<Geotab.Checkmate.ObjectModel.BinaryData>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<ChargeEvent>, GenericGeotabObjectFeeder<ChargeEvent>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<DebugData>, GenericGeotabObjectFeeder<DebugData>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<DeviceStatusInfo>, GenericGeotabObjectFeeder<DeviceStatusInfo>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<DutyStatusLog>, GenericGeotabObjectFeeder<DutyStatusLog>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<DriverChange>, GenericGeotabObjectFeeder<DriverChange>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<DVIRLog>, GenericGeotabObjectFeeder<DVIRLog>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<ExceptionEvent>, GenericGeotabObjectFeeder<ExceptionEvent>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<FaultData>, GenericGeotabObjectFeeder<FaultData>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<LogRecord>, GenericGeotabObjectFeeder<LogRecord>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<StatusData>, GenericGeotabObjectFeeder<StatusData>>()
                        .AddSingleton<IGenericGeotabObjectFeeder<Trip>, GenericGeotabObjectFeeder<Trip>>()

                        // Geotab Object Filterers:
                        .AddSingleton<IGenericGeotabObjectFiltererBase<Device>, GenericGeotabObjectFiltererBase<Device>>()
                        .AddSingleton<IGeotabDeviceFilterer, GeotabDeviceFilterer>()
                        .AddSingleton<IGenericGeotabObjectFiltererBase<Diagnostic>, GenericGeotabObjectFiltererBase<Diagnostic>>()
                        .AddSingleton<IGeotabDiagnosticFilterer, GeotabDiagnosticFilterer>()
                        .AddSingleton<IMinimumIntervalSampler<LogRecord>, MinimumIntervalSampler<LogRecord>>()
                        .AddSingleton<IMinimumIntervalSampler<StatusData>, MinimumIntervalSampler<StatusData>>()

                        // Geotab Object Hydrators:
                        .AddSingleton<IGenericGeotabObjectHydrator<Controller>, GenericGeotabObjectHydrator<Controller>>()
                        .AddSingleton<IGenericGeotabObjectHydrator<Device>, GenericGeotabObjectHydrator<Device>>()
                        .AddSingleton<IGenericGeotabObjectHydrator<Diagnostic>, GenericGeotabObjectHydrator<Diagnostic>>()
                        .AddSingleton<IGenericGeotabObjectHydrator<FailureMode>, GenericGeotabObjectHydrator<FailureMode>>()

                        // Services:
                        .AddHostedService<Orchestrator>()
                        .AddHostedService<BinaryDataProcessor>()
                        .AddHostedService<ChargeEventProcessor>()
                        .AddHostedService<ControllerProcessor>()
                        .AddHostedService<DebugDataProcessor>()
                        .AddHostedService<DeviceProcessor>()
                        .AddHostedService<DeviceStatusInfoProcessor>()
                        .AddHostedService<DiagnosticProcessor>()
                        .AddHostedService<DriverChangeProcessor>()
                        .AddHostedService<DutyStatusAvailabilityProcessor>()
                        .AddHostedService<DutyStatusLogProcessor>()
                        .AddHostedService<DVIRLogManipulator>()
                        .AddHostedService<DVIRLogProcessor>()
                        .AddHostedService<ExceptionEventProcessor>()
                        .AddHostedService<FailureModeProcessor>()
                        .AddHostedService<FaultDataProcessor>()
                        .AddHostedService<GroupProcessor>()
                        .AddHostedService<LogRecordProcessor>()
                        .AddHostedService<OVDSClientWorker>()
                        .AddHostedService<RuleProcessor>()
                        .AddHostedService<StatusDataProcessor>()
                        .AddHostedService<TripProcessor>()
                        .AddHostedService<UnitOfMeasureProcessor>()
                        .AddHostedService<UserProcessor>()
                        .AddHostedService<ZoneProcessor>()
                        .AddHostedService<ZoneTypeProcessor>()
                        ;
                });

    }
}
