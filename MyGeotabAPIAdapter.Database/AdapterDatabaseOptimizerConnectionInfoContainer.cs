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
    public class AdapterDatabaseOptimizerConnectionInfoContainer : IAdapterDatabaseConnectionInfoContainer
    {
        readonly IDataOptimizerConfiguration optimizerConfiguration;
        readonly IExceptionHelper exceptionHelper;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public ConnectionInfo AdapterDatabaseConnectionInfo { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterDatabaseConnectionInfoContainer"/> class.
        /// </summary>
        public AdapterDatabaseOptimizerConnectionInfoContainer(IDataOptimizerConfiguration optimizerConfiguration, IExceptionHelper exceptionHelper)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");

            this.optimizerConfiguration = optimizerConfiguration;
            this.exceptionHelper = exceptionHelper;

            try
            {
                // Instantiate the ConnectionInfo object.
                AdapterDatabaseConnectionInfo = new ConnectionInfo(optimizerConfiguration.AdapterDatabaseConnectionString, optimizerConfiguration.AdapterDatabaseProviderType, Databases.AdapterDatabase, optimizerConfiguration.TimeoutSecondsForDatabaseTasks);
            }
            catch (Exception ex)
            {
                exceptionHelper.LogException(ex, NLogLogLevelName.Error, $"An exception was encountered while attempting to instantiate {nameof(ConnectionInfo)} objects.");
                throw;
            }

            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }
    }
}
