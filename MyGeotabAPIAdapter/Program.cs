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
using MyGeotabAPIAdapter.DataEnhancement;
using MyGeotabAPIAdapter.Geospatial;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using MyGeotabAPIAdapter.MyGeotabAPI;
using MyGeotabAPIAdapter.Services;
using NLog;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Options;

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
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

                var config = new ConfigurationBuilder()
                    .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
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
                        .AddTransient<IForeignKeyServiceDependencyMap, ForeignKeyServiceDependencyMap>()
                        .AddTransient<IGeospatialHelper, GeospatialHelper>()
                        .AddTransient<IGeotabIdConverter, GeotabIdConverter>()
                        .AddTransient<IHttpHelper, HttpHelper>()
                        .AddTransient<ILocationInterpolator, LocationInterpolator>()
                        .AddTransient<IMessageLogger, MessageLogger>()
                        .AddSingleton<IMyGeotabAPIHelper, MyGeotabAPIHelper>()
                        .AddTransient<IStringHelper, StringHelper>()
                        .AddSingleton<IUnmappedDiagnosticManager, UnmappedDiagnosticManager>()
                        .AddTransient<IVSSObjectMapper, VSSObjectMapper>()

                        // Models for Dependency Injection:
                        .AddTransient<DbCondition>()
                        .AddTransient<DbDevice>()
                        .AddTransient<DbDevice2>()
                        .AddTransient<DbDeviceStatusInfo>()
                        .AddTransient<DbDiagnostic>()
                        .AddTransient<DbDiagnostic2>()
                        .AddTransient<DbDiagnosticId2>()
                        .AddTransient<DbDutyStatusAvailability>()
                        .AddTransient<DbGroup>()
                        .AddTransient<DbGroup2>()
                        .AddTransient<DbOServiceTracking>()
                        .AddTransient<DbOServiceTracking2>()
                        .AddTransient<DbRule>()
                        .AddTransient<DbRule2>()
                        .AddTransient<DbUser>()
                        .AddTransient<DbUser2>()
                        .AddTransient<DbZone>()
                        .AddTransient<DbZone2>()
                        .AddTransient<DbZoneType>()
                        .AddTransient<DbZoneType2>()

                        // Unit of Work:
                        .AddTransient<AdapterDatabaseUnitOfWorkContext>()
                        .AddTransient<IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>, GenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>>()

                        // Environment:
                        .AddSingleton<IAdapterEnvironment<DbOServiceTracking>, AdapterEnvironment<DbOServiceTracking>>()
                        .AddSingleton<IAdapterEnvironment<DbOServiceTracking2>, AdapterEnvironment<DbOServiceTracking2>>()
                        .AddSingleton<IAdapterEnvironmentValidator<DbOServiceTracking>, AdapterEnvironmentValidator<DbOServiceTracking>>()
                        .AddSingleton<IAdapterEnvironmentValidator<DbOServiceTracking2>, AdapterEnvironmentValidator<DbOServiceTracking2>>()
                        .AddSingleton<IOrchestratorServiceTracker, OrchestratorServiceTracker>()
                        .AddTransient<IPrerequisiteServiceChecker<DbOServiceTracking>, PrerequisiteServiceChecker<DbOServiceTracking>>()
                        .AddTransient<IPrerequisiteServiceChecker<DbOServiceTracking2>, PrerequisiteServiceChecker<DbOServiceTracking2>>()
                        .AddSingleton<IServiceTracker<DbOServiceTracking>, ServiceTracker<DbOServiceTracking>>()
                        .AddSingleton<IServiceTracker<DbOServiceTracking2>, ServiceTracker<DbOServiceTracking2>>()
                        .AddSingleton<IStateMachine<DbMyGeotabVersionInfo>, StateMachine<DbMyGeotabVersionInfo>>()

                        // Configuration:
                        .AddSingleton<IAdapterConfiguration, AdapterConfiguration>()
                        .AddTransient<IAdapterDatabaseConnectionInfoContainer, AdapterDatabaseConnectionInfoContainer>()
                        .AddSingleton<IAdapterDatabaseObjectNames, AdapterDatabaseObjectNames>()
                        .AddTransient<IConfigurationHelper, ConfigurationHelper>()
                        .AddSingleton<IVSSConfiguration, VSSConfiguration>()

                        // Database Validator:
                        .AddSingleton<IDatabaseValidator, DatabaseValidator>()

                        // Database Entity to Database Entity Mappers:
                        .AddTransient<IDbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper, DbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper>()
                        .AddTransient<IDbFaultData2DbEntityMetadata2EntityMapper, DbFaultData2DbEntityMetadata2EntityMapper>()
                        .AddTransient<IDbLogRecord2DbEntityMetadata2EntityMapper, DbLogRecord2DbEntityMetadata2EntityMapper>()
                        .AddTransient<IDbStatusData2DbEntityMetadata2EntityMapper, DbStatusData2DbEntityMetadata2EntityMapper>()

                        // Geotab object comparers:
                        .AddTransient<IGeotabDateTimeProviderComparer<LogRecord>, GeotabDateTimeProviderComparer<LogRecord>>()
                        .AddTransient<IGeotabDateTimeProviderComparer<StatusData>, GeotabDateTimeProviderComparer<StatusData>>()

                        // Geotab Object to Database Entity Mappers:
                        .AddTransient<IGeotabBinaryDataDbBinaryDataObjectMapper, GeotabBinaryDataDbBinaryDataObjectMapper>()
                        .AddTransient<IGeotabBinaryDataDbBinaryData2ObjectMapper, GeotabBinaryDataDbBinaryData2ObjectMapper>()
                        .AddTransient<IGeotabChargeEventDbChargeEventObjectMapper, GeotabChargeEventDbChargeEventObjectMapper>()
                        .AddTransient<IGeotabChargeEventDbStgChargeEvent2ObjectMapper, GeotabChargeEventDbStgChargeEvent2ObjectMapper>()
                        .AddTransient<IGeotabDebugDataDbDebugDataObjectMapper, GeotabDebugDataDbDebugDataObjectMapper>()
                        .AddTransient<IGeotabConditionDbConditionObjectMapper, GeotabConditionDbConditionObjectMapper>()
                        .AddTransient<IGeotabDeviceDbDeviceObjectMapper, GeotabDeviceDbDeviceObjectMapper>()
                        .AddTransient<IGeotabDeviceDbStgDevice2ObjectMapper, GeotabDeviceDbStgDevice2ObjectMapper>()
                        .AddTransient<IGeotabDeviceStatusInfoDbDeviceStatusInfoObjectMapper, GeotabDeviceStatusInfoDbDeviceStatusInfoObjectMapper>()
                        .AddTransient<IGeotabDiagnosticDbDiagnosticObjectMapper, GeotabDiagnosticDbDiagnosticObjectMapper>()
                        .AddTransient<IGeotabDiagnosticDbStgDiagnostic2ObjectMapper, GeotabDiagnosticDbStgDiagnostic2ObjectMapper>()
                        .AddTransient<IGeotabDriverChangeDbDriverChangeObjectMapper, GeotabDriverChangeDbDriverChangeObjectMapper>()
                        .AddTransient<IGeotabDriverChangeDbStgDriverChange2ObjectMapper, GeotabDriverChangeDbStgDriverChange2ObjectMapper>()
                        .AddTransient<IGeotabDVIRDefectDbDVIRDefectObjectMapper, GeotabDVIRDefectDbDVIRDefectObjectMapper>()
                        .AddTransient<IGeotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper, GeotabDutyStatusAvailabilityDbDutyStatusAvailabilityObjectMapper>()
                        .AddTransient<IGeotabDutyStatusLogDbDutyStatusLogObjectMapper, GeotabDutyStatusLogDbDutyStatusLogObjectMapper>()
                        .AddTransient<IGeotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper, GeotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper>()
                        .AddTransient<IGeotabDVIRLogDbDVIRLogObjectMapper, GeotabDVIRLogDbDVIRLogObjectMapper>()
                        .AddTransient<IGeotabExceptionEventDbExceptionEventObjectMapper, GeotabExceptionEventDbExceptionEventObjectMapper>()
                        .AddTransient<IGeotabExceptionEventDbStgExceptionEvent2ObjectMapper, GeotabExceptionEventDbStgExceptionEvent2ObjectMapper>()
                        .AddTransient<IGeotabFaultDataDbFaultDataObjectMapper, GeotabFaultDataDbFaultDataObjectMapper>()
                        .AddTransient<IGeotabGroupDbGroupObjectMapper, GeotabGroupDbGroupObjectMapper>()
                        .AddTransient<IGeotabGroupDbStgGroup2ObjectMapper, GeotabGroupDbStgGroup2ObjectMapper>()
                        .AddTransient<IGeotabFaultDataDbFaultData2ObjectMapper, GeotabFaultDataDbFaultData2ObjectMapper>()
                        .AddTransient<IGeotabLogRecordDbLogRecordObjectMapper, GeotabLogRecordDbLogRecordObjectMapper>()
                        .AddTransient<IGeotabLogRecordDbLogRecord2ObjectMapper, GeotabLogRecordDbLogRecord2ObjectMapper>()
                        .AddTransient<IGeotabRuleDbRuleObjectMapper, GeotabRuleDbRuleObjectMapper>()
                        .AddTransient<IGeotabRuleDbStgRule2ObjectMapper, GeotabRuleDbStgRule2ObjectMapper>()
                        .AddTransient<IGeotabStatusDataDbStatusDataObjectMapper, GeotabStatusDataDbStatusDataObjectMapper>()
                        .AddTransient<IGeotabStatusDataDbStatusData2ObjectMapper, GeotabStatusDataDbStatusData2ObjectMapper>()
                        .AddTransient<IGeotabTripDbTripObjectMapper, GeotabTripDbTripObjectMapper>()
                        .AddTransient<IGeotabTripDbStgTrip2ObjectMapper, GeotabTripDbStgTrip2ObjectMapper>()
                        .AddTransient<IGeotabUserDbUserObjectMapper, GeotabUserDbUserObjectMapper>()
                        .AddTransient<IGeotabUserDbStgUser2ObjectMapper, GeotabUserDbStgUser2ObjectMapper>()
                        .AddTransient<IGeotabZoneDbZoneObjectMapper, GeotabZoneDbZoneObjectMapper>()
                        .AddTransient<IGeotabZoneDbStgZone2ObjectMapper, GeotabZoneDbStgZone2ObjectMapper>()
                        .AddTransient<IGeotabZoneTypeDbZoneTypeObjectMapper, GeotabZoneTypeDbZoneTypeObjectMapper>()
                        .AddTransient<IGeotabZoneTypeDbStgZoneType2ObjectMapper, GeotabZoneTypeDbStgZoneType2ObjectMapper>()

                        // Database Entity Persisters:
                        .AddTransient<IGenericEntityPersister<DbBinaryData>, GenericEntityPersister<DbBinaryData>>()
                        .AddTransient<IGenericEntityPersister<DbBinaryData2>, GenericEntityPersister<DbBinaryData2>>()
                        .AddTransient<IGenericEntityPersister<DbChargeEvent>, GenericEntityPersister<DbChargeEvent>>()
                        .AddTransient<IGenericEntityPersister<DbChargeEvent2>, GenericEntityPersister<DbChargeEvent2>>()
                        .AddTransient<IGenericEntityPersister<DbCondition>, GenericEntityPersister<DbCondition>>()
                        .AddTransient<IGenericEntityPersister<DbDBMaintenanceLog2>, GenericEntityPersister<DbDBMaintenanceLog2>>()
                        .AddTransient<IGenericEntityPersister<DbDebugData>, GenericEntityPersister<DbDebugData>>()
                        .AddTransient<IGenericEntityPersister<DbDevice>, GenericEntityPersister<DbDevice>>()
                        .AddTransient<IGenericEntityPersister<DbDevice2>, GenericEntityPersister<DbDevice2>>()
                        .AddTransient<IGenericEntityPersister<DbDeviceStatusInfo>, GenericEntityPersister<DbDeviceStatusInfo>>()
                        .AddTransient<IGenericEntityPersister<DbDiagnostic>, GenericEntityPersister<DbDiagnostic>>()
                        .AddTransient<IGenericEntityPersister<DbDiagnostic2>, GenericEntityPersister<DbDiagnostic2>>()
                        .AddTransient<IGenericEntityPersister<DbDiagnosticId2>, GenericEntityPersister<DbDiagnosticId2>>()
                        .AddTransient<IGenericEntityPersister<DbDriverChange>, GenericEntityPersister<DbDriverChange>>()
                        .AddTransient<IGenericEntityPersister<DbDriverChange2>, GenericEntityPersister<DbDriverChange2>>()
                        .AddTransient<IGenericEntityPersister<DbDutyStatusAvailability>, GenericEntityPersister<DbDutyStatusAvailability>>()
                        .AddTransient<IGenericEntityPersister<DbDutyStatusLog>, GenericEntityPersister<DbDutyStatusLog>>()
                        .AddTransient<IGenericEntityPersister<DbDVIRLog>, GenericEntityPersister<DbDVIRLog>>()
                        .AddTransient<IGenericEntityPersister<DbDVIRDefect>, GenericEntityPersister<DbDVIRDefect>>()
                        .AddTransient<IGenericEntityPersister<DbDVIRDefectRemark>, GenericEntityPersister<DbDVIRDefectRemark>>()
                        .AddTransient<IGenericEntityPersister<DbDVIRDefectUpdate>, GenericEntityPersister<DbDVIRDefectUpdate>>()
                        .AddTransient<IGenericEntityPersister<DbEntityMetadata2>, GenericEntityPersister<DbEntityMetadata2>>()
                        .AddTransient<IGenericEntityPersister<DbExceptionEvent>, GenericEntityPersister<DbExceptionEvent>>()
                        .AddTransient<IGenericEntityPersister<DbExceptionEvent2>, GenericEntityPersister<DbExceptionEvent2>>()
                        .AddTransient<IGenericEntityPersister<DbFailedDVIRDefectUpdate>, GenericEntityPersister<DbFailedDVIRDefectUpdate>>()
                        .AddTransient<IGenericEntityPersister<DbFailedOVDSServerCommand>, GenericEntityPersister<DbFailedOVDSServerCommand>>()
                        .AddTransient<IGenericEntityPersister<DbFaultData>, GenericEntityPersister<DbFaultData>>()
                        .AddTransient<IGenericEntityPersister<DbGroup>, GenericEntityPersister<DbGroup>>()
                        .AddTransient<IGenericEntityPersister<DbGroup2>, GenericEntityPersister<DbGroup2>>()
                        .AddTransient<IGenericEntityPersister<DbFaultData2>, GenericEntityPersister<DbFaultData2>>()
                        .AddTransient<IGenericEntityPersister<DbFaultDataLocation2>, GenericEntityPersister<DbFaultDataLocation2>>()
                        .AddTransient<IGenericEntityPersister<DbLogRecord>, GenericEntityPersister<DbLogRecord>>()
                        .AddTransient<IGenericEntityPersister<DbLogRecord2>, GenericEntityPersister<DbLogRecord2>>()
                        .AddTransient<IGenericEntityPersister<DbMyGeotabVersionInfo2>, GenericEntityPersister<DbMyGeotabVersionInfo2>>()
                        .AddTransient<IGenericEntityPersister<DbMyGeotabVersionInfo>, GenericEntityPersister<DbMyGeotabVersionInfo>>()
                        .AddTransient<IGenericEntityPersister<DbMyGeotabVersionInfo2>, GenericEntityPersister<DbMyGeotabVersionInfo2>>()
                        .AddSingleton<IGenericEntityPersister<DbOServiceTracking>, GenericEntityPersister<DbOServiceTracking>>()
                        .AddSingleton<IGenericEntityPersister<DbOServiceTracking2>, GenericEntityPersister<DbOServiceTracking2>>()
                        .AddTransient<IGenericEntityPersister<DbOVDSServerCommand>, GenericEntityPersister<DbOVDSServerCommand>>()
                        .AddTransient<IGenericEntityPersister<DbRule>, GenericEntityPersister<DbRule>>()
                        .AddTransient<IGenericEntityPersister<DbRule2>, GenericEntityPersister<DbRule2>>()
                        .AddTransient<IGenericEntityPersister<DbStatusData>, GenericEntityPersister<DbStatusData>>()
                        .AddTransient<IGenericEntityPersister<DbStatusData2>, GenericEntityPersister<DbStatusData2>>()
                        .AddTransient<IGenericEntityPersister<DbStatusDataLocation2>, GenericEntityPersister<DbStatusDataLocation2>>()
                        .AddTransient<IGenericEntityPersister<DbStgChargeEvent2>, GenericEntityPersister<DbStgChargeEvent2>>()
                        .AddTransient<IGenericEntityPersister<DbStgDevice2>, GenericEntityPersister<DbStgDevice2>>()
                        .AddTransient<IGenericEntityPersister<DbStgDiagnostic2>, GenericEntityPersister<DbStgDiagnostic2>>()
                        .AddTransient<IGenericEntityPersister<DbStgDriverChange2>, GenericEntityPersister<DbStgDriverChange2>>()
                        .AddTransient<IGenericEntityPersister<DbStgExceptionEvent2>, GenericEntityPersister<DbStgExceptionEvent2>>()
                        .AddTransient<IGenericEntityPersister<DbStgGroup2>, GenericEntityPersister<DbStgGroup2>>()
                        .AddTransient<IGenericEntityPersister<DbStgRule2>, GenericEntityPersister<DbStgRule2>>()
                        .AddTransient<IGenericEntityPersister<DbStgTrip2>, GenericEntityPersister<DbStgTrip2>>()
                        .AddTransient<IGenericEntityPersister<DbStgUser2>, GenericEntityPersister<DbStgUser2>>()
                        .AddTransient<IGenericEntityPersister<DbStgZone2>, GenericEntityPersister<DbStgZone2>>()
                        .AddTransient<IGenericEntityPersister<DbStgZoneType2>, GenericEntityPersister<DbStgZoneType2>>()
                        .AddTransient<IGenericEntityPersister<DbTrip>, GenericEntityPersister<DbTrip>>()
                        .AddTransient<IGenericEntityPersister<DbTrip2>, GenericEntityPersister<DbTrip2>>()
                        .AddTransient<IGenericEntityPersister<DbUser>, GenericEntityPersister<DbUser>>()
                        .AddTransient<IGenericEntityPersister<DbUser2>, GenericEntityPersister<DbUser2>>()
                        .AddTransient<IGenericEntityPersister<DbZone>, GenericEntityPersister<DbZone>>()
                        .AddTransient<IGenericEntityPersister<DbZone2>, GenericEntityPersister<DbZone2>>()
                        .AddTransient<IGenericEntityPersister<DbZoneType>, GenericEntityPersister<DbZoneType>>()
                        .AddTransient<IGenericEntityPersister<DbZoneType2>, GenericEntityPersister<DbZoneType2>>()

                        // Database Object Caches:
                        .AddSingleton<AdapterGenericDbObjectCache<DbCondition>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbCondition, AdapterGenericDbObjectCache<DbCondition>>, GenericGenericDbObjectCache<DbCondition, AdapterGenericDbObjectCache<DbCondition>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDevice>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDevice, AdapterGenericDbObjectCache<DbDevice>>, GenericGenericDbObjectCache<DbDevice, AdapterGenericDbObjectCache<DbDevice>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDevice2>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDevice2, AdapterGenericDbObjectCache<DbDevice2>>, GenericGenericDbObjectCache<DbDevice2, AdapterGenericDbObjectCache<DbDevice2>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDeviceStatusInfo>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDeviceStatusInfo, AdapterGenericDbObjectCache<DbDeviceStatusInfo>>, GenericGenericDbObjectCache<DbDeviceStatusInfo, AdapterGenericDbObjectCache<DbDeviceStatusInfo>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDiagnostic>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDiagnostic, AdapterGenericDbObjectCache<DbDiagnostic>>, GenericGenericDbObjectCache<DbDiagnostic, AdapterGenericDbObjectCache<DbDiagnostic>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDiagnosticId2>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDiagnosticId2, AdapterGenericDbObjectCache<DbDiagnosticId2>>, GenericGenericDbObjectCache<DbDiagnosticId2, AdapterGenericDbObjectCache<DbDiagnosticId2>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbDutyStatusAvailability>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbDutyStatusAvailability, AdapterGenericDbObjectCache<DbDutyStatusAvailability>>, GenericGenericDbObjectCache<DbDutyStatusAvailability, AdapterGenericDbObjectCache<DbDutyStatusAvailability>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbGroup>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbGroup, AdapterGenericDbObjectCache<DbGroup>>, GenericGenericDbObjectCache<DbGroup, AdapterGenericDbObjectCache<DbGroup>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbGroup2>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbGroup2, AdapterGenericDbObjectCache<DbGroup2>>, GenericGenericDbObjectCache<DbGroup2, AdapterGenericDbObjectCache<DbGroup2>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbOServiceTracking>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbOServiceTracking, AdapterGenericDbObjectCache<DbOServiceTracking>>, GenericGenericDbObjectCache<DbOServiceTracking, AdapterGenericDbObjectCache<DbOServiceTracking>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbOServiceTracking2>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbOServiceTracking2, AdapterGenericDbObjectCache<DbOServiceTracking2>>, GenericGenericDbObjectCache<DbOServiceTracking2, AdapterGenericDbObjectCache<DbOServiceTracking2>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbRule>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbRule, AdapterGenericDbObjectCache<DbRule>>, GenericGenericDbObjectCache<DbRule, AdapterGenericDbObjectCache<DbRule>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbRule2>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbRule2, AdapterGenericDbObjectCache<DbRule2>>, GenericGenericDbObjectCache<DbRule2, AdapterGenericDbObjectCache<DbRule2>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbUser>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbUser, AdapterGenericDbObjectCache<DbUser>>, GenericGenericDbObjectCache<DbUser, AdapterGenericDbObjectCache<DbUser>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbUser2>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbUser2, AdapterGenericDbObjectCache<DbUser2>>, GenericGenericDbObjectCache<DbUser2, AdapterGenericDbObjectCache<DbUser2>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbZone>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbZone, AdapterGenericDbObjectCache<DbZone>>, GenericGenericDbObjectCache<DbZone, AdapterGenericDbObjectCache<DbZone>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbZone2>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbZone2, AdapterGenericDbObjectCache<DbZone2>>, GenericGenericDbObjectCache<DbZone2, AdapterGenericDbObjectCache<DbZone2>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbZoneType>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbZoneType, AdapterGenericDbObjectCache<DbZoneType>>, GenericGenericDbObjectCache<DbZoneType, AdapterGenericDbObjectCache<DbZoneType>>>()
                        .AddSingleton<AdapterGenericDbObjectCache<DbZoneType2>>()
                        .AddSingleton<IGenericGenericDbObjectCache<DbZoneType2, AdapterGenericDbObjectCache<DbZoneType2>>, GenericGenericDbObjectCache<DbZoneType2, AdapterGenericDbObjectCache<DbZoneType2>>>()

                        // Database Id and Object Caches:
                        .AddSingleton<IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext>, GenericGeotabGUIDCacheableDbObjectCache2<DbDiagnosticId2, AdapterDatabaseUnitOfWorkContext>>()
                        .AddSingleton<IGenericGeotabGUIDCacheableDbObjectCache2<DbDiagnostic2, AdapterDatabaseUnitOfWorkContext>, GenericGeotabGUIDCacheableDbObjectCache2<DbDiagnostic2, AdapterDatabaseUnitOfWorkContext>>()

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
                        ;

                    // Resolve IAdapterConfiguration early to use it for conditional service registration.
                    var serviceProvider = services.BuildServiceProvider();
                    var adapterConfig = serviceProvider.GetRequiredService<IAdapterConfiguration>();

                    // Conditionally register services based on which data model is being used.
                    if (adapterConfig.UseDataModel2 == true)
                    {
                        // Run the DatabaseValidator to ensure the adapter database version is correct.
                        var databaseValidator = serviceProvider.GetRequiredService<IDatabaseValidator>();
                        databaseValidator.ValidateDatabaseVersion();

                        // Configure options for the services. This is necessary because the services are registered as hosted services and the options are used to determine whether the individual services should pause for database maintenance windows wherein operations such as reindexing could potentially cause exceptions.
                        var serviceNames = new string[] { nameof(Orchestrator2), nameof(ChargeEventProcessor2), nameof(ControllerProcessor2), nameof(DeviceProcessor2), nameof(DiagnosticProcessor2), nameof(DriverChangeProcessor2), nameof(ExceptionEventProcessor2), nameof(FailureModeProcessor2), nameof(FaultDataLocationService2), nameof(FaultDataProcessor2), nameof(GroupProcessor2), nameof(LogRecordProcessor2), nameof(RuleProcessor2), nameof(StatusDataLocationService2), nameof(StatusDataProcessor2), nameof(TripProcessor2), nameof(UnitOfMeasureProcessor2), nameof(UserProcessor2), nameof(ZoneProcessor2), nameof(ZoneTypeProcessor2) };

                        // Register the ServiceOprionsProvider.
                        services.AddSingleton<IServiceOptionsProvider, ServiceOptionsProvider>();

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
                                if (serviceName == nameof(Orchestrator2) || serviceName == nameof(ControllerProcessor2) || serviceName == nameof(UnitOfMeasureProcessor2))
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
                        services.AddSingleton<IStateMachine2<DbMyGeotabVersionInfo2>>(sp =>
                        {
                            var serviceOptionsProvider = sp.GetRequiredService<IServiceOptionsProvider>();

                            // Get all service names from configured ServiceOptions.
                            var serviceNames = serviceOptionsProvider.GetAllServiceNames();

                            // Retrieve all configured ServiceOptions using the service names.
                            var serviceOptions = serviceOptionsProvider.GetAllServiceOptions(serviceNames);

                            var adapterConfiguration = sp.GetRequiredService<IAdapterConfiguration>();
                            var myGeotabAPIHelper = sp.GetRequiredService<IMyGeotabAPIHelper>();

                            // Pass the service options and other dependencies to the StateMachine constructor.
                            return new StateMachine2<DbMyGeotabVersionInfo2>(adapterConfiguration, myGeotabAPIHelper, serviceOptions);
                        });

                        // Register the services.
                        services
                        .AddHostedService<Orchestrator2>()
                        .AddHostedService<DatabaseMaintenanceService2>()
                        .AddHostedService<BinaryDataProcessor2>()
                        .AddHostedService<ChargeEventProcessor2>()
                        .AddHostedService<ControllerProcessor2>()
                        .AddHostedService<DeviceProcessor2>()
                        .AddHostedService<DiagnosticProcessor2>()
                        .AddHostedService<DriverChangeProcessor2>()
                        .AddHostedService<ExceptionEventProcessor2>()
                        .AddHostedService<FailureModeProcessor2>()
                        .AddHostedService<FaultDataLocationService2>()
                        .AddHostedService<FaultDataProcessor2>()
                        .AddHostedService<GroupProcessor2>()
                        .AddHostedService<LogRecordProcessor2>()
                        .AddHostedService<RuleProcessor2>()
                        .AddHostedService<StatusDataLocationService2>()
                        .AddHostedService<StatusDataProcessor2>()
                        .AddHostedService<TripProcessor2>()
                        .AddHostedService<UnitOfMeasureProcessor2>()
                        .AddHostedService<UserProcessor2>()
                        .AddHostedService<ZoneProcessor2>()
                        .AddHostedService<ZoneTypeProcessor2>()
                        ;

                        // Register a BackgroundServiceAwaiter for each service.
                        services
                        .AddSingleton<IBackgroundServiceAwaiter<Orchestrator2>, BackgroundServiceAwaiter<Orchestrator2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<DatabaseMaintenanceService2>, BackgroundServiceAwaiter<DatabaseMaintenanceService2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<BinaryDataProcessor2>, BackgroundServiceAwaiter<BinaryDataProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<ChargeEventProcessor2>, BackgroundServiceAwaiter<ChargeEventProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<ControllerProcessor2>, BackgroundServiceAwaiter<ControllerProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<DeviceProcessor2>, BackgroundServiceAwaiter<DeviceProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<DiagnosticProcessor2>, BackgroundServiceAwaiter<DiagnosticProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<DriverChangeProcessor2>, BackgroundServiceAwaiter<DriverChangeProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<ExceptionEventProcessor2>, BackgroundServiceAwaiter<ExceptionEventProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<FailureModeProcessor2>, BackgroundServiceAwaiter<FailureModeProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<FaultDataLocationService2>, BackgroundServiceAwaiter<FaultDataLocationService2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<FaultDataProcessor2>, BackgroundServiceAwaiter<FaultDataProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<GroupProcessor2>, BackgroundServiceAwaiter<GroupProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<LogRecordProcessor2>, BackgroundServiceAwaiter<LogRecordProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<RuleProcessor2>, BackgroundServiceAwaiter<RuleProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<StatusDataLocationService2>, BackgroundServiceAwaiter<StatusDataLocationService2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<StatusDataProcessor2>, BackgroundServiceAwaiter<StatusDataProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<TripProcessor2>, BackgroundServiceAwaiter<TripProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<UnitOfMeasureProcessor2>, BackgroundServiceAwaiter<UnitOfMeasureProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<UserProcessor2>, BackgroundServiceAwaiter<UserProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<ZoneProcessor2>, BackgroundServiceAwaiter<ZoneProcessor2>>()
                        .AddSingleton<IBackgroundServiceAwaiter<ZoneTypeProcessor2>, BackgroundServiceAwaiter<ZoneTypeProcessor2>>()
                        ;
                    }
                    else
                    {
                        // Register the services.
                        services
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
                    }
                });
    }
}