using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;

namespace GeotabDIGAdapter
{
    /// <summary>
    /// Interface for a class for obtaining environment information related to the GeotabDIGAdapter. 
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDbOServiceTracking"/> implementation to be used.</typeparam>
    public interface IAdapterEnvironment<T> where T : IDbOServiceTracking
    {
        /// <summary>
        /// The <see cref="System.Reflection.AssemblyName"/> of the GeotabDIGAdapter assembly.
        /// </summary>
        string AdapterAssemblyName { get; }

        /// <summary>
        /// The <see cref="Environment.MachineName"/> of the computer on which the GeotabDIGAdapter assembly is being executed.
        /// </summary>
        string AdapterMachineName { get; }

        /// <summary>
        /// The <see cref="Version"/> of the GeotabDIGAdapter assembly.
        /// </summary>
        Version AdapterVersion { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// <para>
        /// Validates both the <see cref="AdapterVersion"/> and the <see cref="AdapterMachineName"/> of this <see cref="IAdapterEnvironment{T}"/> as described below.
        /// </para>
        /// <para>
        /// Validates the <see cref="AdapterVersion"/> of this <see cref="IAdapterEnvironment{T}"/> against the <see cref="IDbOServiceTracking"/> in the <paramref name="dbOServiceTrackings"/> identified by <paramref name="adapterService"/>. Intended to help ensure that the same version of the GeotabDIGAdapter is used on all machines in a distributed deployment scenario in which copies of the GeotabDIGAdapter are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput).
        /// </para>
        /// <para>
        /// Validates the <see cref="AdapterMachineName"/> of this <see cref="IAdapterEnvironment{T}"/> against the <see cref="IDbOServiceTracking"/> in the <paramref name="dbOServiceTrackings"/> identified by <paramref name="adapterService"/>. Intended to help ensure that only one instance of the subject <see cref="DIGAdapterService"/> is running against the same adapter database in a distributed deployment scenario in which copies of the GeotabDIGAdapter are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput). Running multiple instances of a service against the same database will result in data duplication amongst other possible issues.
        /// </para>
        /// </summary>
        /// <param name="dbOServiceTrackings">A list of <see cref="IDbOServiceTracking"/> objects to validate the <see cref="IAdapterEnvironment{T}"/> against.</param>
        /// <param name="adapterService">The specific <see cref="DIGAdapterService"/> in the <paramref name="dbOServiceTrackings"/> to be validated against.</param>
        /// <param name="disableMachineNameValidation">Indicates whether machine name validation should be disabled. NOTE: This should always be set to <c>false</c> except in scenarios where machine names in hosted environments are not static. WARNING: Improper use of this setting could result in application instability and data integrity issues.</param>
        void ValidateAdapterEnvironment(List<T> dbOServiceTrackings, DIGAdapterService adapterService, bool disableMachineNameValidation);
    }
}