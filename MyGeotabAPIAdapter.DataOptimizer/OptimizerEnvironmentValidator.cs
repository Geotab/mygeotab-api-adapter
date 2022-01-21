using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyGeotabAPIAdapter.DataOptimizer
{
    public class OptimizerEnvironmentValidator : IOptimizerEnvironmentValidator
    {
        /// <inheritdoc/>
        public void ValidateOptimizerMachineName(IOptimizerEnvironment optimizerEnvironment, List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor)
        {
            var subjectDbOProcessorTracking = dbOProcessorTrackings.Where(dbOProcessorTracking => dbOProcessorTracking.ProcessorId == dataOptimizerProcessor.ToString()).FirstOrDefault();

            if (subjectDbOProcessorTracking == null)
            {
                throw new ArgumentException($"The '{dataOptimizerProcessor}' DataOptimizerProcessor was not found in the '{nameof(dbOProcessorTrackings)}' list. Unable to {nameof(ValidateOptimizerVersion)}.");
            }

            if (subjectDbOProcessorTracking.OptimizerMachineName != null)
            {
                var dbOProcessorTrackingMachineName = subjectDbOProcessorTracking.OptimizerMachineName;

                if (optimizerEnvironment.OptimizerMachineName != dbOProcessorTrackingMachineName)
                {
                    throw new Exception($"The '{nameof(optimizerEnvironment.OptimizerMachineName)}' of '{optimizerEnvironment.OptimizerMachineName}' is different than that of '{dbOProcessorTrackingMachineName}' logged in the optimizer database for the '{dataOptimizerProcessor}' DataOptimizerProcessor. This could be due to another instance of the DataOptimizer running on a different machine. Only one instance of the '{dataOptimizerProcessor}' DataOptimizerProcessor can be run against a single instance of the optimizer database at a time.");
                }
            }
        }

        /// <inheritdoc/>
        public void ValidateOptimizerVersion(IOptimizerEnvironment optimizerEnvironment, List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor)
        {
            const int VersionMatchValue = 0;
            var subjectDbOProcessorTracking = dbOProcessorTrackings.Where(dbOProcessorTracking => dbOProcessorTracking.ProcessorId == dataOptimizerProcessor.ToString()).FirstOrDefault();

            if (subjectDbOProcessorTracking == null)
            {
                throw new ArgumentException($"The '{dataOptimizerProcessor}' DataOptimizerProcessor was not found in the '{nameof(dbOProcessorTrackings)}' list. Unable to {nameof(ValidateOptimizerVersion)}.");
            }

            if (subjectDbOProcessorTracking.OptimizerVersion != null)
            {
                var dbOProcessorTrackingVersion = Version.Parse(subjectDbOProcessorTracking.OptimizerVersion);
                var optimizerVersionCompareResult = optimizerEnvironment.OptimizerVersion.CompareTo(dbOProcessorTrackingVersion);
                if (optimizerVersionCompareResult < VersionMatchValue)
                {
                    throw new Exception($"The '{nameof(optimizerEnvironment.OptimizerVersion)}' of '{optimizerEnvironment.OptimizerVersion}' is lower than that of '{dbOProcessorTrackingVersion}' logged in the optimizer database for the '{dataOptimizerProcessor}' DataOptimizerProcessor. This could be due to a newer instance of the DataOptimizer running on a different machine, or a possible downgrade on the current machine. Either way, all instances of the DataOptimizer operating against the subject database must be of the same version.");
                }
                // The OptimizerVersion of the OptimizerEnvironment being validated is THE SAME OR HIGHER than the version logged in the associated record in the OProcessorTracking table in the optimizer database. If higher, this is likely due to an upgrade of the DataOptimizer on the current machine. In either case, so far, so good. 

                // Make sure there is not a higher OptimizerVersion for ANY other DataOptimizerProcessor record listed in the OProcessorTracking table in the optimizer database. This could occur if the DataOptimizer has been upgraded to a newer version on another machine.
                foreach (var dbOProcessorTracking in dbOProcessorTrackings)
                {
                    if (dbOProcessorTracking.OptimizerVersion != null)
                    {
                        dbOProcessorTrackingVersion = Version.Parse(dbOProcessorTracking.OptimizerVersion);
                        optimizerVersionCompareResult = optimizerEnvironment.OptimizerVersion.CompareTo(dbOProcessorTrackingVersion);
                        if (optimizerVersionCompareResult < VersionMatchValue)
                        {
                            throw new Exception($"The '{nameof(optimizerEnvironment.OptimizerVersion)}' of '{optimizerEnvironment.OptimizerVersion}' is lower than that of '{dbOProcessorTrackingVersion}' logged in the optimizer database for the '{dbOProcessorTracking.ProcessorId}' DataOptimizerProcessor. This could be due to a newer instance of the DataOptimizer running on a different machine, or a possible downgrade on the current machine. Either way, all instances of the DataOptimizer operating against the subject database must be of the same version.");
                        }
                    }
                }
            }
        }
    }
}
