using System;
using Geotab.Checkmate.ObjectModel;
using Moq;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.GeotabObjectMappers;
using MyGeotabAPIAdapter.MyGeotabAPI;
using Xunit;

namespace MyGeotabAPIAdapter.Tests.GeotabObjectMappers
{
    public class GeotabShipmentLogDbStgShipmentLog2ObjectMapperTests
    {
        static readonly Guid TestGuid = new("11112222-3333-4444-5555-666677778888");
        static readonly DateTime TestDateTime = new(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc);
        static readonly DateTime TestActiveFrom = new(2026, 2, 11, 10, 0, 0, DateTimeKind.Utc);
        static readonly DateTime TestActiveTo = new(2026, 2, 11, 18, 0, 0, DateTimeKind.Utc);
        const long TestDeviceId = 101L;
        const long TestDriverId = 202L;
        const long TestVersion = 999L;

        static GeotabShipmentLogDbStgShipmentLog2ObjectMapper CreateMapper()
        {
            var mockGeotabIdConverter = new Mock<IGeotabIdConverter>();
            mockGeotabIdConverter.Setup(c => c.ToGuid(It.IsAny<Id>())).Returns(TestGuid);
            return new GeotabShipmentLogDbStgShipmentLog2ObjectMapper(mockGeotabIdConverter.Object);
        }

        [Fact]
        public void CreateEntity_MapsAllFieldsCorrectly()
        {
            var mapper = CreateMapper();
            var shipmentLogId = Id.Create("b1");
            var shipmentLog = new ShipmentLog
            {
                Id = shipmentLogId,
                DateTime = TestDateTime,
                ActiveFrom = TestActiveFrom,
                ActiveTo = TestActiveTo,
                Commodity = "Electronics",
                DocumentNumber = "DOC-12345",
                ShipperName = "Acme Freight",
                Version = TestVersion
            };

            var result = mapper.CreateEntity(shipmentLog, TestDeviceId, TestDriverId);

            Assert.Equal(Common.DatabaseWriteOperationType.Insert, result.DatabaseWriteOperationType);
            Assert.Equal(TestDateTime, result.DateTime);
            Assert.Equal(TestDeviceId, result.DeviceId);
            Assert.Equal(TestDriverId, result.DriverId);
            Assert.Equal(shipmentLogId.ToString(), result.GeotabId);
            Assert.Equal(TestGuid, result.id);
            Assert.Equal(TestActiveFrom, result.ActiveFrom);
            Assert.Equal(TestActiveTo, result.ActiveTo);
            Assert.Equal("Electronics", result.Commodity);
            Assert.Equal("DOC-12345", result.DocumentNumber);
            Assert.Equal("Acme Freight", result.ShipperName);
            Assert.Equal(TestVersion, result.Version);
            Assert.NotEqual(default, result.RecordLastChangedUtc);
        }

        [Fact]
        public void CreateEntity_LeavesOptionalFieldsNull_WhenSourceValuesAreNull()
        {
            var mapper = CreateMapper();
            var shipmentLog = new ShipmentLog
            {
                Id = Id.Create("b2"),
                DateTime = TestDateTime,
                ActiveFrom = null,
                ActiveTo = null,
                Commodity = null,
                DocumentNumber = null,
                ShipperName = null,
                Version = null
            };

            var result = mapper.CreateEntity(shipmentLog, TestDeviceId, TestDriverId);

            Assert.Null(result.ActiveFrom);
            Assert.Null(result.ActiveTo);
            Assert.Null(result.Commodity);
            Assert.Null(result.DocumentNumber);
            Assert.Null(result.ShipperName);
            Assert.Null(result.Version);
            // Required fields are still populated.
            Assert.Equal(TestDateTime, result.DateTime);
            Assert.Equal(TestGuid, result.id);
        }

        [Fact]
        public void CreateEntity_AllowsNullDeviceAndDriverIds()
        {
            var mapper = CreateMapper();
            var shipmentLog = new ShipmentLog
            {
                Id = Id.Create("b3"),
                DateTime = TestDateTime
            };

            var result = mapper.CreateEntity(shipmentLog, null, null);

            Assert.Null(result.DeviceId);
            Assert.Null(result.DriverId);
            Assert.Equal(Common.DatabaseWriteOperationType.Insert, result.DatabaseWriteOperationType);
        }
    }
}
