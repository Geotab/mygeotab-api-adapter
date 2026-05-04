using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Logging;
using NLog;
using System;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// A container class to hold <see cref="ConnectionInfo"/> instances for use throughout this project.
    /// </summary>
    public class AdapterDatabaseConnectionInfoContainer : IAdapterDatabaseConnectionInfoContainer
    {
        readonly IDatabaseConfiguration databaseConfiguration;
        readonly IExceptionHelper exceptionHelper;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public ConnectionInfo AdapterDatabaseConnectionInfo { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterDatabaseConnectionInfoContainer"/> class.
        /// </summary>
        public AdapterDatabaseConnectionInfoContainer(IDatabaseConfiguration databaseConfiguration, IExceptionHelper exceptionHelper)
        {
            this.databaseConfiguration = databaseConfiguration;
            this.exceptionHelper = exceptionHelper;

            try
            {
                // Instantiate the ConnectionInfo object.
                AdapterDatabaseConnectionInfo = new ConnectionInfo(databaseConfiguration.DatabaseConnectionString, databaseConfiguration.DatabaseProviderType, Databases.AdapterDatabase, databaseConfiguration.TimeoutSecondsForDatabaseTasks);
            }
            catch (Exception ex)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"An exception was encountered while attempting to instantiate {nameof(ConnectionInfo)} objects.");
                throw;
            }
        }
    }
}
