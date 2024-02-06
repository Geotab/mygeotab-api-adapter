using Microsoft.Extensions.Configuration;
using MyGeotabAPIAdapter.Configuration;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Caches;
using MyGeotabAPIAdapter.Database.DataAccess;
using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;
using MyGeotabAPIAdapter.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MyGeotabAPIAdapter.Tests
{
    /// <summary>
    /// A class containing methods that generate mock data representing data that would normally be contined in actual database tables. To be used in conjunction with <see cref="MockBaseRepository2{T}"/> classes to test <see cref="IGenericDbObjectCache{T}"/> implementations without actually needing to interact with an actual database. To be used as "seed" data when creating <see cref="MockBaseRepository2{T}"/> instances.
    /// </summary>
    public static class GenericDbObjectCacheTestMockData
    {
        public static DateTime DateTime1
        {
            get => DateTime.ParseExact("2021-11-12 05:30:08.688326", "yyyy-MM-dd hh:mm:ss.ffffff", CultureInfo.InvariantCulture);
        }

        public static DateTime DateTime2
        {
            get => DateTime.ParseExact("2021-11-13 05:30:08.688326", "yyyy-MM-dd hh:mm:ss.ffffff", CultureInfo.InvariantCulture);
        }

        public static DateTime DateTime3
        {
            get => DateTime.ParseExact("2021-11-14 05:30:08.688326", "yyyy-MM-dd hh:mm:ss.ffffff", CultureInfo.InvariantCulture);
        }

        public static DateTime DateTime4
        {
            get => DateTime.ParseExact("2021-11-14 05:30:10.688326", "yyyy-MM-dd hh:mm:ss.ffffff", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Initializes the mock list of DbDevices.
        /// </summary>
        public static List<DbDevice> GetMockDbDevices()
        {
            return new List<DbDevice>
            {
                new DbDevice
                {
                    ActiveFrom = DateTime.MinValue,
                    ActiveTo = DateTime.MaxValue,
                    Comment = "",
                    DeviceType = "GO9",
                    EntityStatus = 1,
                    GeotabId = "b1",
                    id = 1,
                    LicensePlate = "",
                    LicenseState = "",
                    Name = "Unit 1",
                    ProductId = 120,
                    RecordLastChangedUtc = DateTime1,
                    SerialNumber = "000-000-0000",
                    VIN = ""
                },
                new DbDevice
                {
                    ActiveFrom = DateTime.MinValue,
                    ActiveTo = DateTime.MaxValue,
                    Comment = "",
                    DeviceType = "GO9",
                    EntityStatus = 1,
                    GeotabId = "b2",
                    id = 2,
                    LicensePlate = "",
                    LicenseState = "",
                    Name = "Unit 2",
                    ProductId = 120,
                    RecordLastChangedUtc = DateTime3,
                    SerialNumber = "000-000-0000",
                    VIN = ""
                },
                new DbDevice
                {
                    ActiveFrom = DateTime.MinValue,
                    ActiveTo = DateTime.MaxValue,
                    Comment = "",
                    DeviceType = "GO9",
                    EntityStatus = 1,
                    GeotabId = "b3",
                    id = 3,
                    LicensePlate = "",
                    LicenseState = "",
                    Name = "Unit 3",
                    ProductId = 120,
                    RecordLastChangedUtc = DateTime3,
                    SerialNumber = "000-000-0000",
                    VIN = ""
                },
                new DbDevice
                {
                    ActiveFrom = DateTime.MinValue,
                    ActiveTo = DateTime.MaxValue,
                    Comment = "",
                    DeviceType = "GO9",
                    EntityStatus = 1,
                    GeotabId = "b4",
                    id = 4,
                    LicensePlate = "",
                    LicenseState = "",
                    Name = "Unit 4",
                    ProductId = 120,
                    RecordLastChangedUtc = DateTime4,
                    SerialNumber = "000-000-0000",
                    VIN = ""
                }
            };
        }
    }

    /// <summary>
    /// TheoryData for <see cref="IGenericDbObjectCache{T}.GetObjectAsync(long)"/> (<see cref="IIdCacheableDbEntity.id"/>) method.
    /// </summary>
    public class GenericDbObjectCacheTest_GetObjectAsyncById_TestData : TheoryData<MockBaseRepository2<DbDevice>, long , bool, long?>
    {
        public GenericDbObjectCacheTest_GetObjectAsyncById_TestData()
        {
            var existingDbDevices = GenericDbObjectCacheTestMockData.GetMockDbDevices();
            var mockDbDeviceRepository2 = new MockBaseRepository2<DbDevice>();
            Task insertAsyncTask = Task.Run(() => mockDbDeviceRepository2.InsertAsync(existingDbDevices, new CancellationTokenSource()));
            insertAsyncTask.Wait();

            Add(mockDbDeviceRepository2, 1, true, 1);
            Add(mockDbDeviceRepository2, 100, false, null);
            Add(mockDbDeviceRepository2, -100, false, null);
        }
    }

    /// <summary>
    /// TheoryData for <see cref="IGenericDbObjectCache{T}.GetObjectAsync(string)"/> (<see cref="IIdCacheableDbEntity.GeotabId"/>) method.
    /// </summary>
    public class GenericDbObjectCacheTest_GetObjectAsyncByGeotabId_TestData : TheoryData<MockBaseRepository2<DbDevice>, string, bool, long?>
    {
        public GenericDbObjectCacheTest_GetObjectAsyncByGeotabId_TestData()
        {
            var existingDbDevices = GenericDbObjectCacheTestMockData.GetMockDbDevices();
            var mockDbDeviceRepository2 = new MockBaseRepository2<DbDevice>();
            Task insertAsyncTask = Task.Run(() => mockDbDeviceRepository2.InsertAsync(existingDbDevices, new CancellationTokenSource()));
            insertAsyncTask.Wait();

            Add(mockDbDeviceRepository2, "b2", true, 2);
            Add(mockDbDeviceRepository2, "xx", false, null);
        }
    }

    /// <summary>
    /// TheoryData for <see cref="IGenericDbObjectCache{T}.GetObjectIdAsync(string)"/> (<see cref="IIdCacheableDbEntity.GeotabId"/>) method.
    /// </summary>
    public class GenericDbObjectCacheTest_GetObjectIdAsyncByGeotabId_TestData : TheoryData<MockBaseRepository2<DbDevice>, string, long?>
    {
        public GenericDbObjectCacheTest_GetObjectIdAsyncByGeotabId_TestData()
        {
            var existingDbDevices = GenericDbObjectCacheTestMockData.GetMockDbDevices();
            var mockDbDeviceRepository2 = new MockBaseRepository2<DbDevice>();
            Task insertAsyncTask = Task.Run(() => mockDbDeviceRepository2.InsertAsync(existingDbDevices, new CancellationTokenSource()));
            insertAsyncTask.Wait();

            Add(mockDbDeviceRepository2, "b2", 2);
            Add(mockDbDeviceRepository2, "xx", null);
        }
    }

    /// <summary>
    /// TheoryData for <see cref="IGenericDbObjectCache{T}.GetObjectsAsync()"/> method.
    /// </summary>
    public class GenericDbObjectCacheTest_GetObjectsAsync_TestData : TheoryData<MockBaseRepository2<DbDevice>, bool, List<long>>
    {
        public GenericDbObjectCacheTest_GetObjectsAsync_TestData()
        {
            var existingDbDevices = GenericDbObjectCacheTestMockData.GetMockDbDevices();
            var mockDbDeviceRepository2 = new MockBaseRepository2<DbDevice>();
            Task insertAsyncTask = Task.Run(() => mockDbDeviceRepository2.InsertAsync(existingDbDevices, new CancellationTokenSource()));
            insertAsyncTask.Wait();

            Add(mockDbDeviceRepository2, true, new List<long> { 1, 2, 3, 4 });
        }
    }

    /// <summary>
    /// TheoryData for <see cref="IGenericDbObjectCache{T}.GetObjectsAsync(DateTime)"/> (<see cref="IIdCacheableDbEntity.LastUpsertedUtc"/>) method.
    /// </summary>
    public class GenericDbObjectCacheTest_GetObjectsAsyncByChangedDateTime_TestData : TheoryData<MockBaseRepository2<DbDevice>, DateTime, bool, List<long>>
    {
        public GenericDbObjectCacheTest_GetObjectsAsyncByChangedDateTime_TestData()
        {
            var existingDbDevices = GenericDbObjectCacheTestMockData.GetMockDbDevices();
            var mockDbDeviceRepository2 = new MockBaseRepository2<DbDevice>();
            Task insertAsyncTask = Task.Run(() => mockDbDeviceRepository2.InsertAsync(existingDbDevices, new CancellationTokenSource()));
            insertAsyncTask.Wait();

            Add(mockDbDeviceRepository2, DateTime.MinValue, true, new List<long> { 1, 2, 3, 4 });
            Add(mockDbDeviceRepository2, DateTime.MaxValue, false, new List<long>());
            Add(mockDbDeviceRepository2, GenericDbObjectCacheTestMockData.DateTime1, true, new List<long> { 1, 2, 3, 4 });
            Add(mockDbDeviceRepository2, GenericDbObjectCacheTestMockData.DateTime2, true, new List<long> { 2, 3, 4 });
            Add(mockDbDeviceRepository2, GenericDbObjectCacheTestMockData.DateTime3, true, new List<long> { 2, 3, 4 });
            Add(mockDbDeviceRepository2, GenericDbObjectCacheTestMockData.DateTime4, true, new List<long> { 4 });
        }
    }

    /// <summary>
    /// Unit tests for <see cref="IGenericDbObjectCache{T}"/> implementations.
    /// </summary>
    public class GenericDbObjectCacheTests
    {
        readonly IConfiguration adapterConfig;
        readonly IDateTimeHelper dateTimeHelper;
        readonly IExceptionHelper exceptionHelper;
        readonly IConfigurationHelper adapterConfigurationHelper;
        readonly IAdapterConfiguration adapterConfiguration;
        readonly IAdapterDatabaseConnectionInfoContainer adapterConnectionInfoContainer;
        readonly IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext> adapterContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericDbObjectCacheTests"/> class.
        /// </summary>
        public GenericDbObjectCacheTests()
        {
            //Initialise configuration test environment.
            string projectPath = AppDomain.CurrentDomain.BaseDirectory.Split(new String[] { @"bin\" }, StringSplitOptions.None)[0];
            adapterConfig = new ConfigurationBuilder()
               .SetBasePath(projectPath)
               .AddJsonFile("appsettingsTest.json")
               .Build();

            // Initialize other objects that are required but not actually used in these tests.
            dateTimeHelper = new DateTimeHelper();
            exceptionHelper = new ExceptionHelper();
            adapterConfigurationHelper = new ConfigurationHelper(adapterConfig);
            adapterConfiguration = new AdapterConfiguration(adapterConfigurationHelper);
            adapterConnectionInfoContainer = new AdapterDatabaseConnectionInfoContainer(adapterConfiguration, exceptionHelper);
            var adapterDatabaseUnitOfWorkContext = new AdapterDatabaseUnitOfWorkContext(adapterConnectionInfoContainer);
            adapterContext = new GenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>(adapterDatabaseUnitOfWorkContext);
        }

        //[Theory]
        //[ClassData(typeof(GenericDbObjectCacheTest_GetObjectAsyncById_TestData))]
        //public async Task GenericDbObjectCacheTest_GetObjectAsyncById(MockBaseRepository2<DbDevice> mockDbEntityRepo, long idOfObjectToGet, bool shouldReturnObject, long? expectedObjectId)
        //{ 
        //    var dbDeviceObjectCache = new AdapterGenericDbObjectCache<DbDevice>(dateTimeHelper, adapterContext, mockDbEntityRepo);
        //    await dbDeviceObjectCache.InitializeAsync(Databases.AdapterDatabase);
        //    var dbDevice = await dbDeviceObjectCache.GetObjectAsync(idOfObjectToGet);
        //    if (shouldReturnObject)
        //    {
        //        Assert.Equal(dbDevice.id, expectedObjectId);
        //    }
        //    else
        //    {
        //        Assert.Null(dbDevice);
        //    }
        //}

        //[Theory]
        //[ClassData(typeof(GenericDbObjectCacheTest_GetObjectAsyncByGeotabId_TestData))]
        //public async Task GenericDbObjectCacheTest_GetObjectAsyncByGeotabId(MockBaseRepository2<DbDevice> mockDbEntityRepo, string geotabIdOfObjectToGet, bool shouldReturnObject, long? expectedObjectId)
        //{
        //    var dbDeviceObjectCache = new AdapterGenericDbObjectCache<DbDevice>(dateTimeHelper, (IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>)adapterContext, mockDbEntityRepo);
        //    await dbDeviceObjectCache.InitializeAsync(Databases.AdapterDatabase);
        //    var dbDevice = await dbDeviceObjectCache.GetObjectAsync(geotabIdOfObjectToGet);
        //    if (shouldReturnObject)
        //    {
        //        Assert.Equal(dbDevice.id, expectedObjectId);
        //    }
        //    else
        //    {
        //        Assert.Null(dbDevice);
        //    }
        //}

        //[Theory]
        //[ClassData(typeof(GenericDbObjectCacheTest_GetObjectIdAsyncByGeotabId_TestData))]
        //public async Task GenericDbObjectCacheTest_GetObjectIdAsyncByGeotabId(MockBaseRepository2<DbDevice> mockDbEntityRepo, string geotabIdOfObjectIdGet, long? expectedObjectId)
        //{
        //    var dbDeviceObjectCache = new AdapterGenericDbObjectCache<DbDevice>(dateTimeHelper, (IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>)adapterContext, mockDbEntityRepo);
        //    await dbDeviceObjectCache.InitializeAsync(Databases.AdapterDatabase);
        //    var objectId = await dbDeviceObjectCache.GetObjectIdAsync(geotabIdOfObjectIdGet);
        //    Assert.Equal(objectId, expectedObjectId);
        //}

        //[Theory]
        //[ClassData(typeof(GenericDbObjectCacheTest_GetObjectsAsync_TestData))]
        //public async Task GenericDbObjectCacheTest_GetObjectsAsync(MockBaseRepository2<DbDevice> mockDbEntityRepo, bool shouldReturnObjects, List<long> expectedObjectIds)
        //{
        //    var dbDeviceObjectCache = new AdapterGenericDbObjectCache<DbDevice>(dateTimeHelper, (IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>)adapterContext, mockDbEntityRepo);
        //    await dbDeviceObjectCache.InitializeAsync(Databases.AdapterDatabase);
        //    var dbDevices = await dbDeviceObjectCache.GetObjectsAsync();
        //    if (shouldReturnObjects)
        //    {
        //        Assert.Equal(dbDevices.Count, expectedObjectIds.Count);
        //        foreach (var expectedObjectId in expectedObjectIds)
        //        {
        //            Assert.Contains(dbDevices, dbDevice => dbDevice.id == expectedObjectId);
        //        }
        //    }
        //    else
        //    {
        //        Assert.Empty(dbDevices);
        //    }
        //}

        //[Theory]
        //[ClassData(typeof(GenericDbObjectCacheTest_GetObjectsAsyncByChangedDateTime_TestData))]
        //public async Task GenericDbObjectCacheTest_GetObjectsAsyncByChangedDateTime(MockBaseRepository2<DbDevice> mockDbEntityRepo, DateTime changedSince, bool shouldReturnObjects, List<long> expectedObjectIds)
        //{
        //    var dbDeviceObjectCache = new AdapterGenericDbObjectCache<DbDevice>(dateTimeHelper, (IGenericDatabaseUnitOfWorkContext<AdapterDatabaseUnitOfWorkContext>)adapterContext, mockDbEntityRepo);
        //    await dbDeviceObjectCache.InitializeAsync(Databases.AdapterDatabase);
        //    var dbDevices = await dbDeviceObjectCache.GetObjectsAsync(changedSince);
        //    if (shouldReturnObjects)
        //    {
        //        Assert.Equal(dbDevices.Count, expectedObjectIds.Count);
        //        foreach (var expectedObjectId in expectedObjectIds)
        //        {
        //            Assert.Contains(dbDevices, dbDevice => dbDevice.id == expectedObjectId);
        //        }
        //    }
        //    else
        //    {
        //        Assert.Empty(dbDevices);
        //    }
        //}
    }
}
