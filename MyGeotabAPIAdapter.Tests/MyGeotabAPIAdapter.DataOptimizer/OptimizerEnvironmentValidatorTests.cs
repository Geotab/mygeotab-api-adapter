using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DataOptimizer;
using System;
using System.Collections.Generic;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    public class ValidateOptimizerMachineNameTestData : TheoryData<IOptimizerEnvironment, List<DbOProcessorTracking>, DataOptimizerProcessor, bool>
    {
        public ValidateOptimizerMachineNameTestData()
        {
            // Setup list of DbOProcessorTracking objects to be validated against.
            var dbOProcessorTrackings = new List<DbOProcessorTracking> {
                new DbOProcessorTracking { ProcessorId = "DeviceProcessor", OptimizerVersion = null, OptimizerMachineName = null, EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOProcessorTracking { ProcessorId = "DriverChangeProcessor", OptimizerVersion = "1.5.0.1", OptimizerMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOProcessorTracking { ProcessorId = "FaultDataOptimizer", OptimizerVersion = "1.5.0.1", OptimizerMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue }
            };

            // *** VALID TESTS ***

            // The OptimizerMachineName of the record in the OProcessorTracking table associated with the specified DataOptimizerProcessor is null.
            var testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.DeviceProcessor, false);

            // The OptimizerMachineName of the OptimizerEnvironment being validated is THE SAME as the OptimizerMachineName logged in the associated record in the OProcessorTracking table in the optimizer database.
            testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.DriverChangeProcessor, false);


            // *** INVALID TESTS ***

            // No record exists in the OProcessorTracking table for the specified DataOptimizerProcessor.
            testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.UserProcessor, true);

            // The OptimizerMachineName of the OptimizerEnvironment being validated is DIFFERENT than the OptimizerMachineName logged in the associated record in the OProcessorTracking table in the optimizer database.
            testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "Skynet", "1.5.0.1");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.DriverChangeProcessor, true);
        }
    }

    public class ValidateOptimizerVersionTestData : TheoryData<IOptimizerEnvironment, List<DbOProcessorTracking>, DataOptimizerProcessor, bool>
    {
        public ValidateOptimizerVersionTestData()
        {
            // Setup list of DbOProcessorTracking objects to be validated against.
            var dbOProcessorTrackings = new List<DbOProcessorTracking> {
                new DbOProcessorTracking { ProcessorId = "DeviceProcessor", OptimizerVersion = null, OptimizerMachineName = null, EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOProcessorTracking { ProcessorId = "DriverChangeProcessor", OptimizerVersion = "1.5.0.1", OptimizerMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOProcessorTracking { ProcessorId = "FaultDataOptimizer", OptimizerVersion = "1.5.0.1", OptimizerMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue }
            };

            // *** VALID TESTS ***

            // The OptimizerVersion of the record in the OProcessorTracking table associated with the specified DataOptimizerProcessor is null.
            var testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.DeviceProcessor, false);

            // The OptimizerVersion of the OptimizerEnvironment being validated is THE SAME as the version logged in the associated record in the OProcessorTracking table in the optimizer database and there are no other records in the table with a higher OptimizerVersion value.
            testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.DriverChangeProcessor, false);

            // The OptimizerVersion of the OptimizerEnvironment being validated is HIGHER than the version logged in the associated record in the OProcessorTracking table in the optimizer database and there are no other records in the table with a higher OptimizerVersion value.
            testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "HAL 9000", "1.5.0.2");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.DriverChangeProcessor, false);


            // *** INVALID TESTS ***

            // No record exists in the OProcessorTracking table for the specified DataOptimizerProcessor.
            testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.UserProcessor, true);

            // The OptimizerVersion of the OptimizerEnvironment being validated is LOWER than the version logged in the associated record in the OProcessorTracking table in the optimizer database.
            testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "HAL 9000", "1.5.0.0");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.DriverChangeProcessor, true);
        }
    }

    public class ValidateOptimizerVersionTestData2 : TheoryData<IOptimizerEnvironment, List<DbOProcessorTracking>, DataOptimizerProcessor, bool>
    {
        public ValidateOptimizerVersionTestData2()
        {
            // Setup list of DbOProcessorTracking objects to be validated against.
            var dbOProcessorTrackings = new List<DbOProcessorTracking> {
                new DbOProcessorTracking { ProcessorId = "DeviceProcessor", OptimizerVersion = "1.5.0.1", OptimizerMachineName = null, EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOProcessorTracking { ProcessorId = "DriverChangeProcessor", OptimizerVersion = "1.5.0.1", OptimizerMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOProcessorTracking { ProcessorId = "FaultDataOptimizer", OptimizerVersion = "1.5.0.1", OptimizerMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOProcessorTracking { ProcessorId = "StatusDataOptimizer", OptimizerVersion = "1.5.0.2", OptimizerMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, AdapterDbLastId = null, AdapterDbLastGeotabId = null, AdapterDbLastRecordCreationTimeUtc = null, RecordLastChangedUtc = DateTime.MinValue }
            };

            // *** INVALID TESTS ***

            // The OptimizerVersion of the OptimizerEnvironment being validated is THE SAME as the version logged in the associated record in the OProcessorTracking table in the optimizer database, BUT one or more records in the table have a higher OptimizerVersion value.
            var testOptimizerEnvironment = new TestOptimizerEnvironment("OptimizerAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testOptimizerEnvironment, dbOProcessorTrackings, DataOptimizerProcessor.DriverChangeProcessor, true);
        }
    }

    public class OptimizerEnvironmentValidatorTests
    {
        [Theory]
        [ClassData(typeof(ValidateOptimizerMachineNameTestData))]
        public void ValidateOptimizerMachineNameTests(IOptimizerEnvironment optimizerEnvironment, List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor, bool shouldThrowException)
        {
            var optimizerEnvironmentValidator = new OptimizerEnvironmentValidator();
            var exception = Record.Exception(() => optimizerEnvironmentValidator.ValidateOptimizerMachineName(optimizerEnvironment, dbOProcessorTrackings, dataOptimizerProcessor));
            if (shouldThrowException == true)
            {
                Assert.NotNull(exception);
            }
            else
            {
                Assert.Null(exception);
            }
        }

        [Theory]
        [ClassData(typeof(ValidateOptimizerVersionTestData))]
        public void ValidateOptimizerVersionTests(IOptimizerEnvironment optimizerEnvironment, List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor, bool shouldThrowException)
        {
            var optimizerEnvironmentValidator = new OptimizerEnvironmentValidator();
            var exception = Record.Exception(() => optimizerEnvironmentValidator.ValidateOptimizerVersion(optimizerEnvironment, dbOProcessorTrackings, dataOptimizerProcessor));
            if (shouldThrowException == true)
            {
                Assert.NotNull(exception);
            }
            else
            {
                Assert.Null(exception);
            }
        }

        [Theory]
        [ClassData(typeof(ValidateOptimizerVersionTestData2))]
        public void ValidateOptimizerVersionTests2(IOptimizerEnvironment optimizerEnvironment, List<DbOProcessorTracking> dbOProcessorTrackings, DataOptimizerProcessor dataOptimizerProcessor, bool shouldThrowException)
        {
            var optimizerEnvironmentValidator = new OptimizerEnvironmentValidator();
            var exception = Record.Exception(() => optimizerEnvironmentValidator.ValidateOptimizerVersion(optimizerEnvironment, dbOProcessorTrackings, dataOptimizerProcessor));
            if (shouldThrowException == true)
            {
                Assert.NotNull(exception);
            }
            else
            {
                Assert.Null(exception);
            }
        }
    }
}
