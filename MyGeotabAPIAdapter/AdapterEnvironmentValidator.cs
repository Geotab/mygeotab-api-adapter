using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// A class that includes validation logic to help ensure that only one instance of a given <see cref="AdapterService"/> is running against the same adapter database in a distributed deployment scenario in which copies of the <see cref="MyGeotabAPIAdapter"/> are installed on multiple machines with different services running on each (in order to distribute load and maximize throughput) AND that the same version of the <see cref="MyGeotabAPIAdapter"/> is used on all machines involved. 
    /// <typeparam name="T">The type of <see cref="IDbOServiceTracking"/> implementation to be used.</typeparam>
    /// </summary>
    public class AdapterEnvironmentValidator<T> : IAdapterEnvironmentValidator<T> where T : IDbOServiceTracking
    {
        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public void ValidateAdapterMachineName(IAdapterEnvironment<T> adapterEnvironment, List<T> dbOServiceTrackings, AdapterService adapterService)
        {
            var subjectDbOServiceTracking = dbOServiceTrackings.Where(dbOServiceTracking => dbOServiceTracking.ServiceId == adapterService.ToString()).FirstOrDefault();

            if (subjectDbOServiceTracking == null)
            {
                throw new ArgumentException($"The '{adapterService}' AdapterService was not found in the '{nameof(dbOServiceTrackings)}' list. Unable to {nameof(ValidateAdapterMachineName)}.");
            }

            if (subjectDbOServiceTracking.AdapterMachineName != null)
            {
                var dbOServiceTrackingMachineName = subjectDbOServiceTracking.AdapterMachineName;

                if (adapterEnvironment.AdapterMachineName != dbOServiceTrackingMachineName)
                {
                    throw new Exception($"The '{nameof(adapterEnvironment.AdapterMachineName)}' of '{adapterEnvironment.AdapterMachineName}' is different than that of '{dbOServiceTrackingMachineName}' logged in the adapter database for the '{adapterService}' AdapterService. This could be due to another instance of the MyGeotabAPIAdapter running on a different machine. Only one instance of the '{adapterService}' AdapterService can be run against a single instance of the adapter database at a time.");
                }
            }
        }

        /// <inheritdoc/>
        public void ValidateAdapterVersion(IAdapterEnvironment<T> adapterEnvironment, List<T> dbOServiceTrackings, AdapterService adapterService)
        {
            const int VersionMatchValue = 0;
            var subjectDbOServiceTracking = dbOServiceTrackings.Where(dbOServiceTracking => dbOServiceTracking.ServiceId == adapterService.ToString()).FirstOrDefault();

            if (subjectDbOServiceTracking == null)
            {
                throw new ArgumentException($"The '{adapterService}' AdapterService was not found in the '{nameof(dbOServiceTrackings)}' list. Unable to {nameof(ValidateAdapterVersion)}.");
            }

            if (subjectDbOServiceTracking.AdapterVersion != null)
            {
                var dbOServiceTrackingVersion = Version.Parse(subjectDbOServiceTracking.AdapterVersion);
                var adapterVersionCompareResult = adapterEnvironment.AdapterVersion.CompareTo(dbOServiceTrackingVersion);
                if (adapterVersionCompareResult < VersionMatchValue)
                {
                    throw new Exception($"The '{nameof(adapterEnvironment.AdapterVersion)}' of '{adapterEnvironment.AdapterVersion}' is lower than that of '{dbOServiceTrackingVersion}' logged in the adapter database for the '{adapterService}' AdapterService. This could be due to a newer instance of the MyGeotabAPIAdapter running on a different machine, or a possible downgrade on the current machine. Either way, all instances of the MyGeotabAPIAdapter operating against the subject database must be of the same version.");
                }
                // The AdapterVersion of the AdapterEnvironment being validated is THE SAME OR HIGHER than the version logged in the associated record in the OServiceTracking table in the adapter database. If higher, this is likely due to an upgrade of the MyGeotabAPIAdapter on the current machine. In either case, so far, so good. 

                // Make sure there is not a higher AdapterVersion for ANY other AdapterService record listed in the OServiceTracking table in the adapter database. This could occur if the MyGeotabAPIAdapter has been upgraded to a newer version on another machine.
                foreach (var dbOServiceTracking in dbOServiceTrackings)
                {
                    if (dbOServiceTracking.AdapterVersion != null)
                    {
                        dbOServiceTrackingVersion = Version.Parse(dbOServiceTracking.AdapterVersion);
                        adapterVersionCompareResult = adapterEnvironment.AdapterVersion.CompareTo(dbOServiceTrackingVersion);
                        if (adapterVersionCompareResult < VersionMatchValue)
                        {
                            throw new Exception($"The '{nameof(adapterEnvironment.AdapterVersion)}' of '{adapterEnvironment.AdapterVersion}' is lower than that of '{dbOServiceTrackingVersion}' logged in the adapter database for the '{dbOServiceTracking.ServiceId}' AdapterService. This could be due to a newer instance of the MyGeotabAPIAdapter running on a different machine, or a possible downgrade on the current machine. Either way, all instances of the MyGeotabAPIAdapter operating against the subject database must be of the same version.");
                        }
                    }
                }
            }
        }
    }
}
