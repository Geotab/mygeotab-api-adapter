# Geotab DIG Adapter - Database Schema Reference

This file is a concise, query-oriented reference to the Geotab DIG Adapter database schema. It is designed to be consumed by AI tools and LLMs to generate correct SQL statements for both **SQL Server** and **PostgreSQL**.

- For architecture, configuration, and operational guidance, see the [README](README.md).
- For deployment scripts, see `GeotabDIGAdapter/Scripts/`.
- **Schema**: All tables reside in the `gda` schema within the `geotabadapterdb` database.
- **Database user**: `geotabdigadapter_client`

---

## Table Inventory

| Table | Category | Purpose |
|-------|----------|---------|
| [`MiddlewareVersionInfo`](#middlewareversioninfo) | System | Tracks database schema version |
| [`OServiceTracking`](#oservicetracking) | System | Runtime metrics for each adapter service |
| [`ProvisionedDevices`](#provisioneddevices) | Provisioning | Successfully provisioned devices |
| [`Q_ProvisionDevices`](#q_provisiondevices) | Provisioning Queue | Device provisioning requests |
| [`Q_ProvisionDevicesFail`](#common-fail-table-columns) | Provisioning Fail | Failed provisioning attempts |
| [`Q_GpsRecords`](#q_gpsrecords) | Telemetry Queue | GPS location data |
| [`Q_AccelerationRecords`](#q_accelerationrecords) | Telemetry Queue | 3-axis accelerometer data |
| [`Q_BinaryRecords`](#q_binaryrecords) | Telemetry Queue | Arbitrary binary/string data |
| [`Q_BluetoothRecords`](#q_bluetoothrecords) | Telemetry Queue | Bluetooth beacon data |
| [`Q_DriverChangeRecords`](#q_driverchangerecords) | Telemetry Queue | Driver identification changes |
| [`Q_GenericFaultRecords`](#q_genericfaultrecords) | Telemetry Queue | Generic fault codes (128-1999) |
| [`Q_GenericStatusRecords`](#q_genericstatusrecords) | Telemetry Queue | Generic status codes (<=127 or >=2000) |
| [`Q_J1708FaultRecords`](#q_j1708faultrecords) | Telemetry Queue | J1708 protocol fault data |
| [`Q_J1939FaultRecords`](#q_j1939faultrecords) | Telemetry Queue | J1939 protocol fault data |
| [`Q_ObdiiFaultRecords`](#q_obdiifaultrecords) | Telemetry Queue | OBDII diagnostic trouble codes |
| [`Q_VinRecords`](#q_vinrecords) | Telemetry Queue | Vehicle Identification Numbers |
| [`Q_*RecordsFail`](#common-fail-table-columns) | Telemetry Fail | One fail table per queue table (11 total) |
| [`DIGInvalidRecords`](#diginvalidrecords) | Invalid Records | Records rejected by the DIG API |
| [`DIGInvalidRecordsCursor`](#diginvalidrecordscursor) | Invalid Records | Pagination cursor (single-row table) |

---

## Provider-Specific Syntax

| Concept | SQL Server | PostgreSQL |
|---------|-----------|------------|
| Schema prefix | `[gda].[TableName]` | `gda."TableName"` |
| Current UTC time | `SYSUTCDATETIME()` or `GETUTCDATE()` | `now() AT TIME ZONE 'UTC'` |
| Boolean true/false | `1` / `0` | `true` / `false` |
| Boolean type | `bit` | `boolean` |
| Small integer (0-255) | `tinyint` | `smallint` |
| Timestamp type | `datetime2(7)` | `timestamp without time zone` |
| Binary data | `varbinary(max)` or `varbinary(239)` | `bytea` |
| Large text | `nvarchar(max)` | `text` |
| Unicode strings | `nvarchar(N)` | `varchar(N)` |
| Identity column | `bigint IDENTITY(1,1)` | `bigint GENERATED ALWAYS AS IDENTITY` |
| Filtered index | `WHERE ([Col] = 0)` | `WHERE "Col" = 0` |
| Read-only table hint | `WITH (NOLOCK)` | *(not needed — MVCC)* |
| Call stored procedure | `EXEC gda.spName @Param = value` | `SELECT * FROM gda."spName"(value)` |

---

## Enum / Status Values

### ProcessingStatus (all queue tables)

| Value | Meaning | Description |
|-------|---------|-------------|
| `0` | Pending | Record is waiting to be processed (default) |
| `1` | In Progress | Record has been claimed by the adapter for processing |

### DriverChangeRecords KeyType

| Value | Meaning |
|-------|---------|
| `84` | Custom key type (recommended for custom telematics integrations) |

### Status/Fault Code Classification

Each diagnostic code belongs to exactly one record type. Codes placed in the wrong queue are silently dropped by DIG.

| Code Range | Correct Queue | DIG Behavior |
|------------|---------------|--------------|
| ≤127 | `Q_GenericStatusRecords` | Accepted; maps to Telematics Device diagnostics |
| 128–1999 | `Q_GenericFaultRecords` | Accepted; treated as a generic fault |
| ≥2000 | `Q_GenericStatusRecords` | Accepted; maps to Telematics Device diagnostics |

---

## Logical Relationships

There are no enforced foreign keys between tables. The following are logical relationships used by the adapter application:

| From Column | To Table.Column | Relationship |
|-------------|-----------------|-------------|
| `Q_*.ThirdPartyId` | `ProvisionedDevices.ThirdPartyId` | Device must be provisioned before telemetry is accepted |
| `Q_*Fail.OriginalQueueId` | `Q_*.id` | Links fail record to original queue record |
| `Q_*Fail.ThirdPartyId` | `ProvisionedDevices.ThirdPartyId` | Same device reference as the original queue record |
| `DIGInvalidRecords.SerialNo` | `ProvisionedDevices.GeotabSerialNumber` | Geotab serial of the device that produced the invalid record |

---

## Table Definitions

In the tables below, the **MSSQL / PG Type** column shows SQL Server type first, then PostgreSQL type. Where both use the same type, it is listed once. The **Default** column shows the default value applied on INSERT if the column is omitted.

### MiddlewareVersionInfo

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `DatabaseVersion` | nvarchar(50) / varchar(50) | NO | — | Schema version (e.g., `5.0.0.0`) |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | — | When the version record was inserted |

[↑ Table Inventory](#table-inventory)

### OServiceTracking

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `ServiceId` | nvarchar(50) / varchar(50) | NO | — | Unique service identifier |
| `AdapterVersion` | nvarchar(50) / varchar(50) | YES | — | Application version |
| `AdapterMachineName` | nvarchar(100) / varchar(100) | YES | — | Host machine name |
| `EntitiesLastProcessedUtc` | datetime2 / timestamp | YES | — | Last processing timestamp |
| `LastProcessedFeedVersion` | bigint | YES | — | Feed version marker |
| `LastBatchSize` | int / integer | YES | — | Records in last batch |
| `SuccessCount` | bigint | YES | — | Cumulative successes |
| `FailureCount` | bigint | YES | — | Cumulative failures |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | — | Last update timestamp |

[↑ Table Inventory](#table-inventory)

### ProvisionedDevices

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `ThirdPartyId` | varchar(50) | NO | — | Your device identifier (**unique index**) |
| `ErpNo` | varchar(50) | YES | — | ERP number |
| `GeotabSerialNumber` | varchar(50) | NO | — | Geotab-assigned serial number |
| `IsOkayToSendDataToGeotab` | bit / boolean | NO | — | `true` when device is ready for telemetry |
| `DeviceProvisionedDateTimeUtc` | datetime2 / timestamp | YES | — | When the device was provisioned |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | — | Last update timestamp |

**Indexes:**
- `IX_ProvisionedDevices_ThirdPartyId` (PG) / `IX_ProvisionedDevices_ThirdPartyId_Includes` (MSSQL) — Unique index on `ThirdPartyId`

[↑ Table Inventory](#table-inventory)

### Q_ProvisionDevices

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `ThirdPartyId` | varchar(50) | NO | — | Your unique device identifier |
| `ErpNo` | varchar(50) | YES | — | ERP number |
| `HardwareId` | int / integer | YES | — | Hardware identifier |
| `ProductId` | int / integer | NO | — | Product ID (assigned by Geotab during onboarding) |
| `PromoCode` | varchar(50) | YES | — | Promotional code |
| `SubPlan` | varchar(50) | YES | — | Subscription plan |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |
| `ProcessingStatus` | tinyint / smallint | NO | `0` | `0` = pending, `1` = in progress |
| `ProcessingStartTimeUtc` | datetime2 / timestamp | YES | — | Set when claimed for processing |
| `RetryCount` | tinyint / smallint | NO | `0` | Incremented on stale reclaim |

**Indexes:**
- `IX_Q_ProvisionDevices_PendingWork` — Filtered index on `id` where `ProcessingStatus = 0`

[↑ Table Inventory](#table-inventory)

### Common Queue Table Columns

All 11 telemetry queue tables (`Q_GpsRecords`, `Q_AccelerationRecords`, etc.) share these columns:

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `ThirdPartyId` | varchar(50) | NO | — | Device identifier (must exist in `ProvisionedDevices`) |
| `DateTime` | datetime2 / timestamp | NO | — | When the data was captured by the device (UTC) |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |
| `ProcessingStatus` | tinyint / smallint | NO | `0` | `0` = pending, `1` = in progress |
| `ProcessingStartTimeUtc` | datetime2 / timestamp | YES | — | Set when claimed for processing |
| `RetryCount` | tinyint / smallint | NO | `0` | Incremented on stale reclaim |

Each queue table also has a filtered index `IX_Q_<Name>_PendingWork` on `id` where `ProcessingStatus = 0`.

[↑ Table Inventory](#table-inventory)

### Record-Specific Columns

#### Q_GpsRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `Latitude` | real | NO | Latitude in decimal degrees |
| `Longitude` | real | NO | Longitude in decimal degrees |
| `Speed` | real | YES | Speed in km/h |
| `IsGpsValid` | bit / boolean | YES | Must be `true` for device to show as active in MyGeotab |
| `IsIgnitionOn` | bit / boolean | YES | Ignition state (DIG uses this for trip boundary detection) |
| `IsAuxiliary1On` ... `IsAuxiliary8On` | bit / boolean | YES | Auxiliary input states (8 columns) |

[↑ Table Inventory](#table-inventory)

#### Q_AccelerationRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `X` | smallint | NO | X-axis acceleration in milli-g |
| `Y` | smallint | NO | Y-axis acceleration in milli-g |
| `Z` | smallint | NO | Z-axis acceleration in milli-g |

[↑ Table Inventory](#table-inventory)

#### Q_BinaryRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `Data` | varbinary(max) / bytea | NO | Binary or string data, base64-encoded |

[↑ Table Inventory](#table-inventory)

#### Q_BluetoothRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `Address` | varchar(17) | NO | Bluetooth MAC address |
| `Data` | real | NO | Bluetooth data value |
| `DataType` | tinyint / smallint | NO | Bluetooth data type identifier |

[↑ Table Inventory](#table-inventory)

#### Q_DriverChangeRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `KeyType` | tinyint / smallint | NO | Driver key type (use `84` for Custom) |
| `DriverId` | varbinary(239) / bytea | NO | Driver key identifier, base64-encoded |

[↑ Table Inventory](#table-inventory)

#### Q_GenericFaultRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `Code` | int / integer | NO | Fault diagnostic code (valid range: 128-1999) |
| `FaultStateActive` | bit / boolean | NO | Whether the fault is currently active |

[↑ Table Inventory](#table-inventory)

#### Q_GenericStatusRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `Code` | int / integer | NO | Diagnostic code (<=127 or >=2000; 128-1999 silently dropped) |
| `Value` | int / integer | NO | Raw value (DIG applies: `FinalValue = (RawValue x Conversion) + Offset`) |

[↑ Table Inventory](#table-inventory)

#### Q_J1708FaultRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `MessageId` | tinyint / smallint | NO | J1708 MID |
| `ParameterId` | smallint | YES | J1708 PID |
| `SubsystemId` | smallint | YES | J1708 SID |
| `FailureModeIdentifier` | tinyint / smallint | NO | J1708 FMI |
| `OccurrenceCount` | tinyint / smallint | NO | Number of fault occurrences |
| `FaultStateActive` | bit / boolean | NO | Whether the fault is currently active |

[↑ Table Inventory](#table-inventory)

#### Q_J1939FaultRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `SuspectParameterNumber` | int / integer | NO | J1939 SPN |
| `FailureModeIdentifier` | tinyint / smallint | NO | J1939 FMI |
| `OccurrenceCount` | tinyint / smallint | NO | Number of fault occurrences |
| `SourceAddress` | tinyint / smallint | NO | J1939 source address |
| `MalfunctionLamp` | bit / boolean | YES | Malfunction indicator lamp state |
| `RedStopLamp` | bit / boolean | YES | Red stop lamp state |
| `AmberWarningLamp` | bit / boolean | YES | Amber warning lamp state |
| `ProtectWarningLamp` | bit / boolean | YES | Protect warning lamp state |
| `FaultStateActive` | bit / boolean | NO | Whether the fault is currently active |

[↑ Table Inventory](#table-inventory)

#### Q_ObdiiFaultRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `Code` | varchar(10) | NO | OBDII DTC code (e.g., `P0301`) |
| `FaultStateActive` | bit / boolean | NO | Whether the fault is currently active |

[↑ Table Inventory](#table-inventory)

#### Q_VinRecords

| Column | MSSQL / PG Type | Nullable | Description |
|--------|----------------|----------|-------------|
| `VehicleIdentificationNumber` | varchar(17) | NO | 17-character VIN |

[↑ Table Inventory](#table-inventory)

### Common Fail Table Columns

Each queue table has a corresponding fail table (`Q_*RecordsFail`). All fail tables share these columns in addition to the record-specific columns from the corresponding queue table:

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `OriginalQueueId` | bigint | NO | — | Reference to original `Q_*.id` |
| `ThirdPartyId` | varchar(50) | NO | — | Device identifier |
| `DateTime` | datetime2 / timestamp | NO | — | Original record timestamp |
| *(record-specific columns)* | — | — | — | Copied from original queue record |
| `OriginalRecordLastChangedUtc` | datetime2 / timestamp | NO | — | `RecordLastChangedUtc` from original queue record |
| `FailureReason` | nvarchar(max) / text | NO | — | Why the record failed |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |

**Indexes:** Each fail table has `IX_Q_<Name>Fail_ThirdPartyId` on `ThirdPartyId`.

[↑ Table Inventory](#table-inventory)

### DIGInvalidRecords

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `GeotabGUID` | nvarchar(50) / varchar(50) | NO | — | DIG-assigned record identifier (**unique index**) |
| `RecordType` | nvarchar(50) / varchar(50) | NO | — | Record type that was rejected |
| `SerialNo` | nvarchar(50) / varchar(50) | NO | — | Device serial number |
| `RecordDateTime` | datetime2 / timestamp | NO | — | Original record timestamp |
| `BaseRecordJson` | nvarchar(max) / text | NO | — | Full JSON of the rejected record |
| `Cause` | nvarchar(1000) / varchar(1000) | NO | — | Reason for rejection |
| `TimeStamp` | datetime2 / timestamp | NO | — | DIG server timestamp |
| `UserId` | nvarchar(100) / varchar(100) | NO | — | DIG user that submitted the record |
| `RetrievedAtUtc` | datetime2 / timestamp | NO | Current UTC | When the adapter retrieved this record |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |

**Indexes:**
- `IX_DIGInvalidRecords_GeotabGUID` — Unique index on `GeotabGUID`
- `IX_DIGInvalidRecords_RecordType_TimeStamp` — Composite index on `RecordType`, `TimeStamp DESC`

[↑ Table Inventory](#table-inventory)

### DIGInvalidRecordsCursor

Single-row table (enforced by CHECK constraint: `id = 1`). Never DELETE from this table.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | int / integer | NO | `1` | Must always be `1` |
| `NextResultKey` | int / integer | NO | `0` | Cursor for next page of results |
| `LastUpdatedUtc` | datetime2 / timestamp | NO | Current UTC | When the cursor was last updated |

[↑ Table Inventory](#table-inventory)

---

## Stored Procedures

Each queue table has a claim procedure that atomically selects and locks a batch of pending records for processing. These are called by the adapter, not by external systems. Listed here for reference only.

| Procedure | Queue Table |
|-----------|-------------|
| `gda.spClaimQProvisionDevicesBatch` | `Q_ProvisionDevices` |
| `gda.spClaimQGpsRecordsBatch` | `Q_GpsRecords` |
| `gda.spClaimQAccelerationRecordsBatch` | `Q_AccelerationRecords` |
| `gda.spClaimQBinaryRecordsBatch` | `Q_BinaryRecords` |
| `gda.spClaimQBluetoothRecordsBatch` | `Q_BluetoothRecords` |
| `gda.spClaimQDriverChangeRecordsBatch` | `Q_DriverChangeRecords` |
| `gda.spClaimQGenericFaultRecordsBatch` | `Q_GenericFaultRecords` |
| `gda.spClaimQGenericStatusRecordsBatch` | `Q_GenericStatusRecords` |
| `gda.spClaimQJ1708FaultRecordsBatch` | `Q_J1708FaultRecords` |
| `gda.spClaimQJ1939FaultRecordsBatch` | `Q_J1939FaultRecords` |
| `gda.spClaimQObdiiFaultRecordsBatch` | `Q_ObdiiFaultRecords` |
| `gda.spClaimQVinRecordsBatch` | `Q_VinRecords` |

**Parameters** (all procedures): `@BatchSize INT`, `@StaleThresholdMinutes INT`

---

## Example SQL Queries

All examples show **PostgreSQL** syntax first, then **SQL Server** syntax. Columns with defaults (`id`, `RecordCreationTimeUtc`, `RecordLastChangedUtc`, `ProcessingStatus`, `RetryCount`) are omitted from INSERT statements — the database sets them automatically.

### INSERT: Q_ProvisionDevices

```sql
-- PostgreSQL
INSERT INTO gda."Q_ProvisionDevices"
    ("ThirdPartyId", "ErpNo", "HardwareId", "ProductId", "PromoCode", "SubPlan")
VALUES
    ('DEVICE-001', '<ErpNo>', NULL, 1234, NULL, NULL);

-- SQL Server
INSERT INTO [gda].[Q_ProvisionDevices]
    ([ThirdPartyId], [ErpNo], [HardwareId], [ProductId], [PromoCode], [SubPlan])
VALUES
    ('DEVICE-001', '<ErpNo>', NULL, 1234, NULL, NULL);
```

### INSERT: Q_GpsRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_GpsRecords"
    ("ThirdPartyId", "DateTime", "Latitude", "Longitude", "Speed",
     "IsGpsValid", "IsIgnitionOn",
     "IsAuxiliary1On", "IsAuxiliary2On", "IsAuxiliary3On", "IsAuxiliary4On",
     "IsAuxiliary5On", "IsAuxiliary6On", "IsAuxiliary7On", "IsAuxiliary8On")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 43.6532, -79.3832, 55.0,
     true, true,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

-- SQL Server
INSERT INTO [gda].[Q_GpsRecords]
    ([ThirdPartyId], [DateTime], [Latitude], [Longitude], [Speed],
     [IsGpsValid], [IsIgnitionOn],
     [IsAuxiliary1On], [IsAuxiliary2On], [IsAuxiliary3On], [IsAuxiliary4On],
     [IsAuxiliary5On], [IsAuxiliary6On], [IsAuxiliary7On], [IsAuxiliary8On])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 43.6532, -79.3832, 55.0,
     1, 1,
     NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
```

### INSERT: Q_AccelerationRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_AccelerationRecords"
    ("ThirdPartyId", "DateTime", "X", "Y", "Z")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 50, -20, 980);

-- SQL Server
INSERT INTO [gda].[Q_AccelerationRecords]
    ([ThirdPartyId], [DateTime], [X], [Y], [Z])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 50, -20, 980);
```

### INSERT: Q_BinaryRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_BinaryRecords"
    ("ThirdPartyId", "DateTime", "Data")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', decode('SGVsbG8gV29ybGQ=', 'base64'));

-- SQL Server
INSERT INTO [gda].[Q_BinaryRecords]
    ([ThirdPartyId], [DateTime], [Data])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', CAST('Hello World' AS VARBINARY(MAX)));
```

### INSERT: Q_BluetoothRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_BluetoothRecords"
    ("ThirdPartyId", "DateTime", "Address", "Data", "DataType")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 'AA:BB:CC:DD:EE:FF', -65.0, 1);

-- SQL Server
INSERT INTO [gda].[Q_BluetoothRecords]
    ([ThirdPartyId], [DateTime], [Address], [Data], [DataType])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 'AA:BB:CC:DD:EE:FF', -65.0, 1);
```

### INSERT: Q_DriverChangeRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_DriverChangeRecords"
    ("ThirdPartyId", "DateTime", "KeyType", "DriverId")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 84, decode('QUJDREVGR0g=', 'base64'));

-- SQL Server
INSERT INTO [gda].[Q_DriverChangeRecords]
    ([ThirdPartyId], [DateTime], [KeyType], [DriverId])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 84, CAST('ABCDEFGH' AS VARBINARY(239)));
```

### INSERT: Q_GenericFaultRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_GenericFaultRecords"
    ("ThirdPartyId", "DateTime", "Code", "FaultStateActive")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 500, true);

-- SQL Server
INSERT INTO [gda].[Q_GenericFaultRecords]
    ([ThirdPartyId], [DateTime], [Code], [FaultStateActive])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 500, 1);
```

### INSERT: Q_GenericStatusRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_GenericStatusRecords"
    ("ThirdPartyId", "DateTime", "Code", "Value")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 5, 123456);

-- SQL Server
INSERT INTO [gda].[Q_GenericStatusRecords]
    ([ThirdPartyId], [DateTime], [Code], [Value])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 5, 123456);
```

### INSERT: Q_J1708FaultRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_J1708FaultRecords"
    ("ThirdPartyId", "DateTime", "MessageId", "ParameterId", "SubsystemId",
     "FailureModeIdentifier", "OccurrenceCount", "FaultStateActive")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 128, 110, NULL, 3, 1, true);

-- SQL Server
INSERT INTO [gda].[Q_J1708FaultRecords]
    ([ThirdPartyId], [DateTime], [MessageId], [ParameterId], [SubsystemId],
     [FailureModeIdentifier], [OccurrenceCount], [FaultStateActive])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 128, 110, NULL, 3, 1, 1);
```

### INSERT: Q_J1939FaultRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_J1939FaultRecords"
    ("ThirdPartyId", "DateTime", "SuspectParameterNumber", "FailureModeIdentifier",
     "OccurrenceCount", "SourceAddress",
     "MalfunctionLamp", "RedStopLamp", "AmberWarningLamp", "ProtectWarningLamp",
     "FaultStateActive")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 190, 3,
     1, 0,
     true, false, false, false,
     true);

-- SQL Server
INSERT INTO [gda].[Q_J1939FaultRecords]
    ([ThirdPartyId], [DateTime], [SuspectParameterNumber], [FailureModeIdentifier],
     [OccurrenceCount], [SourceAddress],
     [MalfunctionLamp], [RedStopLamp], [AmberWarningLamp], [ProtectWarningLamp],
     [FaultStateActive])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 190, 3,
     1, 0,
     1, 0, 0, 0,
     1);
```

### INSERT: Q_ObdiiFaultRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_ObdiiFaultRecords"
    ("ThirdPartyId", "DateTime", "Code", "FaultStateActive")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 'P0301', true);

-- SQL Server
INSERT INTO [gda].[Q_ObdiiFaultRecords]
    ([ThirdPartyId], [DateTime], [Code], [FaultStateActive])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', 'P0301', 1);
```

### INSERT: Q_VinRecords

```sql
-- PostgreSQL
INSERT INTO gda."Q_VinRecords"
    ("ThirdPartyId", "DateTime", "VehicleIdentificationNumber")
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', '1HGCM82633A004352');

-- SQL Server
INSERT INTO [gda].[Q_VinRecords]
    ([ThirdPartyId], [DateTime], [VehicleIdentificationNumber])
VALUES
    ('DEVICE-001', '2026-04-16 14:30:00', '1HGCM82633A004352');
```

### SELECT: Pending records by device

```sql
-- PostgreSQL
SELECT "id", "ThirdPartyId", "DateTime", "ProcessingStatus"
FROM gda."Q_GpsRecords"
WHERE "ThirdPartyId" = 'DEVICE-001' AND "ProcessingStatus" = 0
ORDER BY "DateTime";

-- SQL Server
SELECT [id], [ThirdPartyId], [DateTime], [ProcessingStatus]
FROM [gda].[Q_GpsRecords] WITH (NOLOCK)
WHERE [ThirdPartyId] = 'DEVICE-001' AND [ProcessingStatus] = 0
ORDER BY [DateTime];
```

### SELECT: Failed records with reasons

```sql
-- PostgreSQL
SELECT f."ThirdPartyId", f."DateTime", f."FailureReason", f."RecordCreationTimeUtc"
FROM gda."Q_GpsRecordsFail" f
ORDER BY f."RecordCreationTimeUtc" DESC
LIMIT 50;

-- SQL Server
SELECT TOP 50 f.[ThirdPartyId], f.[DateTime], f.[FailureReason], f.[RecordCreationTimeUtc]
FROM [gda].[Q_GpsRecordsFail] f WITH (NOLOCK)
ORDER BY f.[RecordCreationTimeUtc] DESC;
```

### SELECT: Processing throughput

```sql
-- PostgreSQL
SELECT "ServiceId", "SuccessCount", "FailureCount", "LastBatchSize",
       "EntitiesLastProcessedUtc", "RecordLastChangedUtc"
FROM gda."OServiceTracking"
ORDER BY "ServiceId";

-- SQL Server
SELECT [ServiceId], [SuccessCount], [FailureCount], [LastBatchSize],
       [EntitiesLastProcessedUtc], [RecordLastChangedUtc]
FROM [gda].[OServiceTracking] WITH (NOLOCK)
ORDER BY [ServiceId];
```

### SELECT: Invalid records by type

```sql
-- PostgreSQL
SELECT "RecordType", COUNT(*) AS "Count"
FROM gda."DIGInvalidRecords"
GROUP BY "RecordType"
ORDER BY "Count" DESC;

-- SQL Server
SELECT [RecordType], COUNT(*) AS [Count]
FROM [gda].[DIGInvalidRecords] WITH (NOLOCK)
GROUP BY [RecordType]
ORDER BY [Count] DESC;
```

### SELECT: Record counts by processing status

```sql
-- PostgreSQL
SELECT "ProcessingStatus", COUNT(*) AS "Count"
FROM gda."Q_GpsRecords"
GROUP BY "ProcessingStatus";

-- SQL Server
SELECT [ProcessingStatus], COUNT(*) AS [Count]
FROM [gda].[Q_GpsRecords] WITH (NOLOCK)
GROUP BY [ProcessingStatus];
```

### UPDATE: Reset stale provisioning claims

```sql
-- PostgreSQL
UPDATE gda."Q_ProvisionDevices"
SET "ProcessingStatus" = 0, "ProcessingStartTimeUtc" = NULL
WHERE "ProcessingStatus" = 1
  AND "ProcessingStartTimeUtc" < (now() AT TIME ZONE 'UTC') - INTERVAL '30 minutes';

-- SQL Server
UPDATE [gda].[Q_ProvisionDevices]
SET [ProcessingStatus] = 0, [ProcessingStartTimeUtc] = NULL
WHERE [ProcessingStatus] = 1
  AND [ProcessingStartTimeUtc] < DATEADD(MINUTE, -30, GETUTCDATE());
```

### UPDATE: Reset invalid records cursor

```sql
-- PostgreSQL
UPDATE gda."DIGInvalidRecordsCursor"
SET "NextResultKey" = 0, "LastUpdatedUtc" = now() AT TIME ZONE 'UTC'
WHERE "id" = 1;

-- SQL Server
UPDATE [gda].[DIGInvalidRecordsCursor]
SET [NextResultKey] = 0, [LastUpdatedUtc] = GETUTCDATE()
WHERE [id] = 1;
```
