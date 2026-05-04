using System;
using System.Collections.Generic;
using System.Text.Json;
using MyGeotabAPIAdapter.DIGAPI.Models;
using Xunit;

namespace MyGeotabAPIAdapter.Tests.GeotabDIGAdapter.Core.DIGAPI.Models
{
    public class DIGRecordSerializationTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        [Fact]
        public void DIGGpsRecord_SerializesToCorrectJson()
        {
            var record = new DIGGpsRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                Latitude = 43.4516f,
                Longitude = -80.4925f,
                Speed = 65.5f,
                IsGpsValid = true,
                IsIgnitionOn = true
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"GpsRecord\"", json);
            Assert.Contains("\"SerialNo\":\"G9TEST123456\"", json);
            Assert.Contains("\"Latitude\":43.4516", json);
            Assert.Contains("\"Longitude\":-80.4925", json);
            Assert.Contains("\"Speed\":65.5", json);
            Assert.Contains("\"IsGpsValid\":true", json);
            Assert.Contains("\"IsIgnitionOn\":true", json);
        }

        [Fact]
        public void DIGGpsRecord_OmitsNullOptionalFields()
        {
            var record = new DIGGpsRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                Latitude = 43.4516f,
                Longitude = -80.4925f
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.DoesNotContain("\"Speed\"", json);
            Assert.DoesNotContain("\"IsGpsValid\"", json);
            Assert.DoesNotContain("\"IsAuxiliary1On\"", json);
        }

        [Fact]
        public void DIGAccelerationRecord_SerializesToCorrectJson()
        {
            var record = new DIGAccelerationRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                X = 100,
                Y = -50,
                Z = 980
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"AccelerationRecord\"", json);
            Assert.Contains("\"X\":100", json);
            Assert.Contains("\"Y\":-50", json);
            Assert.Contains("\"Z\":980", json);
        }

        [Fact]
        public void DIGBinaryRecord_SerializesDataAsBase64()
        {
            var testData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var record = new DIGBinaryRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                Data = testData
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"BinaryRecord\"", json);
            Assert.Contains("\"Data\":\"AQIDBA==\"", json); // Base64 of {0x01, 0x02, 0x03, 0x04}
        }

        [Fact]
        public void DIGBluetoothRecord_SerializesToCorrectJson()
        {
            var record = new DIGBluetoothRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                Address = "FF:FF:FF:00:AA:9B",
                Data = 25.5f,
                DataType = 1
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"BluetoothRecord\"", json);
            Assert.Contains("\"Address\":\"FF:FF:FF:00:AA:9B\"", json);
            Assert.Contains("\"Data\":25.5", json);
            Assert.Contains("\"DataType\":1", json);
        }

        [Fact]
        public void DIGDriverChangeRecord_SerializesToCorrectJson()
        {
            var driverId = new byte[] { 0xAA, 0xBB, 0xCC };
            var record = new DIGDriverChangeRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                KeyType = 1,
                DriverId = driverId
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"DriverChangeRecord\"", json);
            Assert.Contains("\"KeyType\":1", json);
            Assert.Contains("\"DriverId\":\"qrvM\"", json); // Base64 of {0xAA, 0xBB, 0xCC}
        }

        [Fact]
        public void DIGGenericFaultRecord_SerializesToCorrectJson()
        {
            var record = new DIGGenericFaultRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                Code = 500,
                FaultStateActive = true
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"GenericFaultRecord\"", json);
            Assert.Contains("\"Code\":500", json);
            Assert.Contains("\"FaultStateActive\":true", json);
        }

        [Fact]
        public void DIGGenericStatusRecord_SerializesToCorrectJson()
        {
            var record = new DIGGenericStatusRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                Code = 2000,
                Value = 12345
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"GenericStatusRecord\"", json);
            Assert.Contains("\"Code\":2000", json);
            Assert.Contains("\"Value\":12345", json);
        }

        [Fact]
        public void DIGJ1708FaultRecord_SerializesToCorrectJson()
        {
            var record = new DIGJ1708FaultRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                MessageId = 128,
                ParameterId = 100,
                SubsystemId = 50,
                FailureModeIdentifier = 5,
                OccurrenceCount = 3,
                FaultStateActive = true
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"J1708FaultRecord\"", json);
            Assert.Contains("\"MessageId\":128", json);
            Assert.Contains("\"ParameterId\":100", json);
            Assert.Contains("\"SubsystemId\":50", json);
            Assert.Contains("\"FailureModeIdentifier\":5", json);
            Assert.Contains("\"OccurrenceCount\":3", json);
        }

        [Fact]
        public void DIGJ1939FaultRecord_SerializesToCorrectJson()
        {
            var record = new DIGJ1939FaultRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                SuspectParameterNumber = 190,
                FailureModeIdentifier = 14,
                OccurrenceCount = 2,
                SourceAddress = 0,
                MalfunctionLamp = true,
                RedStopLamp = false,
                AmberWarningLamp = true,
                ProtectWarningLamp = false,
                FaultStateActive = true
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"J1939FaultRecord\"", json);
            Assert.Contains("\"SuspectParameterNumber\":190", json);
            Assert.Contains("\"MalfunctionLamp\":true", json);
            Assert.Contains("\"RedStopLamp\":false", json);
            Assert.Contains("\"AmberWarningLamp\":true", json);
        }

        [Fact]
        public void DIGObdiiFaultRecord_SerializesToCorrectJson()
        {
            var record = new DIGObdiiFaultRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                Code = "P0420",
                FaultStateActive = true
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"ObdiiFaultRecord\"", json);
            Assert.Contains("\"Code\":\"P0420\"", json);
            Assert.Contains("\"FaultStateActive\":true", json);
        }

        [Fact]
        public void DIGVinRecord_SerializesToCorrectJson()
        {
            var record = new DIGVinRecord
            {
                DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                SerialNo = "G9TEST123456",
                VehicleIdentificationNumber = "1HGBH41JXMN109186"
            };

            var json = JsonSerializer.Serialize(record, JsonOptions);

            Assert.Contains("\"Type\":\"VinRecord\"", json);
            Assert.Contains("\"VehicleIdentificationNumber\":\"1HGBH41JXMN109186\"", json);
        }

        [Fact]
        public void MixedRecordBatch_SerializesToJsonArray()
        {
            var records = new List<DIGBaseRecord>
            {
                new DIGGpsRecord
                {
                    DateTime = new DateTime(2026, 2, 11, 12, 0, 0, DateTimeKind.Utc),
                    SerialNo = "G9TEST123456",
                    Latitude = 43.4516f,
                    Longitude = -80.4925f
                },
                new DIGAccelerationRecord
                {
                    DateTime = new DateTime(2026, 2, 11, 12, 0, 1, DateTimeKind.Utc),
                    SerialNo = "G9TEST123456",
                    X = 100,
                    Y = -50,
                    Z = 980
                },
                new DIGVinRecord
                {
                    DateTime = new DateTime(2026, 2, 11, 12, 0, 2, DateTimeKind.Utc),
                    SerialNo = "G9TEST123456",
                    VehicleIdentificationNumber = "1HGBH41JXMN109186"
                }
            };

            var json = JsonSerializer.Serialize(records, JsonOptions);

            Assert.StartsWith("[", json);
            Assert.EndsWith("]", json);
            Assert.Contains("\"Type\":\"GpsRecord\"", json);
            Assert.Contains("\"Type\":\"AccelerationRecord\"", json);
            Assert.Contains("\"Type\":\"VinRecord\"", json);
        }

        [Fact]
        public void AllRecordTypes_HaveCorrectTypeValue()
        {
            Assert.Equal("GpsRecord", new DIGGpsRecord().Type);
            Assert.Equal("AccelerationRecord", new DIGAccelerationRecord().Type);
            Assert.Equal("BinaryRecord", new DIGBinaryRecord().Type);
            Assert.Equal("BluetoothRecord", new DIGBluetoothRecord().Type);
            Assert.Equal("DriverChangeRecord", new DIGDriverChangeRecord().Type);
            Assert.Equal("GenericFaultRecord", new DIGGenericFaultRecord().Type);
            Assert.Equal("GenericStatusRecord", new DIGGenericStatusRecord().Type);
            Assert.Equal("J1708FaultRecord", new DIGJ1708FaultRecord().Type);
            Assert.Equal("J1939FaultRecord", new DIGJ1939FaultRecord().Type);
            Assert.Equal("ObdiiFaultRecord", new DIGObdiiFaultRecord().Type);
            Assert.Equal("VinRecord", new DIGVinRecord().Type);
        }
    }
}
