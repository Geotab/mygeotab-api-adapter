using Geotab.Checkmate.ObjectModel;
using Microsoft.Extensions.Hosting;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.EntityPersisters;
using MyGeotabAPIAdapter.Database.Enums;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Exceptions;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.Logging;
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
    /// A <see cref="BackgroundService"/> that extracts <see cref="DVIRLog"/> objects from a MyGeotab database and inserts/updates corresponding records in the Adapter database. 
    /// </summary>
    class DVIRLogProcessor2 : BackgroundService
    {
        bool feedVersionRollbackRequired = false;

        string CurrentClassName { get => $"{GetType().Assembly.GetName().Name}.{GetType().Name} (v{GetType().Assembly.GetName().Version})"; }
        string DefaultErrorMessagePrefix { get => $"{CurrentClassName} process caught an exception"; }

        // Polly-related items:
        const int MaxRetries = 10;
        readonly AsyncRetryPolicy asyncRetryPolicyForDatabaseTransactions;

        DateTime defectListPartDefect2CacheExpiryTime = DateTime.MinValue;
        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment;
        readonly IBackgroundServiceAwaiter<DVIRLogProcessor2> awaiter;
        readonly IBaseRepository<DbDevice2> dbDevice2Repo;
        readonly IBaseRepository<DbStgDVIRDefect2> dbStgDVIRDefect2Repo;
        readonly IBaseRepository<DbStgDVIRDefectRemark2> dbStgDVIRDefectRemark2Repo;
        readonly IBaseRepository<DbStgDVIRLog2> dbStgDVIRLog2Repo;
        readonly IDictionary<Id, DefectListPartDefect2> defectListPartDefect2sDictionary;
        readonly IExceptionHelper exceptionHelper;
        readonly IForeignKeyServiceDependencyMap dvirLogForeignKeyServiceDependencyMap;
        readonly IGenericEntityPersister<DbStgDVIRLog2> dbStgDVIRLog2EntityPersister;
        readonly IGenericEntityPersister<DbStgDVIRDefect2> dbStgDVIRDefect2EntityPersister;
        readonly IGenericEntityPersister<DbStgDVIRDefectRemark2> dbStgDVIRDefectRemark2EntityPersister;
        readonly IGenericGeotabObjectFeeder<DVIRLog> dvirLogGeotabObjectFeeder;
        readonly IGeotabDeviceFilterer geotabDeviceFilterer;
        readonly IGeotabIdConverter geotabIdConverter;
        readonly IGeotabDVIRLogDbStgDVIRLog2ObjectMapper geotabDVIRLogDbStgDVIRLog2ObjectMapper;
        readonly IGeotabDVIRDefectDbStgDVIRDefect2ObjectMapper geotabDVIRDefectDbStgDVIRDefect2ObjectMapper;
        readonly IGeotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper geotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper;
        readonly IMyGeotabAPIHelper myGeotabAPIHelper;
        readonly IServiceTracker<DbOServiceTracking2> serviceTracker;
        readonly IStateMachine2<DbMyGeotabVersionInfo2> stateMachine;

        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DVIRLogProcessor2"/> class.
        /// </summary>
        public DVIRLogProcessor2(IAdapterConfiguration adapterConfiguration, IAdapterEnvironment<DbOServiceTracking2> adapterEnvironment, IBackgroundServiceAwaiter<DVIRLogProcessor2> awaiter, IExceptionHelper exceptionHelper, IGenericEntityPersister<DbStgDVIRLog2> dbStgDVIRLog2EntityPersister, IGenericEntityPersister<DbStgDVIRDefect2> dbStgDVIRDefect2EntityPersister, IGenericEntityPersister<DbStgDVIRDefectRemark2> dbStgDVIRDefectRemark2EntityPersister, IGenericGeotabObjectFeeder<DVIRLog> dvirLogGeotabObjectFeeder, IGeotabDeviceFilterer geotabDeviceFilterer, IGeotabIdConverter geotabIdConverter, IGeotabDVIRLogDbStgDVIRLog2ObjectMapper geotabDVIRLogDbStgDVIRLog2ObjectMapper, IGeotabDVIRDefectDbStgDVIRDefect2ObjectMapper geotabDVIRDefectDbStgDVIRDefect2ObjectMapper, IGeotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper geotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper, IMyGeotabAPIHelper myGeotabAPIHelper, IServiceTracker<DbOServiceTracking2> serviceTracker, IStateMachine2<DbMyGeotabVersionInfo2> stateMachine, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.adapterEnvironment = adapterEnvironment;
            this.awaiter = awaiter;
            this.exceptionHelper = exceptionHelper;
            this.dbStgDVIRLog2EntityPersister = dbStgDVIRLog2EntityPersister;
            this.dbStgDVIRDefect2EntityPersister = dbStgDVIRDefect2EntityPersister;
            this.dbStgDVIRDefectRemark2EntityPersister = dbStgDVIRDefectRemark2EntityPersister;
            this.dvirLogGeotabObjectFeeder = dvirLogGeotabObjectFeeder;
            this.geotabDeviceFilterer = geotabDeviceFilterer;
            this.geotabIdConverter = geotabIdConverter;
            this.geotabDVIRLogDbStgDVIRLog2ObjectMapper = geotabDVIRLogDbStgDVIRLog2ObjectMapper;
            this.geotabDVIRDefectDbStgDVIRDefect2ObjectMapper = geotabDVIRDefectDbStgDVIRDefect2ObjectMapper;
            this.geotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper = geotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper;
            this.myGeotabAPIHelper = myGeotabAPIHelper;
            this.serviceTracker = serviceTracker;
            this.stateMachine = stateMachine;

            dbDevice2Repo = new BaseRepository<DbDevice2>(adapterContext);
            dbStgDVIRLog2Repo = new BaseRepository<DbStgDVIRLog2>(adapterContext);
            dbStgDVIRDefect2Repo = new BaseRepository<DbStgDVIRDefect2>(adapterContext);
            dbStgDVIRDefectRemark2Repo = new BaseRepository<DbStgDVIRDefectRemark2>(adapterContext);
            defectListPartDefect2sDictionary = new Dictionary<Id, DefectListPartDefect2>();

            this.adapterContext = adapterContext;
            logger.Debug($"{nameof(AdapterDatabaseUnitOfWorkContext)} [Id: {adapterContext.Id}] associated with {CurrentClassName}.");

            // Setup a database transaction retry policy.
            asyncRetryPolicyForDatabaseTransactions = DatabaseResilienceHelper.CreateAsyncRetryPolicyForDatabaseTransactions<Exception>(logger);

            // Setup the foreign key service dependency map.
            dvirLogForeignKeyServiceDependencyMap = new ForeignKeyServiceDependencyMap(
                [
                    new ForeignKeyServiceDependency("FK_DVIRLogs2_Devices2", AdapterService.DeviceProcessor2)
                ]
            );
        }

        /// <summary>
        /// Creates a <see cref="DefectListPartDefect"/> using the input parameter values and adds it to the <paramref name="defectListPartDefect2sDictionary"/>.
        /// </summary>
        /// <param name="defectListAssetType">The value for <see cref="DefectListPartDefect.DefectListAssetType"/>.</param>
        /// <param name="defectListID">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectListName">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="partID">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="partName">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectID">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectName">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectSeverity">The value for <see cref="DefectListPartDefect"/>.</param>
        /// <param name="defectListPartDefect2sDictionary">The <see cref="IDictionary{TKey, TValue}"/> to which the new <see cref="DefectListPartDefect2"/> is to be added.</param>
        /// <returns></returns>
        void AddDefectListPartDefect2ToList(string defectListAssetType, string defectListID, string defectListName, string partID, string partName, string defectID, string defectName, short? defectSeverity, IDictionary<Id, DefectListPartDefect2> defectListPartDefect2sDictionary)
        {
            var defectListPartDefect2 = new DefectListPartDefect2
            {
                DefectListAssetType = defectListAssetType,
                DefectListID = defectListID,
                DefectListName = defectListName,
                PartID = partID,
                PartName = partName,
                DefectID = defectID,
                DefectName = defectName,
                DefectSeverity = defectSeverity
            };

            // Note: Use the defectListPartDefect2.DefectID as the Id for this dictionary since it is the DefectId that is used for searching for DefectListPartDefect2s from this dictionary.
            defectListPartDefect2sDictionary.Add(Id.Create(defectListPartDefect2.DefectID), defectListPartDefect2);

            logger.Debug($"DefectListId {defectListPartDefect2.DefectListID}|DefectListName {defectListPartDefect2.DefectListName}|DefectListAssetType {defectListPartDefect2.DefectListAssetType}|PartId {defectListPartDefect2.PartID}|PartName {defectListPartDefect2.PartName}|DefectId {defectListPartDefect2.DefectID}|DefectName {defectListPartDefect2.DefectName}|DefectSeverity {defectListPartDefect2.DefectSeverity}");
        }

        /// <summary>
        /// Iteratively executes the business logic until the service is stopped.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // DVIRLogs2:
            const string MergeFunctionSQL_Postgres_DVIRLogs2 = @"SELECT public.""spMerge_stg_DVIRLogs2""();";
            const string MergeProcedureSQL_SQLServer_DVIRLogs2 = @"EXEC [dbo].[spMerge_stg_DVIRLogs2];";
            const string TruncateStagingTableSQL_Postgres_DVIRLogs2 = @"TRUNCATE TABLE public.""stg_DVIRLogs2"";";
            const string TruncateStagingTableSQL_SQLServer_DVIRLogs2 = @"TRUNCATE TABLE [dbo].[stg_DVIRLogs2];";
            // DVIRDefects2:
            const string MergeFunctionSQL_Postgres_DVIRDefects2 = @"SELECT public.""spMerge_stg_DVIRDefects2""();";
            const string MergeProcedureSQL_SQLServer_DVIRDefects2 = @"EXEC [dbo].[spMerge_stg_DVIRDefects2];";
            const string TruncateStagingTableSQL_Postgres_DVIRDefects2 = @"TRUNCATE TABLE public.""stg_DVIRDefects2"";";
            const string TruncateStagingTableSQL_SQLServer_DVIRDefects2 = @"TRUNCATE TABLE [dbo].[stg_DVIRDefects2];";
            //DVIRDefectRemarks2:
            const string MergeFunctionSQL_Postgres_DVIRDefectRemarks2 = @"SELECT public.""spMerge_stg_DVIRDefectRemarks2""();";
            const string MergeProcedureSQL_SQLServer_DVIRDefectRemarks2 = @"EXEC [dbo].[spMerge_stg_DVIRDefectRemarks2];";
            const string TruncateStagingTableSQL_Postgres_DVIRDefectRemarks2 = @"TRUNCATE TABLE public.""stg_DVIRDefectRemarks2"";";
            const string TruncateStagingTableSQL_SQLServer_DVIRDefectRemarks2 = @"TRUNCATE TABLE [dbo].[stg_DVIRDefectRemarks2];";

            MethodBase methodBase = MethodBase.GetCurrentMethod();
            var delayTimeSpan = TimeSpan.FromSeconds(adapterConfiguration.DVIRLogFeedIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait if necessary.
                var prerequisiteServices = new List<AdapterService>
                {
                    AdapterService.DatabaseMaintenanceService2,
                    AdapterService.DeviceProcessor2,
                    AdapterService.UserProcessor2
                };
                await awaiter.WaitForPrerequisiteServicesIfNeededAsync(prerequisiteServices, stoppingToken);
                await awaiter.WaitForDatabaseMaintenanceCompletionIfNeededAsync(stoppingToken);
                var connectivityRestored = await awaiter.WaitForConnectivityRestorationIfNeededAsync(stoppingToken);
                if (connectivityRestored == true)
                {
                    feedVersionRollbackRequired = true;
                    connectivityRestored = false;
                }

                try
                {
                    logger.Trace($"Started iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");

                    using (var cancellationTokenSource = new CancellationTokenSource())
                    {
                        var dbOServiceTracking = await serviceTracker.GetDVIRLogService2InfoAsync();

                        // Initialize the Geotab object feeder.
                        if (dvirLogGeotabObjectFeeder.IsInitialized == false)
                        {
                            await dvirLogGeotabObjectFeeder.InitializeAsync(cancellationTokenSource, adapterConfiguration.DVIRLogFeedIntervalSeconds, myGeotabAPIHelper.GetFeedResultLimitDefault, (long?)dbOServiceTracking.LastProcessedFeedVersion);
                        }

                        // If this is the first iteration after a connectivity disruption, roll-back the LastFeedVersion of the GeotabObjectFeeder to the last processed feed version that was committed to the database and set the LastFeedRetrievalTimeUtc to DateTime.MinValue to start processing without further delay.
                        if (feedVersionRollbackRequired == true)
                        {
                            dvirLogGeotabObjectFeeder.Rollback(dbOServiceTracking.LastProcessedFeedVersion);
                            feedVersionRollbackRequired = false;
                        }

                        // Update the DefectListPartDefect cache.
                        await UpdateDefectListPartDefect2sDictionaryAsync(cancellationTokenSource);

                        // Get a batch of DVIRLog objects from Geotab.
                        await dvirLogGeotabObjectFeeder.GetFeedDataBatchAsync(cancellationTokenSource);
                        stoppingToken.ThrowIfCancellationRequested();

                        // Process any returned DVIRLogs.
                        var dvirLogs = dvirLogGeotabObjectFeeder.GetFeedResultDataValuesList();
                        var dbStgDVIRLog2sToPersist = new List<DbStgDVIRLog2>();
                        var dbStgDVIRDefect2sToPersist = new List<DbStgDVIRDefect2>();
                        var dbStgDVIRDefectRemark2sToPersist = new List<DbStgDVIRDefectRemark2>();
                        if (dvirLogs.Count != 0)
                        {
                            // Apply tracked device filter and/or tracked diagnostic filter (if configured in appsettings.json).
                            var filteredDVIRLogs = await geotabDeviceFilterer.ApplyDeviceFilterAsync(cancellationTokenSource, dvirLogs);

                            // Map the DVIRLog entities to DbStgDVIRLog2 entities. Also, process any associated DVIRDefect and DVIRDefectRemark entities.
                            foreach (var filteredDVIRLog in filteredDVIRLogs)
                            {
                                // Create a DbStgDVIRLog2 entity from the DVIRLog entity. First, convert the string GeotabId values to long values, where applicable, and then map the properties.
                                long? dvirLogCertifiedByUserId = null;
                                if (filteredDVIRLog.CertifiedBy != null)
                                {
                                    if (filteredDVIRLog.CertifiedBy.GetType() == typeof(NoDriver))
                                    {
                                        dvirLogCertifiedByUserId = AdapterDbSentinelIdsForMYGKnownIds.NoDriverId;
                                    }
                                    else if (filteredDVIRLog.CertifiedBy.GetType() == typeof(UnknownDriver))
                                    {
                                        dvirLogCertifiedByUserId = AdapterDbSentinelIdsForMYGKnownIds.UnknownDriverId;
                                    }
                                    else if (filteredDVIRLog.CertifiedBy.GetType() == typeof(NoUser))
                                    {
                                        dvirLogCertifiedByUserId = AdapterDbSentinelIdsForMYGKnownIds.NoUserId;
                                    }
                                    else if (filteredDVIRLog.CertifiedBy.Id != null)
                                    {
                                        dvirLogCertifiedByUserId = geotabIdConverter.ToLong(filteredDVIRLog.CertifiedBy.Id);
                                    }
                                }

                                long? dvirLogDeviceId = null;
                                if (filteredDVIRLog.Device != null)
                                {
                                    if (filteredDVIRLog.Device.GetType() == typeof(NoDevice))
                                    {
                                        dvirLogDeviceId = AdapterDbSentinelIdsForMYGKnownIds.NoDeviceId;
                                    }
                                    else if (filteredDVIRLog.Device.Id != null)
                                    {
                                        dvirLogDeviceId = geotabIdConverter.ToLong(filteredDVIRLog.Device.Id);
                                    }
                                }
                                // Temporary logic to get DeviceId associated with a Trailer (which will be deprecated in the future).
                                if (dvirLogDeviceId == null && filteredDVIRLog.Trailer != null && filteredDVIRLog.Trailer.GetType() != typeof(NoTrailer) && filteredDVIRLog.Trailer.Id != null)
                                {
                                    dvirLogDeviceId = await GetTrailerDeviceIdAsync(filteredDVIRLog.Trailer, cancellationTokenSource);
                                }
                                if (dvirLogDeviceId == null)
                                {
                                    logger.Warn($"Could not process {nameof(DVIRLog)} with GeotabId '{filteredDVIRLog.Id}' because its {nameof(DVIRLog.Device)} is null.");
                                    continue;
                                }

                                long? dvirLogDriverId = null;
                                if (filteredDVIRLog.Driver != null)
                                { 
                                    if (filteredDVIRLog.Driver.GetType() == typeof(NoDriver))
                                    {
                                        dvirLogDriverId = AdapterDbSentinelIdsForMYGKnownIds.NoDriverId;
                                    }
                                    else if (filteredDVIRLog.Driver.GetType() == typeof(UnknownDriver))
                                    {
                                        dvirLogDriverId = AdapterDbSentinelIdsForMYGKnownIds.UnknownDriverId;
                                    }
                                    else if (filteredDVIRLog.Driver.GetType() == typeof(NoUser))
                                    {
                                        dvirLogDriverId = AdapterDbSentinelIdsForMYGKnownIds.NoUserId;
                                    }
                                    else if (filteredDVIRLog.Driver.Id != null)
                                    {
                                        dvirLogDriverId = geotabIdConverter.ToLong(filteredDVIRLog.Driver.Id);
                                    }
                                }

                                long? dvirLogRepairedByUserId = null;
                                if (filteredDVIRLog.RepairedBy != null)
                                {
                                    if (filteredDVIRLog.RepairedBy.GetType() == typeof(NoDriver))
                                    {
                                        dvirLogRepairedByUserId = AdapterDbSentinelIdsForMYGKnownIds.NoDriverId;
                                    }
                                    else if (filteredDVIRLog.RepairedBy.GetType() == typeof(UnknownDriver))
                                    {
                                        dvirLogRepairedByUserId = AdapterDbSentinelIdsForMYGKnownIds.UnknownDriverId;
                                    }
                                    else if (filteredDVIRLog.RepairedBy.GetType() == typeof(NoUser))
                                    {
                                        dvirLogRepairedByUserId = AdapterDbSentinelIdsForMYGKnownIds.NoUserId;
                                    }
                                    else if (filteredDVIRLog.RepairedBy.Id != null)
                                    {
                                        dvirLogRepairedByUserId = geotabIdConverter.ToLong(filteredDVIRLog.RepairedBy.Id);
                                    }
                                }

                                var dbStgDVIRLog2 = geotabDVIRLogDbStgDVIRLog2ObjectMapper.CreateEntity(filteredDVIRLog, dvirLogCertifiedByUserId, (long)dvirLogDeviceId, dvirLogDriverId, dvirLogRepairedByUserId);
                                dbStgDVIRLog2sToPersist.Add(dbStgDVIRLog2);

                                // Process any DVIRDefects associated with the subject DVIRLog.
                                if (filteredDVIRLog.DVIRDefects != null)
                                {
                                    foreach (var dvirDefect in filteredDVIRLog.DVIRDefects)
                                    {
                                        Defect defect = dvirDefect.Defect;
                                        DbDVIRDefect existingDbDVIRDefectForRemarkProcessing = null;

                                        // Get the DefectListPartDefect2 associated with the subject Defect.
                                        if (defectListPartDefect2sDictionary.TryGetValue(defect.Id, out var defectListPartDefect2))
                                        {
                                            var dbStgDVIRDefect2 = geotabDVIRDefectDbStgDVIRDefect2ObjectMapper.CreateEntity(dbStgDVIRLog2, dvirDefect, defect, defectListPartDefect2);
                                            dbStgDVIRDefect2sToPersist.Add(dbStgDVIRDefect2);

                                            // Process any DefectRemarks associated with the subject DVIRDefect.
                                            if (dvirDefect.DefectRemarks != null)
                                            {
                                                foreach (var dvirDefectRemark in dvirDefect.DefectRemarks)
                                                {
                                                    var dbStgDVIRDefectRemark2 = geotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper.CreateEntity(dbStgDVIRDefect2, dvirDefectRemark);
                                                    dbStgDVIRDefectRemark2sToPersist.Add(dbStgDVIRDefectRemark2);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            logger.Debug($"No DefectListPartDefect could be found for the Defect '{defect.Id} ({defect.Name})' associated with the DVIRDefect '{dvirDefect.Id}' of DVIRLog '{filteredDVIRLog.Id}'. This DVIRDefect will not be processed.");
                                        }
                                    }
                                }
                            }
                        }

                        stoppingToken.ThrowIfCancellationRequested();

                        // Persist changes to database. Step 1: Persist the DbStgDVIRLog2, DbStgDVIRDefect2 and DbStgDVIRDefectRemark2 entities.
                        if (dbStgDVIRLog2sToPersist.Count != 0)
                        {
                            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                            {
                                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                                {
                                    try
                                    {
                                        // Truncate staging tables in case they contain any data:
                                        (string truncateDVIRLogs2Sql, string truncateDVIRDefects2Sql, string truncateDVIRDefectRemarks2Sql) = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => (TruncateStagingTableSQL_Postgres_DVIRLogs2, TruncateStagingTableSQL_Postgres_DVIRDefects2, TruncateStagingTableSQL_Postgres_DVIRDefectRemarks2),
                                            ConnectionInfo.DataAccessProviderType.SQLServer => (TruncateStagingTableSQL_SQLServer_DVIRLogs2, TruncateStagingTableSQL_SQLServer_DVIRDefects2, TruncateStagingTableSQL_SQLServer_DVIRDefectRemarks2),
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };
                                        await dbStgDVIRLog2Repo.ExecuteAsync(truncateDVIRLogs2Sql, null, cancellationTokenSource, true, adapterContext);
                                        await dbStgDVIRDefect2Repo.ExecuteAsync(truncateDVIRDefects2Sql, null, cancellationTokenSource, true, adapterContext);
                                        await dbStgDVIRDefectRemark2Repo.ExecuteAsync(truncateDVIRDefectRemarks2Sql, null, cancellationTokenSource, true, adapterContext);

                                        // Bulk-insert into the staging tables:
                                        await dbStgDVIRLog2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgDVIRLog2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                                        await dbStgDVIRDefect2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgDVIRDefect2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);
                                        await dbStgDVIRDefectRemark2EntityPersister.PersistEntitiesToDatabaseAsync(adapterContext, dbStgDVIRDefectRemark2sToPersist, cancellationTokenSource, Logging.LogLevel.Info);

                                        // Commit transactions:
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
                        }

                        // Persist changes to database. Step 2: Merge entities from the staging tables into the production tables and update the DbOServiceTracking table.
                        await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
                        {
                            using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                            {
                                try
                                {
                                    if (dbStgDVIRLog2sToPersist.Count != 0)
                                    {
                                        // Build the SQL statements to execute the merge procedures.
                                        (string mergeStgDVIRLogs2Sql, string mergeStgDVIRDefects2Sql, string mergeStgDVIRDefectRemarks2Sql) = adapterContext.ProviderType switch
                                        {
                                            ConnectionInfo.DataAccessProviderType.PostgreSQL => (MergeFunctionSQL_Postgres_DVIRLogs2, MergeFunctionSQL_Postgres_DVIRDefects2, MergeFunctionSQL_Postgres_DVIRDefectRemarks2),
                                            ConnectionInfo.DataAccessProviderType.SQLServer => (MergeProcedureSQL_SQLServer_DVIRLogs2, MergeProcedureSQL_SQLServer_DVIRDefects2, MergeProcedureSQL_SQLServer_DVIRDefectRemarks2),
                                            _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
                                        };

                                        // Execute the merge procedures.
                                        await dbStgDVIRLog2Repo.ExecuteAsync(mergeStgDVIRLogs2Sql, null, cancellationTokenSource);
                                        await dbStgDVIRDefect2Repo.ExecuteAsync(mergeStgDVIRDefects2Sql, null, cancellationTokenSource);
                                        await dbStgDVIRDefectRemark2Repo.ExecuteAsync(mergeStgDVIRDefectRemarks2Sql, null, cancellationTokenSource);
                                    }

                                    // DbOServiceTracking:
                                    if (dbStgDVIRLog2sToPersist.Count != 0)
                                    {
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DVIRLogProcessor2, dvirLogGeotabObjectFeeder.LastFeedRetrievalTimeUtc, dvirLogGeotabObjectFeeder.LastFeedVersion);
                                    }
                                    else
                                    {
                                        // No DVIRLogs were returned, but the OServiceTracking record for this service still needs to be updated to show that the service is operating.
                                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DVIRLogProcessor2, DateTime.UtcNow);
                                    }

                                    // Commit transactions:
                                    await adapterUOW.CommitAsync();
                                }
                                catch (Exception ex)
                                {
                                    feedVersionRollbackRequired = true;
                                    await adapterUOW.RollBackAsync();
                                    exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                                    throw;
                                }
                            }
                        }, new Context());

                        // Clear FeedResultData.
                        dvirLogGeotabObjectFeeder.FeedResultData.Clear();
                    }

                    logger.Trace($"Completed iteration of {methodBase.ReflectedType.Name}.{methodBase.Name}");
                }
                catch (OperationCanceledException)
                {
                    string errorMessage = $"{CurrentClassName} process cancelled.";
                    logger.Warn(errorMessage);
                    throw new Exception(errorMessage);
                }
                catch (AdapterDatabaseConnectionException databaseConnectionException)
                {
                    exceptionHelper.LogException(databaseConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(databaseConnectionException, NLogLogLevelName.Error);
                }
                catch (MyGeotabConnectionException myGeotabConnectionException)
                {
                    exceptionHelper.LogException(myGeotabConnectionException, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                    stateMachine.HandleException(myGeotabConnectionException, NLogLogLevelName.Error);

                }
                catch (Exception ex)
                {
                    var exceptionToAnalyze = ex.InnerException ?? ex;
                    if (ForeignKeyExceptionHelper.IsForeignKeyViolationException(exceptionToAnalyze))
                    {
                        var violatedConstraint = ForeignKeyExceptionHelper.GetConstraintNameFromException(exceptionToAnalyze);
                        if (!string.IsNullOrEmpty(violatedConstraint) && dvirLogForeignKeyServiceDependencyMap.TryGetDependency(violatedConstraint, out AdapterService prerequisiteService))
                        {
                            await awaiter.WaitForPrerequisiteServiceToProcessEntitiesAsync(prerequisiteService, stoppingToken);
                            // After waiting, this iteration's attempt is considered "handled" by waiting. The next iteration will be the actual retry of the operation.
                            logger.Debug($"Iteration handling for FK violation on '{violatedConstraint}' complete (waited for {prerequisiteService}). Ready for next iteration.");
                        }
                        else
                        {
                            // FK violation occurred, but constraint name not found OR not included in the dependency map.
                            string reason = string.IsNullOrEmpty(violatedConstraint) ? "constraint name not extractable" : $"constraint '{violatedConstraint}' not included in dvirLogForeignKeyServiceDependencyMap";
                            exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, $"{DefaultErrorMessagePrefix} Unhandled FK violation: {reason}.");
                            stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                        }
                    }
                    else
                    {
                        // Not an FK violation. Treat as fatal.
                        exceptionHelper.LogException(ex, NLogLogLevelName.Fatal, DefaultErrorMessagePrefix);
                        stateMachine.HandleException(ex, NLogLogLevelName.Fatal);
                    }
                }

                // If the feed is up-to-date, add a delay equivalent to the configured interval.
                if (dvirLogGeotabObjectFeeder.FeedCurrent == true)
                {
                    await awaiter.WaitForConfiguredIntervalAsync(delayTimeSpan, DelayIntervalType.Feed, stoppingToken);
                }
            }
        }

        /// <summary>
        /// TEMPORARY METHOD: Retrieves the DeviceId associated with a <see cref="Trailer"/>. This is a temporary method that will be removed once <see cref="DVIRLog.Trailer"/> is deprecated and <see cref="DVIRLog.Device"/> is always populated (for Devices and Trailers). 
        /// </summary>
        /// <param name="trailer">The Trailer for which to retrieve the asscoiated Device Id.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task<long?> GetTrailerDeviceIdAsync(Trailer trailer, CancellationTokenSource cancellationTokenSource)
        {
            // Build the SQL statement to use based on database provider type.
            var sql = adapterContext.ProviderType switch
            {
                ConnectionInfo.DataAccessProviderType.SQLServer => @"SELECT * FROM [dbo].[Devices2] WHERE [TmpTrailerId] = @TmpTrailerId;",
                ConnectionInfo.DataAccessProviderType.PostgreSQL => @"SELECT * FROM public.""Devices2"" WHERE ""TmpTrailerId"" = @TmpTrailerId;",
                _ => throw new Exception($"The provider type '{adapterContext.ProviderType}' is not supported.")
            };

            // Set the parameters for the SQL query.
            var dvirLogTrailerId = geotabIdConverter.ToGuid(trailer.Id);
            var parameters = new
            {
                TmpTrailerId = dvirLogTrailerId
            };

            // Execute the SQL query to retrieve the Device associated with the Trailer.
            IEnumerable<DbDevice2> dbDevice2s = null;
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        dbDevice2s = await dbDevice2Repo.QueryAsync(sql, parameters, cancellationTokenSource);
                    }
                    catch (Exception ex)
                    {
                        exceptionHelper.LogException(ex, NLogLogLevelName.Error, DefaultErrorMessagePrefix);
                        throw;
                    }
                }
            }, new Context());

            var dbDevice2 = dbDevice2s.FirstOrDefault();
            if (dbDevice2 != null)
            {
                return dbDevice2.id;
            }
            else
            {
                logger.Warn($"No Device found for Trailer with GeotabId {trailer.Id}.");
                return null;
            }
        }

        /// <summary>
        /// Starts the current <see cref="DVIRLogProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var dbOserviceTrackings = await serviceTracker.GetDbOServiceTrackingListAsync();
            adapterEnvironment.ValidateAdapterEnvironment(dbOserviceTrackings, AdapterService.DVIRLogProcessor2, adapterConfiguration.DisableMachineNameValidation);
            await asyncRetryPolicyForDatabaseTransactions.ExecuteAsync(async pollyContext =>
            {
                using (var adapterUOW = adapterContext.CreateUnitOfWork(Databases.AdapterDatabase))
                {
                    try
                    {
                        await serviceTracker.UpdateDbOServiceTrackingRecordAsync(adapterContext, AdapterService.DVIRLogProcessor2, adapterEnvironment.AdapterVersion.ToString(), adapterEnvironment.AdapterMachineName);
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
                stateMachine.RegisterService(nameof(DVIRLogProcessor2), adapterConfiguration.EnableDVIRLogFeed);
            }

            // Only start this service if it has been configured to be enabled.
            if (adapterConfiguration.UseDataModel2 == true && adapterConfiguration.EnableDVIRLogFeed == true)
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
        /// Stops the current <see cref="DVIRLogProcessor2"/> instance.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns></returns>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // Update the registration of this service with the StateMachine. Set mustPauseForDatabaseMaintenance to false since it is stopping and will no longer be able to participate in pauses for database mainteance.
            if (adapterConfiguration.UseDataModel2 == true)
            {
                stateMachine.RegisterService(nameof(DVIRLogProcessor2), false);
            }

            logger.Info($"******** STOPPED SERVICE: {CurrentClassName} ********");
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Builds a list of <see cref="DefectListPartDefect2"/> objects by retrieving all defect lists in the database and flattening the trees - capturing the defect list, part and part defect information required when processing <see cref="DVIRDefect"/> information associated with <see cref="DVIRLog"/>s. 
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/>.</param>
        /// <returns></returns>
        async Task UpdateDefectListPartDefect2sDictionaryAsync(CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            // Only update the cache if the defectListPartDefect2CacheExpiryTime has been reached.
            var currentTime = DateTime.Now;
            if (currentTime < defectListPartDefect2CacheExpiryTime)
            {
                return;
            }

            // Clear the defectListPartDefect2s cache and set a new cache expiry time.
            defectListPartDefect2sDictionary.Clear();
            defectListPartDefect2CacheExpiryTime = currentTime.AddMinutes(adapterConfiguration.DVIRDefectListCacheRefreshIntervalMinutes);

            // Get all defect lists.
            DefectSearch defectSearch = new()
            {
                IncludeAllTrees = true
            };
            var defectLists = await myGeotabAPIHelper.GetAsync<Defect>(defectSearch, adapterConfiguration.TimeoutSecondsForMyGeotabTasks);

            cancellationToken.ThrowIfCancellationRequested();

            // Enumerate the defect lists, getting the parts associated with each defect list.
            foreach (var defectList in defectLists)
            {
                var defectListParts = defectList.Children;
                // Enumerate the parts associated with the current defect list, getting the defects associated with each part.
                foreach (var part in defectListParts)
                {
                    // First, add a virtual DefectListPartDefect2 to account for situations where the user chooses "other" when loggng a DVIRDefect (vs. selecting a part or defined defect from the lists provided). This will simplify DVIRLog processing logic later.
                    Defect virtualDefect = (Defect)part;
                    AddDefectListPartDefect2ToList(defectList.AssetType.ToString(), defectList.Id.ToString(), defectList.Name, part.Id.ToString(), part.Name, virtualDefect.Id.ToString(), virtualDefect.Name, (short?)virtualDefect.Severity, defectListPartDefect2sDictionary);

                    // Now, process the defects associated with the subject part.
                    var partDefects = part.Children;
                    foreach (var partDefect in partDefects)
                    {
                        if (partDefect is Defect defect)
                        {
                            AddDefectListPartDefect2ToList(defectList.AssetType.ToString(), defectList.Id.ToString(), defectList.Name, part.Id.ToString(), part.Name, defect.Id.ToString(), defect.Name, (short?)defect.Severity, defectListPartDefect2sDictionary);
                        }
                        else
                        {
                            logger.Debug($"partDefect '{partDefect.Id} ({partDefect.Name})' is not a Defect.");
                        }
                    }
                }
            }
        }
    }
}
