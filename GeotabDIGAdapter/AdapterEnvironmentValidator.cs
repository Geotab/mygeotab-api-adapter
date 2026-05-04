using MyGeotabAPIAdapter.Database.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeotabDIGAdapter
{
    /// <summary>
    /// A class that includes validation logic to help ensure that only one instance of a given <see cref="DIGAdapterService"/> is running against the same adapter database in a distributed deployment scenario in which copies of the GeotabDIGAdapter are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput) AND that the same version of the GeotabDIGAdapter is used on all machines involved. 
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDbOServiceTracking"/> implementation to be used.</typeparam>
    public class AdapterEnvironmentValidator<T> : IAdapterEnvironmentValidator<T> where T : IDbOServiceTracking
    {
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterEnvironmentValidator{T}"/> class.
        /// </summary>
        public AdapterEnvironmentValidator()
        {
            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(AdapterEnvironmentValidator<T>)} [Id: {Id}] created.");
        }

        /// <inheritdoc/>
        public void ValidateAdapterMachineName(IAdapterEnvironment<T> adapterEnvironment, List<T> dbOServiceTrackings, DIGAdapterService adapterService)
        {
            var subjectDbOServiceTracking = dbOServiceTrackings.Where(dbOServiceTracking => dbOServiceTracking.ServiceId == adapterService.ToString()).FirstOrDefault();

            if (subjectDbOServiceTracking == null)
            {
                throw new ArgumentException($"The '{adapterService}' DIGAdapterService was not found in the '{nameof(dbOServiceTrackings)}' list. Unable to {nameof(ValidateAdapterMachineName)}.");
            }

            if (subjectDbOServiceTracking.AdapterMachineName != null)
            {
                var dbOServiceTrackingMachineName = subjectDbOServiceTracking.AdapterMachineName;

                if (adapterEnvironment.AdapterMachineName != dbOServiceTrackingMachineName)
                {
                    throw new Exception($"The '{nameof(adapterEnvironment.AdapterMachineName)}' of '{adapterEnvironment.AdapterMachineName}' is different than that of '{dbOServiceTrackingMachineName}' logged in the adapter database for the '{adapterService}' DIGAdapterService. This could be due to another instance of the GeotabDIGAdapter running on a different machine. Only one instance of the '{adapterService}' DIGAdapterService can be run against a single instance of the adapter database at a time.");
                }
            }
        }

        /// <inheritdoc/>
        public void ValidateAdapterVersion(IAdapterEnvironment<T> adapterEnvironment, List<T> dbOServiceTrackings, DIGAdapterService adapterService)
        {
            const int VersionMatchValue = 0;
            var subjectDbOServiceTracking = dbOServiceTrackings.Where(dbOServiceTracking => dbOServiceTracking.ServiceId == adapterService.ToString()).FirstOrDefault();

            if (subjectDbOServiceTracking == null)
            {
                throw new ArgumentException($"The '{adapterService}' DIGAdapterService was not found in the '{nameof(dbOServiceTrackings)}' list. Unable to {nameof(ValidateAdapterVersion)}.");
            }

            if (subjectDbOServiceTracking.AdapterVersion != null)
            {
                var dbOServiceTrackingVersion = Version.Parse(subjectDbOServiceTracking.AdapterVersion);
                var adapterVersionCompareResult = adapterEnvironment.AdapterVersion.CompareTo(dbOServiceTrackingVersion);
                if (adapterVersionCompareResult < VersionMatchValue)
                {
                    throw new Exception($"The '{nameof(adapterEnvironment.AdapterVersion)}' of '{adapterEnvironment.AdapterVersion}' is lower than that of '{dbOServiceTrackingVersion}' logged in the adapter database for the '{adapterService}' DIGAdapterService. This could be due to a newer instance of the GeotabDIGAdapter running on a different machine, or a possible downgrade on the current machine. Either way, all instances of the GeotabDIGAdapter operating against the subject database must be of the same version.");
                }
                // The AdapterVersion of the AdapterEnvironment being validated is THE SAME OR HIGHER than the version logged in the associated record in the OServiceTracking table in the adapter database. If higher, this is likely due to an upgrade of the GeotabDIGAdapter on the current machine. In either case, so far, so good. 

                // Make sure there is not a higher AdapterVersion for ANY other DIGAdapterService record listed in the OServiceTracking table in the adapter database. This could occur if the GeotabDIGAdapter has been upgraded to a newer version on another machine.
                foreach (var dbOServiceTracking in dbOServiceTrackings)
                {
                    if (dbOServiceTracking.AdapterVersion != null)
                    {
                        dbOServiceTrackingVersion = Version.Parse(dbOServiceTracking.AdapterVersion);
                        adapterVersionCompareResult = adapterEnvironment.AdapterVersion.CompareTo(dbOServiceTrackingVersion);
                        if (adapterVersionCompareResult < VersionMatchValue)
                        {
                            throw new Exception($"The '{nameof(adapterEnvironment.AdapterVersion)}' of '{adapterEnvironment.AdapterVersion}' is lower than that of '{dbOServiceTrackingVersion}' logged in the adapter database for the '{dbOServiceTracking.ServiceId}' DIGAdapterService. This could be due to a newer instance of the GeotabDIGAdapter running on a different machine, or a possible downgrade on the current machine. Either way, all instances of the GeotabDIGAdapter operating against the subject database must be of the same version.");
                        }
                    }
                }
            }
        }
    }
}