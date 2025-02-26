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
    /// <typeparam name="T">The type of <see cref="IDbOServiceTracking"/> implementation to be used.</typeparam>
    public class AdapterEnvironment<T> : IAdapterEnvironment<T> where T : IDbOServiceTracking
    {
        readonly IAdapterEnvironmentValidator<T> adapterEnvironmentValidator;
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
        public AdapterEnvironment(IAdapterEnvironmentValidator<T> adapterEnvironmentValidator)
        {
            this.adapterEnvironmentValidator = adapterEnvironmentValidator;
            AdapterAssemblyName = GetType().Assembly.GetName().Name;
            AdapterMachineName = Environment.MachineName;
            AdapterVersion = GetType().Assembly.GetName().Version;

            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(AdapterEnvironment<T>)} [Id: {Id}] created.");
        }

        /// <inheritdoc/>
        public void ValidateAdapterEnvironment(List<T> dbOServiceTrackings, AdapterService adapterService, bool disableMachineNameValidation)
        {
            adapterEnvironmentValidator.ValidateAdapterVersion(this, dbOServiceTrackings, adapterService);
            if (disableMachineNameValidation == false)
            {
                adapterEnvironmentValidator.ValidateAdapterMachineName(this, dbOServiceTrackings, adapterService);
            }
        }
    }
}
