using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// A class that validates the adapter database to make sure that the database version matches the required database version associated with the application version.
    /// </summary>
    public class DatabaseValidator : IDatabaseValidator
    {
        // The required version of the middleware database for the current version of the middleware application. Any time the middleware database is updated as part of an application update:
        // 1. This value should be updated to reflect the application version at the time.
        // 2. Database changes should be included in a single script file and the filename should be formatted as "prefix_version_suffix.sql" (e.g. "MSSQL_3.0.0.0_InitialSchemaCreation.sql") where the version portion of the filename is equal to the value of this constant.
        const string RequiredDatabaseVersion = "3.0.0.0";

        readonly IExceptionHelper exceptionHelper;
        readonly Logger logger = LogManager.GetCurrentClassLogger();
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseValidator"/> class.
        /// </summary>
        public DatabaseValidator(IExceptionHelper exceptionHelper, IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> context)
        {
            this.exceptionHelper = exceptionHelper;
            this.context = context;
        }

        /// <inheritdoc/>
        public void ValidateDatabaseVersion()
        {
            try
            {
                string applictionName = Assembly.GetEntryAssembly()?.GetName().Name;
                string applicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var dbMiddlewareVersionInfo2Repo = new BaseRepository<DbMiddlewareVersionInfo2>(context);
                    var dbMiddlewareVersionInfo2s = dbMiddlewareVersionInfo2Repo.GetAllAsync(cancellationTokenSource, null, null, "", true, context).ConfigureAwait(true).GetAwaiter().GetResult();
                    var latestDbMiddlewareVersionInfo2 = dbMiddlewareVersionInfo2s.Last();
                    if (latestDbMiddlewareVersionInfo2.DatabaseVersion != RequiredDatabaseVersion)
                    { 
                        throw new Exception($"The middleware database version is '{latestDbMiddlewareVersionInfo2.DatabaseVersion}', but the required version is '{RequiredDatabaseVersion}'. Please update the middleware database to the required version.");
                    }
                    logger.Info($"******** APPLICATION INFO: [Application Name: '{applictionName}' | Application Version: '{applicationVersion}' | Middleware Database Version: '{latestDbMiddlewareVersionInfo2.DatabaseVersion}']");
                }
            }
            catch (Exception ex)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Error, "An exception was encountered while attempting to validate the adapter database version.");
                throw;
            }
        }
    }
}
