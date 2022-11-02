using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class for obtaining environment information related to the <see cref="MyGeotabAPIAdapter"/>. 
    /// </summary>
    public class AdapterEnvironment : IAdapterEnvironment
    {
        readonly IAdapterEnvironmentValidator adapterEnvironmentValidator;
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public string AdapterAssemblyName { get; }

        /// <inheritdoc/>
        public string AdapterMachineName { get; }

        /// <inheritdoc/>
        public Version AdapterVersion { get; }

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterEnvironment"/> class.
        /// </summary>
        public AdapterEnvironment(IAdapterEnvironmentValidator adapterEnvironmentValidator)
        {
            MethodBase methodBase = MethodBase.GetCurrentMethod();
            logger.Trace($"Begin {methodBase.ReflectedType.Name}.{methodBase.Name}");
                        
            this.adapterEnvironmentValidator = adapterEnvironmentValidator;
            AdapterAssemblyName = GetType().Assembly.GetName().Name;
            AdapterMachineName = Environment.MachineName;
            AdapterVersion = GetType().Assembly.GetName().Version;

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(AdapterEnvironment)} [Id: {Id}] created.");
            logger.Trace($"End {methodBase.ReflectedType.Name}.{methodBase.Name}");
        }

        /// <inheritdoc/>
        public void ValidateAdapterEnvironment(List<DbOServiceTracking> dbOServiceTrackings, AdapterService adapterService)
        {
            adapterEnvironmentValidator.ValidateAdapterVersion(this, dbOServiceTrackings, adapterService);
            adapterEnvironmentValidator.ValidateAdapterMachineName(this, dbOServiceTrackings,adapterService);
        }
    }
}
