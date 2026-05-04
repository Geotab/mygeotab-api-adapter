using System;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.EntityMappers;
using MyGeotabAPIAdapter.Database.Models;
using Xunit;

namespace MyGeotabAPIAdapter.Tests.GeotabDIGAdapter.Core.Database.EntityMappers
{
    public class DIGRecordEntityMapperTests
    {
        private const string TestSerialNo = "G9TEST123456";
        private const string TestThirdPartyId = "THIRD-PARTY-001";
        private const string TestFailureReason = "Device not provisioned";
        private static readonly DateTime TestDateTime = new(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime TestRecordLastChangedUtc = new(2026, 2, 11, 11, 59, 0, DateTimeKind.Utc);

        #region GpsRecord Mappers

        [Fact]
        public void GpsRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQGpsRecordDIGGpsRecordEntityMapper();
            var source = new DbGdaQGpsRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Latitude = 43.4516f,
                Longitude = -80.4925f,
                Speed = 65.5f,
                IsGpsValid = true,
                IsIgnitionOn = true,
                IsAuxiliary1On = false,
                IsAuxiliary2On = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal(TestDateTime, result.DateTime);
            Assert.Equal(43.4516f, result.Latitude);
            Assert.Equal(-80.4925f, result.Longitude);
            Assert.Equal(65.5f, result.Speed);
            Assert.True(result.IsGpsValid);
            Assert.True(result.IsIgnitionOn);
            Assert.False(result.IsAuxiliary1On);
            Assert.True(result.IsAuxiliary2On);
            Assert.Equal("GpsRecord", result.Type);
        }

        [Fact]
        public void GpsRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQGpsRecordDbGdaQGpsRecordFailEntityMapper();
            var source = new DbGdaQGpsRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Latitude = 43.4516f,
                Longitude = -80.4925f,
                Speed = 65.5f,
                IsGpsValid = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal(TestThirdPartyId, result.ThirdPartyId);
            Assert.Equal(TestDateTime, result.DateTime);
            Assert.Equal(43.4516f, result.Latitude);
            Assert.Equal(-80.4925f, result.Longitude);
            Assert.Equal(TestFailureReason, result.FailureReason);
            Assert.Equal(TestRecordLastChangedUtc, result.OriginalRecordLastChangedUtc);
            Assert.Equal(Common.DatabaseWriteOperationType.Insert, result.DatabaseWriteOperationType);
        }

        #endregion

        #region AccelerationRecord Mappers

