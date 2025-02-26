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
    public class DataOptimizerDatabaseConnectionInfoContainer : IDataOptimizerDatabaseConnectionInfoContainer
    {
        readonly IDataOptimizerConfiguration dataOptimizerConfiguration;
        readonly IExceptionHelper exceptionHelper;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public ConnectionInfo AdapterDatabaseConnectionInfo { get; }

        /// <inheritdoc/>
        public ConnectionInfo OptimizerDatabaseConnectionInfo { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOptimizerDatabaseConnectionInfoContainer"/> class.
        /// </summary>
        public DataOptimizerDatabaseConnectionInfoContainer(IDataOptimizerConfiguration dataOptimizerConfiguration, IExceptionHelper exceptionHelper)
        {
            this.dataOptimizerConfiguration = dataOptimizerConfiguration;
            this.exceptionHelper = exceptionHelper;

            try
            {
                // Instantiate the ConnectionInfo objects.
                AdapterDatabaseConnectionInfo = new ConnectionInfo(dataOptimizerConfiguration.AdapterDatabaseConnectionString, dataOptimizerConfiguration.AdapterDatabaseProviderType, Databases.AdapterDatabase, dataOptimizerConfiguration.TimeoutSecondsForDatabaseTasks);
                OptimizerDatabaseConnectionInfo = new ConnectionInfo(dataOptimizerConfiguration.OptimizerDatabaseConnectionString, dataOptimizerConfiguration.OptimizerDatabaseProviderType, Databases.OptimizerDatabase, dataOptimizerConfiguration.TimeoutSecondsForDatabaseTasks);
            }
            catch (Exception ex)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"An exception was encountered while attempting to instantiate {nameof(ConnectionInfo)} objects.");
                throw;
            }
        }
    }
}
