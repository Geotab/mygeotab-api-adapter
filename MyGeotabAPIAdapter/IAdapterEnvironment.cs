using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a class for obtaining environment information related to the <see cref="MyGeotabAPIAdapter"/>. 
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDbOServiceTracking"/> implementation to be used.</typeparam>
    public interface IAdapterEnvironment<T> where T : IDbOServiceTracking
    {
        /// <summary>
        /// The <see cref="System.Reflection.AssemblyName"/> of the <see cref="MyGeotabAPIAdapter"/> assembly.
        /// </summary>
        string AdapterAssemblyName { get; }

        /// <summary>
        /// The <see cref="Environment.MachineName"/> of the computer on which the <see cref="MyGeotabAPIAdapter"/> assembly is being executed.
        /// </summary>
        string AdapterMachineName { get; }

        /// <summary>
        /// The <see cref="Version"/> of the <see cref="MyGeotabAPIAdapter"/> assembly.
        /// </summary>
        Version AdapterVersion { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// <para>
        /// Validates both the <see cref="AdapterVersion"/> and the <see cref="IAdapterEnvironment.AdapterMachineName"/> of this <see cref="IAdapterEnvironment"/> as decribed below.
        /// </para>
        /// <para>
        /// Validates the <see cref="AdapterVersion"/> of this <see cref="IAdapterEnvironment"/> against the <see cref="IDbOServiceTracking"/> in the <paramref name="dbOServiceTrackings"/> identified by <paramref name="adapterService"/>. Intended to help ensure that the same version of the <see cref="MyGeotabAPIAdapter"/> is used on all machines in a distributed deployment scenario in which copies of the <see cref="MyGeotabAPIAdapter"/> are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput).
        /// </para>
        /// <para>
        /// Validates the <see cref="AdapterMachineName"/> of this <see cref="IAdapterEnvironment"/> against the <see cref="IDbOServiceTracking"/> in the <paramref name="dbOServiceTrackings"/> identified by <paramref name="adapterService"/>. Intended to help ensure that only one instance of the subject <see cref="AdapterService"/> is running against the same adapter database in a distributed deployment scenario in which copies of the <see cref="MyGeotabAPIAdapter"/> are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput). Running multiple instances of a service against the same database will result in data duplication amongst other possible issues.
        /// </para>
        /// </summary>
        /// <param name="dbOServiceTrackings">A list of <see cref="IDbOServiceTracking"/> objects to validate the <see cref="AdapterEnvironment"/> against.</param>
        /// <param name="adapterService">The specific <see cref="AdapterService"/> in the <paramref name="dbOServiceTrackings"/> to be validated against.</param>
        /// <param name="disableMachineNameValidation">Indicates whether machine name validation should be disabled. NOTE: This should always be set to <c>false</c> except in scenarios where machine names in hosted environments are not static. WARNING: Improper use of this setting could result in application instability and data integrity issues.</param>
        void ValidateAdapterEnvironment(List<T> dbOServiceTrackings, AdapterService adapterService, bool disableMachineNameValidation);
    }
}