        [Fact]
        public void AccelerationRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQAccelerationRecordDIGAccelerationRecordEntityMapper();
            var source = new DbGdaQAccelerationRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                X = 100,
                Y = -50,
                Z = 980,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal(TestDateTime, result.DateTime);
            Assert.Equal((short)100, result.X);
            Assert.Equal((short)-50, result.Y);
            Assert.Equal((short)980, result.Z);
            Assert.Equal("AccelerationRecord", result.Type);
        }

        [Fact]
        public void AccelerationRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQAccelerationRecordDbGdaQAccelerationRecordFailEntityMapper();
            var source = new DbGdaQAccelerationRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                X = 100,
                Y = -50,
                Z = 980,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal(100, result.X);
            Assert.Equal(-50, result.Y);
            Assert.Equal(980, result.Z);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion

        #region BinaryRecord Mappers

        [Fact]
        public void BinaryRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQBinaryRecordDIGBinaryRecordEntityMapper();
            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var source = new DbGdaQBinaryRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Data = testData,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal(testData, result.Data);
            Assert.Equal("BinaryRecord", result.Type);
        }

        [Fact]
        public void BinaryRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQBinaryRecordDbGdaQBinaryRecordFailEntityMapper();
            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var source = new DbGdaQBinaryRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Data = testData,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal(testData, result.Data);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion

        #region BluetoothRecord Mappers

        [Fact]
        public void BluetoothRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQBluetoothRecordDIGBluetoothRecordEntityMapper();
            var source = new DbGdaQBluetoothRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Address = "FF:FF:FF:00:AA:9B",
                Data = 25.5f,
                DataType = 1,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal("FF:FF:FF:00:AA:9B", result.Address);
            Assert.Equal(25.5f, result.Data);
            Assert.Equal(1, result.DataType);
            Assert.Equal("BluetoothRecord", result.Type);
        }

        [Fact]
        public void BluetoothRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper();
            var source = new DbGdaQBluetoothRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Address = "FF:FF:FF:00:AA:9B",
                Data = 25.5f,
                DataType = 1,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal("FF:FF:FF:00:AA:9B", result.Address);
            Assert.Equal(25.5f, result.Data);
            Assert.Equal(1, result.DataType);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion

        #region DriverChangeRecord Mappers

        [Fact]
        public void DriverChangeRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQDriverChangeRecordDIGDriverChangeRecordEntityMapper();
            var driverId = new byte[] { 0xAA, 0xBB, 0xCC };
            var source = new DbGdaQDriverChangeRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                KeyType = 1,
                DriverId = driverId,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal(1, result.KeyType);
            Assert.Equal(driverId, result.DriverId);
            Assert.Equal("DriverChangeRecord", result.Type);
        }

        [Fact]
        public void DriverChangeRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQDriverChangeRecordDbGdaQDriverChangeRecordFailEntityMapper();
            var driverId = new byte[] { 0xAA, 0xBB, 0xCC };
            var source = new DbGdaQDriverChangeRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                KeyType = 1,
                DriverId = driverId,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal(1, result.KeyType);
            Assert.Equal(driverId, result.DriverId);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion

        #region GenericFaultRecord Mappers

        [Fact]
        public void GenericFaultRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQGenericFaultRecordDIGGenericFaultRecordEntityMapper();
            var source = new DbGdaQGenericFaultRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Code = 500,
                FaultStateActive = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal(500, result.Code);
            Assert.True(result.FaultStateActive);
            Assert.Equal("GenericFaultRecord", result.Type);
        }

        [Fact]
        public void GenericFaultRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQGenericFaultRecordDbGdaQGenericFaultRecordFailEntityMapper();
            var source = new DbGdaQGenericFaultRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Code = 500,
                FaultStateActive = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal(500, result.Code);
            Assert.True(result.FaultStateActive);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion

        #region GenericStatusRecord Mappers

        [Fact]
        public void GenericStatusRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper();
            var source = new DbGdaQGenericStatusRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Code = 2000,
                Value = 12345,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal(2000, result.Code);
            Assert.Equal(12345, result.Value);
            Assert.Equal("GenericStatusRecord", result.Type);
        }

        [Fact]
        public void GenericStatusRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQGenericStatusRecordDbGdaQGenericStatusRecordFailEntityMapper();
            var source = new DbGdaQGenericStatusRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Code = 2000,
                Value = 12345,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal(2000, result.Code);
            Assert.Equal(12345, result.Value);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion

        #region J1708FaultRecord Mappers

        [Fact]
        public void J1708FaultRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQJ1708FaultRecordDIGJ1708FaultRecordEntityMapper();
            var source = new DbGdaQJ1708FaultRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                MessageId = 128,
                ParameterId = 100,
                SubsystemId = 50,
                FailureModeIdentifier = 5,
                OccurrenceCount = 3,
                FaultStateActive = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal(128, result.MessageId);
            Assert.Equal(100, result.ParameterId);
            Assert.Equal(50, result.SubsystemId);
            Assert.Equal(5, result.FailureModeIdentifier);
            Assert.Equal(3, result.OccurrenceCount);
            Assert.True(result.FaultStateActive);
            Assert.Equal("J1708FaultRecord", result.Type);
        }

        [Fact]
        public void J1708FaultRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQJ1708FaultRecordDbGdaQJ1708FaultRecordFailEntityMapper();
            var source = new DbGdaQJ1708FaultRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                MessageId = 128,
                ParameterId = 100,
                SubsystemId = 50,
                FailureModeIdentifier = 5,
                OccurrenceCount = 3,
                FaultStateActive = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal((byte)128, result.MessageId);
            Assert.Equal((short?)100, result.ParameterId);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion

        #region J1939FaultRecord Mappers

        [Fact]
        public void J1939FaultRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQJ1939FaultRecordDIGJ1939FaultRecordEntityMapper();
            var source = new DbGdaQJ1939FaultRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                SuspectParameterNumber = 190,
                FailureModeIdentifier = 14,
                OccurrenceCount = 2,
                SourceAddress = 0,
                MalfunctionLamp = true,
                RedStopLamp = false,
                AmberWarningLamp = true,
                ProtectWarningLamp = false,
                FaultStateActive = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal(190, result.SuspectParameterNumber);
            Assert.Equal(14, result.FailureModeIdentifier);
            Assert.Equal(2, result.OccurrenceCount);
            Assert.Equal(0, result.SourceAddress);
            Assert.True(result.MalfunctionLamp);
            Assert.False(result.RedStopLamp);
            Assert.True(result.AmberWarningLamp);
            Assert.False(result.ProtectWarningLamp);
            Assert.True(result.FaultStateActive);
            Assert.Equal("J1939FaultRecord", result.Type);
        }

        [Fact]
        public void J1939FaultRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQJ1939FaultRecordDbGdaQJ1939FaultRecordFailEntityMapper();
            var source = new DbGdaQJ1939FaultRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                SuspectParameterNumber = 190,
                FailureModeIdentifier = 14,
                OccurrenceCount = 2,
                SourceAddress = 0,
                MalfunctionLamp = true,
                RedStopLamp = false,
                FaultStateActive = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal(190, result.SuspectParameterNumber);
            Assert.True(result.MalfunctionLamp);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion

        #region ObdiiFaultRecord Mappers

        [Fact]
        public void ObdiiFaultRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQObdiiFaultRecordDIGObdiiFaultRecordEntityMapper();
            var source = new DbGdaQObdiiFaultRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Code = "P0420",
                FaultStateActive = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal("P0420", result.Code);
            Assert.True(result.FaultStateActive);
            Assert.Equal("ObdiiFaultRecord", result.Type);
        }

        [Fact]
        public void ObdiiFaultRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQObdiiFaultRecordDbGdaQObdiiFaultRecordFailEntityMapper();
            var source = new DbGdaQObdiiFaultRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                Code = "P0420",
                FaultStateActive = true,
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal("P0420", result.Code);
            Assert.True(result.FaultStateActive);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion

        #region VinRecord Mappers

        [Fact]
        public void VinRecordMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQVinRecordDIGVinRecordEntityMapper();
            var source = new DbGdaQVinRecord
            {
                id = 1,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                VehicleIdentificationNumber = "1HGBH41JXMN109186",
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestSerialNo);

            Assert.Equal(TestSerialNo, result.SerialNo);
            Assert.Equal("1HGBH41JXMN109186", result.VehicleIdentificationNumber);
            Assert.Equal("VinRecord", result.Type);
        }

        [Fact]
        public void VinRecordFailMapper_MapsAllFieldsCorrectly()
        {
            var mapper = new DbGdaQVinRecordDbGdaQVinRecordFailEntityMapper();
            var source = new DbGdaQVinRecord
            {
                id = 42,
                ThirdPartyId = TestThirdPartyId,
                DateTime = TestDateTime,
                VehicleIdentificationNumber = "1HGBH41JXMN109186",
                RecordLastChangedUtc = TestRecordLastChangedUtc
            };

            var result = mapper.CreateEntity(source, TestFailureReason);

            Assert.Equal(42, result.OriginalQueueId);
            Assert.Equal("1HGBH41JXMN109186", result.VehicleIdentificationNumber);
            Assert.Equal(TestFailureReason, result.FailureReason);
        }

        #endregion
    }
}
