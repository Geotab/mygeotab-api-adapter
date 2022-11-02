using MyGeotabAPIAdapter.Database.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    public class ValidateAdapterMachineNameTestData : TheoryData<IAdapterEnvironment, List<DbOServiceTracking>, AdapterService, bool>
    {
        public ValidateAdapterMachineNameTestData()
        {
            // Setup list of DbOServiceTracking objects to be validated against.
            var dbOServiceTrackings = new List<DbOServiceTracking> {
                new DbOServiceTracking { ServiceId = "DeviceProcessor", AdapterVersion = null, AdapterMachineName = null, EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOServiceTracking { ServiceId = "DriverChangeProcessor", AdapterVersion = "1.5.0.1", AdapterMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOServiceTracking { ServiceId = "FaultDataProcessor", AdapterVersion = "1.5.0.1", AdapterMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue }
            };

            // *** VALID TESTS ***

            // The AdapterMachineName of the record in the OServiceTracking table associated with the specified AdapterService is null.
            var testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.DeviceProcessor, false);

            // The AdapterMachineName of the AdapterEnvironment being validated is THE SAME as the AdapterMachineName logged in the associated record in the OServiceTracking table in the optimizer database.
            testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.DriverChangeProcessor, false);


            // *** INVALID TESTS ***

            // No record exists in the OServiceTracking table for the specified AdapterService.
            testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.UserProcessor, true);

            // The AdapterMachineName of the AdapterEnvironment being validated is DIFFERENT than the AdapterMachineName logged in the associated record in the OServiceTracking table in the optimizer database.
            testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "Skynet", "1.5.0.1");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.DriverChangeProcessor, true);
        }
    }

    public class ValidateAdapterVersionTestData : TheoryData<IAdapterEnvironment, List<DbOServiceTracking>, AdapterService, bool>
    {
        public ValidateAdapterVersionTestData()
        {
            // Setup list of DbOServiceTracking objects to be validated against.
            var dbOServiceTrackings = new List<DbOServiceTracking> {
                new DbOServiceTracking { ServiceId = "DeviceProcessor", AdapterVersion = null, AdapterMachineName = null, EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOServiceTracking { ServiceId = "DriverChangeProcessor", AdapterVersion = "1.5.0.1", AdapterMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOServiceTracking { ServiceId = "FaultDataProcessor", AdapterVersion = "1.5.0.1", AdapterMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue }
            };

            // *** VALID TESTS ***

            // The AdapterVersion of the record in the OServiceTracking table associated with the specified AdapterService is null.
            var testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.DeviceProcessor, false);

            // The AdapterVersion of the AdapterEnvironment being validated is THE SAME as the version logged in the associated record in the OServiceTracking table in the optimizer database and there are no other records in the table with a higher AdapterVersion value.
            testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.DriverChangeProcessor, false);

            // The AdapterVersion of the AdapterEnvironment being validated is HIGHER than the version logged in the associated record in the OServiceTracking table in the optimizer database and there are no other records in the table with a higher AdapterVersion value.
            testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "HAL 9000", "1.5.0.2");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.DriverChangeProcessor, false);


            // *** INVALID TESTS ***

            // No record exists in the OServiceTracking table for the specified AdapterService.
            testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.UserProcessor, true);

            // The AdapterVersion of the AdapterEnvironment being validated is LOWER than the version logged in the associated record in the OServiceTracking table in the optimizer database.
            testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "HAL 9000", "1.5.0.0");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.DriverChangeProcessor, true);
        }
    }

    public class ValidateAdapterVersionTestData2 : TheoryData<IAdapterEnvironment, List<DbOServiceTracking>, AdapterService, bool>
    {
        public ValidateAdapterVersionTestData2()
        {
            // Setup list of DbOServiceTracking objects to be validated against.
            var dbOServiceTrackings = new List<DbOServiceTracking> {
                new DbOServiceTracking { ServiceId = "DeviceProcessor", AdapterVersion = "1.5.0.1", AdapterMachineName = null, EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOServiceTracking { ServiceId = "DriverChangeProcessor", AdapterVersion = "1.5.0.1", AdapterMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOServiceTracking { ServiceId = "FaultDataProcessor", AdapterVersion = "1.5.0.1", AdapterMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue },
                new DbOServiceTracking { ServiceId = "StatusDataProcessor", AdapterVersion = "1.5.0.2", AdapterMachineName = "HAL 9000", EntitiesLastProcessedUtc = DateTime.MinValue, LastProcessedFeedVersion = null, RecordLastChangedUtc = DateTime.MinValue }
            };

            // *** INVALID TESTS ***

            // The AdapterVersion of the AdapterEnvironment being validated is THE SAME as the version logged in the associated record in the OServiceTracking table in the optimizer database, BUT one or more records in the table have a higher AdapterVersion value.
            var testAdapterEnvironment = new TestAdapterEnvironment("AdapterAssemblyName", "HAL 9000", "1.5.0.1");
            Add(testAdapterEnvironment, dbOServiceTrackings, AdapterService.DriverChangeProcessor, true);
        }
    }

    public class AdapterEnvironmentValidatorTests
    {
        [Theory]
        [ClassData(typeof(ValidateAdapterMachineNameTestData))]
        public void ValidateAdapterMachineNameTests(IAdapterEnvironment optimizerEnvironment, List<DbOServiceTracking> dbOServiceTrackings, AdapterService adapterService, bool shouldThrowException)
        {
            var optimizerEnvironmentValidator = new AdapterEnvironmentValidator();
            var exception = Record.Exception(() => optimizerEnvironmentValidator.ValidateAdapterMachineName(optimizerEnvironment, dbOServiceTrackings, adapterService));
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
        [ClassData(typeof(ValidateAdapterVersionTestData))]
        public void ValidateAdapterVersionTests(IAdapterEnvironment optimizerEnvironment, List<DbOServiceTracking> dbOServiceTrackings, AdapterService adapterService, bool shouldThrowException)
        {
            var optimizerEnvironmentValidator = new AdapterEnvironmentValidator();
            var exception = Record.Exception(() => optimizerEnvironmentValidator.ValidateAdapterVersion(optimizerEnvironment, dbOServiceTrackings, adapterService));
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
        [ClassData(typeof(ValidateAdapterVersionTestData2))]
        public void ValidateAdapterVersionTests2(IAdapterEnvironment optimizerEnvironment, List<DbOServiceTracking> dbOServiceTrackings, AdapterService adapterService, bool shouldThrowException)
        {
            var optimizerEnvironmentValidator = new AdapterEnvironmentValidator();
            var exception = Record.Exception(() => optimizerEnvironmentValidator.ValidateAdapterVersion(optimizerEnvironment, dbOServiceTrackings, adapterService));
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
