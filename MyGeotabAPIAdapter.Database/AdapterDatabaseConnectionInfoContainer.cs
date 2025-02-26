using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Logging;
using NLog;
using System;
using System.Reflection;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// A container class to hold <see cref="ConnectionInfo"/> instances for use throughout this project.
    /// </summary>
    public class AdapterDatabaseConnectionInfoContainer : IAdapterDatabaseConnectionInfoContainer
    {
        readonly IAdapterConfiguration adapterConfiguration;
        readonly IExceptionHelper exceptionHelper;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public ConnectionInfo AdapterDatabaseConnectionInfo { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterDatabaseConnectionInfoContainer"/> class.
        /// </summary>
        public AdapterDatabaseConnectionInfoContainer(IAdapterConfiguration adapterConfiguration, IExceptionHelper exceptionHelper)
        {
            this.adapterConfiguration = adapterConfiguration;
            this.exceptionHelper = exceptionHelper;

            try
            {
                // Instantiate the ConnectionInfo object.
                AdapterDatabaseConnectionInfo = new ConnectionInfo(adapterConfiguration.DatabaseConnectionString, adapterConfiguration.DatabaseProviderType, Databases.AdapterDatabase, adapterConfiguration.TimeoutSecondsForDatabaseTasks);
            }
            catch (Exception ex)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"An exception was encountered while attempting to instantiate {nameof(ConnectionInfo)} objects.");
                throw;
            }
        }
    }
}
