using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// A test class for simulating environment information related to the <see cref="MyGeotabAPIAdapter"/>. 
    /// </summary>
    class TestAdapterEnvironment : IAdapterEnvironment
    {
        /// <inheritdoc/>
        public string AdapterAssemblyName { get; }

        /// <inheritdoc/>
        public string AdapterMachineName { get; }

        /// <inheritdoc/>
        public Version AdapterVersion { get; }

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAdapterEnvironment"/> class.
        /// </summary>
        /// <param name="adapterAssemblyName">The value to be used for <see cref="AdapterAssemblyName"/>.</param>
        /// <param name="adapterMachineName">The value to be used for <see cref="AdapterMachineName"/>.</param>
        /// <param name="adapterVersion">The value to be used for <see cref="AdapterVersion"/>.</param>
        public TestAdapterEnvironment(string adapterAssemblyName, string adapterMachineName, string adapterVersion)
        {
            AdapterAssemblyName = adapterAssemblyName;
            AdapterMachineName = adapterMachineName;
            AdapterVersion = Version.Parse(adapterVersion);
        }

        /// <summary>
        /// This method doesn't do anything in this <see cref="TestAdapterEnvironment"/> testing class and is only here due to interface requirement.
        /// </summary>
        public void ValidateAdapterEnvironment(List<DbOServiceTracking> dbOServiceTrackings, AdapterService adapterService, bool disableMachineNameValidation)
        {
            throw new NotImplementedException();
        }
    }
}
