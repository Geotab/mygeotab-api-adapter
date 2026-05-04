using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Logging;
using NLog;
using System.Reflection;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// A class that validates the DIG Adapter database (gda schema) to ensure the schema version matches the required version associated with the GeotabDIGAdapter application version.
    /// </summary>
    public class DIGAdapterDatabaseValidator : IDIGAdapterDatabaseValidator
    {
        // The required version of the DIG Adapter middleware database for the current version of the GeotabDIGAdapter application.
        // Any time the DIG Adapter database schema is updated:
        // 1. This value should be updated to reflect the application version at the time.
        // 2. Database changes should be included in a single script file and the filename should be formatted as 
        //    "prefix_version_suffix.sql" (e.g. "MSSQL_1.0.0.0_InitialSchemaCreation.sql") where the version 
        //    portion of the filename is equal to the value of this constant.
        const string RequiredDIGAdapterDatabaseVersion = "5.0.0.0";

        readonly IMyAdminExceptionHelper exceptionHelper;
        readonly IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DIGAdapterDatabaseValidator"/> class.
        /// </summary>
        public DIGAdapterDatabaseValidator(IMyAdminExceptionHelper exceptionHelper, IDIGAdapterDatabaseObjectNames digAdapterDatabaseObjectNames, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context)
        {
            this.exceptionHelper = exceptionHelper;
            this.digAdapterDatabaseObjectNames = digAdapterDatabaseObjectNames;
            this.context = context;
        }

        /// <inheritdoc/>
        public void ValidateDatabaseVersion()
        {
            try
            {
                string applicationName = Assembly.GetEntryAssembly()?.GetName().Name;
                string applicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var dbGdaMiddlewareVersionInfoRepo = new BaseRepository<DbGdaMiddlewareVersionInfo>(context);
                    var dbGdaMiddlewareVersionInfos = dbGdaMiddlewareVersionInfoRepo
                        .GetAllAsync(cancellationTokenSource, null, null, "", true, context)
                        .ConfigureAwait(true)
                        .GetAwaiter()
                        .GetResult();

                    if (!dbGdaMiddlewareVersionInfos.Any())
                    {
                        throw new Exception($"No version information found in the '{digAdapterDatabaseObjectNames.DbGdaMiddlewareVersionInfoTableName}' table. Please ensure the DIG Adapter database schema has been properly initialized.");
                    }

                    var latestDbGdaMiddlewareVersionInfo = dbGdaMiddlewareVersionInfos.Last();
                    if (latestDbGdaMiddlewareVersionInfo.DatabaseVersion != RequiredDIGAdapterDatabaseVersion)
                    {
                        throw new Exception($"The DIG Adapter middleware database version is '{latestDbGdaMiddlewareVersionInfo.DatabaseVersion}', but the required version is '{RequiredDIGAdapterDatabaseVersion}'. Please update the DIG Adapter middleware database schema to the required version.");
                    }

                    logger.Info($"******** APPLICATION INFO: [Application Name: '{applicationName}' | Application Version: '{applicationVersion}' | DIG Adapter Database Schema: '{digAdapterDatabaseObjectNames.DIGAdapterSchemaName}' | DIG Adapter Database Version: '{latestDbGdaMiddlewareVersionInfo.DatabaseVersion}']");
                }
            }
            catch (Exception ex)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"An exception was encountered while attempting to validate the DIG Adapter database version in schema '{digAdapterDatabaseObjectNames.DIGAdapterSchemaName}'.");
                throw;
            }
        }
    }
}