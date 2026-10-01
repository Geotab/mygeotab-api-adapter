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
| [`DigPartitionInfo`](#digpartitioninfo) | System | Partitioning settings pinned by the first maintenance run |
| [`DigMaintenanceLogs`](#digmaintenancelogs) | System | One row per database maintenance run |
| [`ProvisionedDevices`](#provisioneddevices) | Provisioning | Successfully provisioned devices |
| [`Q_ProvisionDevices`](#q_provisiondevices) | Provisioning Queue | Device provisioning requests |
| [`Q_ProvisionDevicesFail`](#common-fail-table-columns) | Provisioning Fail | Failed provisioning attempts **(partitioned)** |
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
| [`Q_*RecordsFail`](#common-fail-table-columns) | Telemetry Fail | One fail table per queue table (11 total) **(partitioned)** |
| [`DIGInvalidRecords`](#diginvalidrecords) | Invalid Records | Records rejected by the DIG API |
| [`DIGInvalidRecordsCursor`](#diginvalidrecordscursor) | Invalid Records | Pagination cursor (single-row table) |
| [`DeviceSubscriptions`](#devicesubscriptions) | Subscription | Device subscription state (active and terminated) |
| [`Q_DeviceSubscriptions`](#q_devicesubscriptions) | Subscription Queue | Subscribe and Terminate instructions |
| [`Q_DeviceSubscriptionsFail`](#q_devicesubscriptionsfail) | Subscription Fail | Rejected subscription instructions **(partitioned)** |
| [`stg_DeviceSubscriptions`](#stg_devicesubscriptions) | Subscription Staging | Staging input to the subscription merge |
| [`DeviceSubscriptionHistory`](#devicesubscriptionhistory) | Subscription Audit | Append-only record of applied instructions |
| [`DeviceSubscriptionGuardState`](#devicesubscriptionguardstate) | Subscription | Termination limit state (single-row table) |

> **(partitioned)** marks the **13 fail tables**, which are range-partitioned on `RecordCreationTimeUtc` so that old failure records can be reclaimed a period at a time. This is transparent to anything that reads or writes them — including PostgreSQL `COPY … FROM STDIN` — but it changes their primary key to a composite of `RecordCreationTimeUtc` and `id`. The key column order differs by provider — see [Common Fail Table Columns](#common-fail-table-columns), which gives each and explains why.
>
> The **live `Q_*` queue tables are not partitioned and are never subject to retention.** They hold work in progress, not history.

### Sequences

| Sequence | Type | Purpose |
|----------|------|---------|
| `gda.SeqDeviceSubscriptionOrdinal` | bigint | Supplies the ordering key compared against `DeviceSubscriptions.LastAppliedOrdinal`. Values are monotonic but **not** gapless, and ordinal order is *draw* order rather than *commit* order. |

### Views

| View | Purpose |
|------|---------|
| [`vwStatsForLevel1DigMaintenance`](#vwstatsforlevel1digmaintenance) | The `gda` tables that may need Level 1 maintenance (statistics, and on PostgreSQL vacuuming) |
| [`vwStatsForLevel2DigMaintenance`](#vwstatsforlevel2digmaintenance) | The `gda` indexes that may need Level 2 maintenance (reorganizing, rebuilding or reindexing) |

Both are read by the adapter's `DatabaseMaintenanceService` and are safe to query by hand. Their columns differ by provider, because the two engines expose different statistics.

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

### SubscriptionStatus (`DeviceSubscriptions`)

| Value | Meaning | Description |
|-------|---------|-------------|
| `0` | Terminated | Subscription has ended. The row is **retained**, not deleted. |
| `1` | Active | Device is subscribed and eligible to send telemetry (default) |

### OperationType (subscription queue, fail, staging and history tables)

| Value | Meaning | Description |
|-------|---------|-------------|
| `0` | Terminate | End a subscription |
| `1` | Subscribe | Start or resume a subscription |

### Outcome (`DeviceSubscriptionHistory`)

| Value | Meaning | Description |
|-------|---------|-------------|
| `0` | Applied | The instruction changed the subscription row |
| `1` | NoOp | A valid Terminate that changed nothing — the subscription was already Terminated, or no row for the device exists. Only a Terminate produces this: a Subscribe against an already-Active row still refreshes it and is recorded as Applied. Its ordinal still counts: a Subscribe with a lower ordinal, merged later, is Superseded if it names the same identifier — or, when the Terminate named only a serial number, the same serial number |
| `2` | Superseded | Something later had already won. Either a higher-ordinal instruction for the same identifier or serial arrived in the same batch, or - for a replayed or out-of-order instruction - the ordinal was at or below the one already applied to that subscription. Recorded for audit only |
| `3` | Rejected | The instruction failed validation and was written to the fail table |

### Merge Mode (`spMerge_stg_DeviceSubscriptions`)

| Value | Meaning | Description |
|-------|---------|-------------|
| `0` | Delta | Rows are individual instructions; rows absent from the batch are left untouched |
| `1` | Snapshot | The batch is the complete subscribed set; every active row absent from it is terminated |

### Merge ResultCode (`spMerge_stg_DeviceSubscriptions`)

The merge returns one row per rejected staged row, plus a final summary row, discriminated by `ResultCode`.

| Value | Meaning | Description |
|-------|---------|-------------|
| `0` | Completed | Summary row; `ResultMessage` carries the counts |
| `1` | RowRejected | The caller writes this row to `Q_DeviceSubscriptionsFail` |
| `2` | BrakeWouldTrip | Nothing was applied; the caller persists the guard state |
| `3` | EmptySnapshotRefused | Nothing was applied |
| `4` | GuardAlreadyTripped | Nothing was applied |

### MaintenanceTypeId (`DigMaintenanceLogs`)

| Value | Meaning | Description |
|-------|---------|-------------|
| `1` | Level1 | Level 1 maintenance: statistics, and on PostgreSQL vacuuming |
| `2` | Level2 | Level 2 maintenance: index reorganizing, rebuilding or reindexing |
| `3` | Partition | Partition management (`spManageDigPartitions`) |
| `4` | Retention | Partition retirement (`spRetireDigPartitions`), or the `DIGInvalidRecords` sweep |

Values `1` to `3` mean what they mean in the MyGeotab API Adapter's `DBMaintenanceLogs2`. `4` exists only here, because only this adapter performs retention.

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
| `Q_*.ThirdPartyId` | `DeviceSubscriptions.ThirdPartyId` **or** `ProvisionedDevices.ThirdPartyId` | The device must be transmittable before telemetry is accepted. Which table authorises it depends on the workflow; the adapter resolves both through one composed lookup and there is no foreign key either way |
| `Q_*Fail.OriginalQueueId` | `Q_*.id` | Links fail record to original queue record |
| `Q_*Fail.ThirdPartyId` | `DeviceSubscriptions.ThirdPartyId` **or** `ProvisionedDevices.ThirdPartyId` | Same device reference as the original queue record |
| `DIGInvalidRecords.SerialNo` | `DeviceSubscriptions.GeotabSerialNumber` **or** `ProvisionedDevices.GeotabSerialNumber` | Geotab serial of the device that produced the invalid record |
| `Q_DeviceSubscriptions.ThirdPartyId` | `DeviceSubscriptions.ThirdPartyId` | Instruction target; the merge upserts on this column |
| `Q_DeviceSubscriptionsFail.OriginalQueueId` | `Q_DeviceSubscriptions.id` | Links the fail record to the original queue record. **Nullable** — a rejection that never had a queue row leaves it `NULL`. |
| `stg_DeviceSubscriptions.OriginalQueueId` | `Q_DeviceSubscriptions.id` | Carries queue identity through staging so a rejection can be attributed. **Nullable** for the same reason. |
| `DeviceSubscriptionHistory.ThirdPartyId` | `DeviceSubscriptions.ThirdPartyId` | Device the recorded instruction targeted |

---

## Table Definitions

In the tables below, the **MSSQL / PG Type** column shows SQL Server type first, then PostgreSQL type. Where both use the same type, it is listed once. The **Default** column shows the default value applied on INSERT if the column is omitted.

### MiddlewareVersionInfo

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `DatabaseVersion` | nvarchar(50) / varchar(50) | NO | — | Schema version (e.g., `5.2.0.0`) |
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

**Indexes:**
- `IX_OServiceTracking_ServiceId` — Unique index on `ServiceId`. One row per service, enforced. `ServiceTracker` creates the missing rows at startup by reading the table and inserting what it does not find, and the loop covers every service whether or not this host enables it — so without this index two instances starting against the same database both insert the full set, and every startup after that fails while building the object cache, on both hosts, until the extra rows are removed by hand.

[↑ Table Inventory](#table-inventory)

### DigPartitionInfo

Records the partitioning settings chosen by the **first** maintenance run, so that every later run can be validated against them. Written once by `gda.spManageDigPartitions` and thereafter only read. Nothing external should modify it.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `InitialMinDateTimeUtc` | datetime2 / timestamp | NO | — | The date partitions were first created from — the oldest record present in the fail tables at that moment, or the current date if they were empty |
| `InitialPartitionInterval` | nvarchar(50) / varchar(50) | NO | — | `monthly`, `weekly` or `daily` |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | — | When the settings were pinned. Unlike the other tables, this column has **no** database default — `spManageDigPartitions` supplies the value on the single insert it makes |

> **Both recorded values are effectively permanent.** `spManageDigPartitions` refuses a call whose partition interval differs from `InitialPartitionInterval`, or whose minimum date precedes `InitialMinDateTimeUtc` — the existing partitions were already cut on the recorded values, so honouring a new one would leave the table divided two ways at once. Changing either genuinely requires rebuilding the partitioned tables.

[↑ Table Inventory](#table-inventory)

### DigMaintenanceLogs

One row per database maintenance run, written by the adapter's `DatabaseMaintenanceService`. The service reads back the latest Level 1 and Level 2 rows to decide when each is next due, so these rows are what space those runs out; they are also the only record in the database of when maintenance ran, and whether it succeeded.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `MaintenanceTypeId` | smallint | NO | — | What ran — see [MaintenanceTypeId](#maintenancetypeid-digmaintenancelogs) |
| `StartTimeUtc` | datetime2 / timestamp | NO | — | When the run started |
| `EndTimeUtc` | datetime2 / timestamp | YES | — | When the run ended |
| `Success` | bit / boolean | YES | — | Whether it completed. `false` records a run that failed, a Level 2 run skipped on PostgreSQL because `pgstattuple` was unusable, or a Level 2 run that could not start because the other services did not pause |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | — | When the row was written |

**Indexes:**
- `IX_DigMaintenanceLogs_MaintenanceTypeId_StartTimeUtc` — serves the one query the adapter issues against this table: the latest row of a given type.
- `IX_DigMaintenanceLogs_RecordLastChangedUtc` — on `RecordLastChangedUtc`.

> **Retention writes two rows per run**, one for partition retirement, which runs while the other services are paused, and one for the `DIGInvalidRecords` sweep, which runs after they resume. The rows are small and few — a few dozen a day — and the table is never pruned.
>
> **Clearing this table is harmless.** Level 1 and Level 2 are then treated as never having run, and run on the next iteration. Partition management and retention keep their own schedule, held in memory, which a restart of the adapter resets. `PG_ClearDatabaseScript.sql` and `MSSQL_ClearDatabaseScript.sql` clear it.

[↑ Table Inventory](#table-inventory)

### ProvisionedDevices

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `ThirdPartyId` | varchar(50) | NO | — | Your device identifier (**unique index**) |
| `ErpNo` | varchar(50) | YES | — | Account ERP number the device is activated against — the MyAdmin account that will own it. Used only by the device provisioning workflow. Optional; sent to MyAdmin only when supplied |
| `GeotabSerialNumber` | varchar(50) | NO | — | Geotab-assigned serial number |
| `IsOkayToSendDataToGeotab` | bit / boolean | NO | — | `true` when device is ready for telemetry |
| `DeviceProvisionedDateTimeUtc` | datetime2 / timestamp | YES | — | When the device was provisioned |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | — | Last update timestamp |

**Indexes:**
- `IX_ProvisionedDevices_ThirdPartyId` (PG) / `IX_ProvisionedDevices_ThirdPartyId_Includes` (MSSQL) — Unique index on `ThirdPartyId`
- `IX_ProvisionedDevices_PendingDIGReadiness` — Filtered index on `RecordLastChangedUtc` where `IsOkayToSendDataToGeotab` is false. Serves `DeviceDIGReadinessService`, which asks for the next batch of devices still awaiting a readiness check every ten seconds, ordered by `RecordLastChangedUtc` so that every device is checked before any is rechecked. Without it that query reads and sorts the whole table each time, at a cost that grows with the fleet. It is filtered because the rows it needs to find are the ones that have *not* yet been confirmed ready — so on a fleet where every device is ready the index holds nothing at all, which is also the case in which the unindexed query was slowest: with nothing to find there is no early exit, and every row is read to conclude that none qualify.

[↑ Table Inventory](#table-inventory)

### Q_ProvisionDevices

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `ThirdPartyId` | varchar(50) | NO | — | Your unique device identifier |
| `ErpNo` | varchar(50) | YES | — | Account ERP number the device is activated against — the MyAdmin account that will own it. Used only by the device provisioning workflow. Optional; sent to MyAdmin only when supplied |
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
- `IX_Q_ProvisionDevices_RecordCreationTimeUtc` — Index on `RecordCreationTimeUtc`, the column the claim procedure orders by
- `IX_Q_ProvisionDevices_Exhausted` — Filtered index on `id` where `RetryCount > 0`, for the exhausted-row sweep the claim procedure runs first

See [Why every queue table carries three indexes](#why-every-queue-table-carries-three-indexes).

[↑ Table Inventory](#table-inventory)

### Common Queue Table Columns

All 11 telemetry queue tables (`Q_GpsRecords`, `Q_AccelerationRecords`, etc.) share these columns:

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `ThirdPartyId` | varchar(50) | NO | — | Device identifier. Must be transmittable: an Active row in `DeviceSubscriptions`, or a row in `ProvisionedDevices` with `IsOkayToSendDataToGeotab = true`, depending on which workflow the deployment runs |
| `DateTime` | datetime2 / timestamp | NO | — | When the data was captured by the device (UTC) |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |
| `ProcessingStatus` | tinyint / smallint | NO | `0` | `0` = pending, `1` = in progress |
| `ProcessingStartTimeUtc` | datetime2 / timestamp | YES | — | Set when claimed for processing |
| `RetryCount` | tinyint / smallint | NO | `0` | Incremented on stale reclaim |

Each queue table also has three indexes — `IX_Q_<Name>_PendingWork` on `id` filtered to `ProcessingStatus = 0`, `IX_Q_<Name>_RecordCreationTimeUtc` on `RecordCreationTimeUtc`, and `IX_Q_<Name>_Exhausted` on `id` filtered to `RetryCount > 0`. See [Why every queue table carries three indexes](#why-every-queue-table-carries-three-indexes).

[↑ Table Inventory](#table-inventory)

### Why every queue table carries three indexes

All 13 queue tables carry the same three, because all 13 are read by the same two statements — the claim, and the exhausted-row sweep that runs immediately before it.

| Index | Key | Filtered to | Serves |
|-------|-----|-------------|--------|
| `IX_Q_<Name>_PendingWork` | `id` (`OrderingOrdinal` on `Q_DeviceSubscriptions`) | `ProcessingStatus = 0` | Locating available work |
| `IX_Q_<Name>_RecordCreationTimeUtc` | `RecordCreationTimeUtc` (`OrderingOrdinal` on `Q_DeviceSubscriptions`) | — | The claim's `ORDER BY` |
| `IX_Q_<Name>_Exhausted` | `id` | `RetryCount > 0` | The exhausted-row sweep |

**The `PendingWork` index cannot serve either of the other two**, which is why they exist. It is filtered to `ProcessingStatus = 0`, but the claim and the sweep both also take rows whose claim has gone **stale** — and those sit outside the filter, so the `OR` in both predicates rules the index out.

Two consequences follow, and they are the reason these are not optional:

- Without `_RecordCreationTimeUtc` the claim **sorts the entire queue** to take one batch, so its cost grows with queue depth. It is therefore most expensive exactly when the queue has backed up and throughput matters most.
- Without `_Exhausted` the sweep reads every row on every claim, and its **worst case is the healthy one**: when nothing has exhausted its retries the row limit can never be filled, so there is no early exit.

Measured on a 6,000,000-row queue, the pair took a 5,000-row claim from 5,953 ms to 90 ms on PostgreSQL and from 5,073 ms to 464 ms on SQL Server. **Neither index alone is sufficient on either provider.** At that depth `_RecordCreationTimeUtc` occupies about 40 MB on PostgreSQL and about 128 MB on SQL Server, while `_Exhausted` holds only rows that a device-lookup hold or a stale reclaim has touched — few or none in the normal state. The write cost is about 4.3% on the insert path, which retains roughly 25x headroom over what the queue is fed.

`_Exhausted` is filtered to `RetryCount > 0` rather than to the claim ceiling the sweep actually compares against. That is a deliberate **superset**: the configured ceiling is never below 1, so no qualifying row is missed, and the index does not have to be rebuilt if the ceiling's floor ever moves.

The sweep compares `RetryCount` with a literal `> 0` as well as with its ceiling parameter. That changes no result, being implied by the ceiling's floor, but it is what lets a cached plan use this index: SQL Server can never match a filtered index from a comparison with a procedure parameter alone, and a PostgreSQL generic plan cannot prove the implication either. Without the literal the SQL Server sweep reads the whole queue on every claim despite the index — the 464 ms figure above was measured that way — and with it a claim on a steady 1,000,000-row queue takes 324 ms rather than 495 ms. The SQL Server sweep also skips rows another session has locked (`READPAST`), as the claim does: once it seeks this index it looks each row up in the table, and without `READPAST` that lookup could wait on, or deadlock with, a locked row.

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
| `IsAuxiliary1On` | bit / boolean | YES | Auxiliary input 1 state |
| `IsAuxiliary2On` | bit / boolean | YES | Auxiliary input 2 state |
| `IsAuxiliary3On` | bit / boolean | YES | Auxiliary input 3 state |
| `IsAuxiliary4On` | bit / boolean | YES | Auxiliary input 4 state |
| `IsAuxiliary5On` | bit / boolean | YES | Auxiliary input 5 state |
| `IsAuxiliary6On` | bit / boolean | YES | Auxiliary input 6 state |
| `IsAuxiliary7On` | bit / boolean | YES | Auxiliary input 7 state |
| `IsAuxiliary8On` | bit / boolean | YES | Auxiliary input 8 state |

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

Each telemetry queue table has a corresponding fail table (`Q_*RecordsFail`). Those eleven tables share the columns below, in addition to the record-specific columns from the corresponding queue table. `Q_ProvisionDevicesFail` and `Q_DeviceSubscriptionsFail` follow the same shape but have **no `DateTime` column** — a provisioning or subscription instruction carries no capture timestamp. `Q_DeviceSubscriptionsFail` has its own section [below](#q_devicesubscriptionsfail); `Q_ProvisionDevicesFail` adds the `Q_ProvisionDevices` columns to the set below and has no section of its own.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Part of the composite primary key — see below |
| `OriginalQueueId` | bigint | NO | — | Reference to original `Q_*.id` |
| `ThirdPartyId` | varchar(50) | NO | — | Device identifier |
| `DateTime` | datetime2 / timestamp | NO | — | Original record timestamp |
| *(record-specific columns)* | — | — | — | Copied from original queue record |
| `OriginalRecordLastChangedUtc` | datetime2 / timestamp | NO | — | `RecordLastChangedUtc` from original queue record |
| `FailureReason` | nvarchar(max) / text | NO | — | Why the record failed |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert. Also the **partitioning key**. |

**Primary key:** composite, not `id` alone — `([RecordCreationTimeUtc], [id])` on SQL Server, `("id", "RecordCreationTimeUtc")` on PostgreSQL. Both engines require the partitioning column to participate in the primary key of a partitioned table. The column order differs deliberately: on SQL Server the primary key is the clustered index, which *is* the table, so leading with the partitioning column is what puts the rows on the partition scheme; PostgreSQL's index column order has no equivalent effect, so `id` stays first. `id` is still generated from a per-table identity/sequence and remains unique in practice, but it is no longer the sole declared key.

**Indexes:** Each fail table has `IX_Q_<Name>Fail_ThirdPartyId` on `ThirdPartyId`, aligned to the partition scheme.

**Partitioning:** All 13 fail tables are **range-partitioned on `RecordCreationTimeUtc`** (`RANGE RIGHT` on SQL Server). Each partition covers one period — `monthly` by default, optionally `weekly` or `daily` — and every table also has a catch-all for periods no other partition covers, so an insert can never fail for want of a partition. On PostgreSQL that is an explicit `DEFAULT` partition. On SQL Server it is the pair of open-ended partitions `RANGE RIGHT` leaves at **either** end — everything below the lowest boundary falls in the first, everything at or above the highest falls in the last. The partition function is created with a floor boundary of `2000-01-01`, so a merely back-dated record lands in an ordinary bounded partition and is retired by period like any other; only a record predating 2000 reaches the lower open-ended one.

Partitions are created and retired by `gda.spManageDigPartitions` and `gda.spRetireDigPartitions`, which the adapter's `DatabaseMaintenanceService` calls — see [Stored Procedures](#stored-procedures). Nothing external needs to manage them, and nothing about writing to these tables changes: `INSERT` and PostgreSQL `COPY … FROM STDIN` both target the table by name exactly as before.

> [!WARNING]
> **Retirement deletes data.** With the adapter's default configuration, partitions whose entire period precedes a 90-day cutoff are dropped permanently. Because a whole period must precede the cutoff, records can survive up to one partition interval beyond the nominal window.

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
- `IX_DIGInvalidRecords_RecordCreationTimeUtc` — Supports retention; without it each retention batch would scan the whole table it is trying to shrink

**Not partitioned, deliberately.** `IX_DIGInvalidRecords_GeotabGUID` is unique on `GeotabGUID` alone, and that uniqueness is what makes duplicate retrieval impossible. PostgreSQL requires the partitioning column to appear in every unique index on a partitioned table, so partitioning this table would mean widening that index and giving the guarantee up.

It is reclaimed by a **batched `DELETE`** on `RecordCreationTimeUtc` instead, which is affordable because the adapter sets this table's insert rate itself by polling the DIG API rather than being exposed to whatever an upstream integration sends. One consequence is worth knowing: the retention cutoff applies to **each record exactly** here, whereas on the partitioned fail tables it can only remove a whole period at a time.

[↑ Table Inventory](#table-inventory)

### DIGInvalidRecordsCursor

Single-row table (enforced by CHECK constraint: `id = 1`). Never DELETE from this table.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | int / integer | NO | `1` | Must always be `1` |
| `NextResultKey` | int / integer | NO | `0` | Cursor for next page of results |
| `LastUpdatedUtc` | datetime2 / timestamp | NO | Current UTC | When the cursor was last updated |

[↑ Table Inventory](#table-inventory)

### DeviceSubscriptions

Subscription state. Terminated devices are retained with `SubscriptionStatus = 0` rather than deleted, so a re-subscription is an update and the audit answer stays unambiguous.

Written **only** by `spMerge_stg_DeviceSubscriptions`. Do not INSERT or UPDATE this table directly.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `ThirdPartyId` | varchar(50) | NO | — | Device identifier (**unique index**) |
| `IdentifierType` | varchar(20) | NO | `'Vin'` | What kind of identifier `ThirdPartyId` holds |
| `GeotabSerialNumber` | varchar(50) | NO | — | Geotab-assigned serial number (**unique among active rows**) |
| `SubscriptionStatus` | tinyint / smallint | NO | `1` | `0` = terminated, `1` = active |
| `SubPlan` | varchar(50) | YES | — | Subscription plan |
| `LastAppliedOrdinal` | bigint | NO | `0` | High-water mark. The merge ignores any instruction whose ordinal is not greater than this. A Subscribe is also ignored when its ordinal is not greater than that of a Terminate already merged for the same `ThirdPartyId` — including a Terminate that changed nothing (history outcome NoOp), which raises no row's mark — or of a Terminate that cleared the same `GeotabSerialNumber` or named only that serial number. A NoOp Terminate that names a `ThirdPartyId` counts for that identifier alone. |
| `SubscribedDateTimeUtc` | datetime2 / timestamp | YES | — | When the current subscription began |
| `TerminatedDateTimeUtc` | datetime2 / timestamp | YES | — | When the subscription was terminated |
| `TerminationReason` | varchar(200) | YES | — | Reason supplied with the terminating instruction |
| `LastSeenInSnapshotUtc` | datetime2 / timestamp | YES | — | Last time the device appeared in a snapshot batch |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | Current UTC | Last update timestamp |

**Indexes:**
- `IX_DeviceSubscriptions_ThirdPartyId` — Unique index on `ThirdPartyId`. **Unfiltered on purpose:** it is the merge's conflict/join target, and filtering it would let a re-subscribe insert a second row for an identifier that already has a terminated one.
- `IX_DeviceSubscriptions_GeotabSerialNumber_Active` — Unique index on `GeotabSerialNumber` **filtered to `SubscriptionStatus = 1`**. Filtered on purpose: one identifier per serial number holds for the life of a subscription, but terminate-then-reprovision can legitimately re-pair a serial number to a different identifier. An unfiltered index would encode "one identifier per serial number forever".
- `IX_DeviceSubscriptions_Active` — Index on `SubscriptionStatus` including `ThirdPartyId` and `GeotabSerialNumber`

> **Collation (SQL Server):** `ThirdPartyId`, `IdentifierType` and `GeotabSerialNumber` are pinned to `SQL_Latin1_General_CP1_CS_AS` so identifier comparison is **case-sensitive and deployment-independent**. Identifiers are trimmed, never case-folded. Two identifiers differing only in case are therefore two different devices.

[↑ Table Inventory](#table-inventory)

### Q_DeviceSubscriptions

Subscription instruction queue. An external process inserts Subscribe and Terminate instructions; the adapter claims, stages and merges them.

Deliberately **not** unique on `ThirdPartyId` — a device can be subscribed, terminated and subscribed again.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `OrderingOrdinal` | bigint | NO | Next sequence value | Ordering key drawn from `gda.SeqDeviceSubscriptionOrdinal` |
| `OperationType` | tinyint / smallint | NO | — | `0` = Terminate, `1` = Subscribe; CHECK constraint permits no other value |
| `ThirdPartyId` | varchar(50) | YES | — | Device identifier |
| `IdentifierType` | varchar(20) | YES | — | What kind of identifier `ThirdPartyId` holds |
| `GeotabSerialNumber` | varchar(50) | YES | — | Geotab-assigned serial number |
| `SubPlan` | varchar(50) | YES | — | Subscription plan |
| `SourceStatus` | varchar(30) | YES | — | Status verbatim from the source system |
| `EffectiveDateTimeUtc` | datetime2 / timestamp | YES | — | When the instruction takes effect |
| `TerminationReason` | varchar(200) | YES | — | Reason, for Terminate instructions |
| `SourceReference` | varchar(500) | YES | — | Free-text provenance (e.g. the source file or message) |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | Current UTC | Auto-set on insert |
| `ProcessingStatus` | tinyint / smallint | NO | `0` | `0` = pending, `1` = in progress |
| `ProcessingStartTimeUtc` | datetime2 / timestamp | YES | — | Set when claimed for processing |
| `ClaimId` | uniqueidentifier / uuid | YES | — | Identifies the claiming batch, for traceability |
| `RetryCount` | tinyint / smallint | NO | `0` | Incremented on stale reclaim; capped by the claim procedure |

**Indexes:**
- `IX_Q_DeviceSubscriptions_PendingWork` — Filtered index on `OrderingOrdinal` where `ProcessingStatus = 0`. Keyed on `OrderingOrdinal` rather than `id` because that is what the claim procedure orders by.
- `IX_Q_DeviceSubscriptions_OrderingOrdinal` — Index on `OrderingOrdinal`, the column the claim procedure orders by. This is the one place the pair described in [Why every queue table carries three indexes](#why-every-queue-table-carries-three-indexes) is keyed on something other than `RecordCreationTimeUtc`, for that reason; an index on `RecordCreationTimeUtc` would serve nothing on this table.
- `IX_Q_DeviceSubscriptions_Exhausted` — Filtered index on `id` where `RetryCount > 0`, for the exhausted-row sweep the claim procedure runs first

> **SQL Server note:** this table carries filtered indexes, so ad-hoc DML against it requires `SET QUOTED_IDENTIFIER ON`. This is not specific to the subscription tables — see the note under [Example SQL Queries](#example-sql-queries).

[↑ Table Inventory](#table-inventory)

### Q_DeviceSubscriptionsFail

Rejected subscription instructions. Follows the `Q_ProvisionDevicesFail` shape: no processing columns, `RecordLastChangedUtc` renamed to `OriginalRecordLastChangedUtc`, and `RecordCreationTimeUtc` redefined to mean *when the failure was recorded*. Like every other fail table, it is range-partitioned on `RecordCreationTimeUtc` — see the [Table Inventory](#table-inventory) and [Common Fail Table Columns](#common-fail-table-columns).

Unlike `Q_DeviceSubscriptions` and `stg_DeviceSubscriptions`, `OperationType` here carries **no** check constraint, and the omission is deliberate. The table records what happened rather than asserting what should happen, and a constraint that rejected a row here would destroy the only record of the rejection. (An out-of-range `OperationType` cannot in fact arrive: both intake tables carry `CHECK (OperationType IN (0, 1))`, so such a row is refused at insert and never becomes an instruction at all. The omission still stands on its own reasoning.) `DeviceSubscriptionHistory` is unconstrained for the same reason: both record what happened, rather than asserting what should happen.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Part of the composite primary key with `RecordCreationTimeUtc`, because the table is partitioned |
| `OriginalQueueId` | bigint | YES | — | Reference to original `Q_DeviceSubscriptions.id` |
| `OriginalOrderingOrdinal` | bigint | YES | — | `OrderingOrdinal` from the original queue record |
| `OperationType` | tinyint / smallint | NO | — | `0` = Terminate, `1` = Subscribe |
| `ThirdPartyId` | varchar(50) | YES | — | Device identifier |
| `IdentifierType` | varchar(20) | YES | — | What kind of identifier `ThirdPartyId` holds |
| `GeotabSerialNumber` | varchar(50) | YES | — | Geotab-assigned serial number |
| `SubPlan` | varchar(50) | YES | — | Subscription plan |
| `SourceStatus` | varchar(30) | YES | — | Status verbatim from the source system |
| `EffectiveDateTimeUtc` | datetime2 / timestamp | YES | — | When the instruction would have taken effect |
| `TerminationReason` | varchar(200) | YES | — | Reason, for Terminate instructions |
| `SourceReference` | varchar(500) | YES | — | Free-text provenance |
| `OriginalRecordCreationTimeUtc` | datetime2 / timestamp | YES | — | `RecordCreationTimeUtc` from the original queue record |
| `OriginalRecordLastChangedUtc` | datetime2 / timestamp | YES | — | `RecordLastChangedUtc` from the original queue record |
| `FailureReason` | nvarchar(max) / text | NO | — | Why the instruction was rejected |
| `RecordCreationTimeUtc` | datetime2 / timestamp | NO | Current UTC | When the failure was recorded |

**Indexes:** `IX_Q_DeviceSubscriptionsFail_ThirdPartyId` on `ThirdPartyId`.

> The three `Original*` columns are nullable on purpose. A rejected row does not necessarily originate from a queue record, so there is not always a queue identity to record.

[↑ Table Inventory](#table-inventory)

### stg_DeviceSubscriptions

Staging input to `spMerge_stg_DeviceSubscriptions`. Every intake converges here so that no two paths can disagree on identical data.

Unlike the MyGeotab API Adapter's staging tables, this one can have more than one writer, so the merge scopes every operation to a `BatchId` and DELETEs only its own rows rather than TRUNCATEing the table.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `BatchId` | uniqueidentifier / uuid | NO | — | Scopes every operation the merge performs |
| `OperationType` | tinyint / smallint | NO | — | `0` = Terminate, `1` = Subscribe; CHECK constraint permits no other value |
| `Ordinal` | bigint | NO | — | Ordering key compared against `DeviceSubscriptions.LastAppliedOrdinal` |
| `OriginalQueueId` | bigint | YES | — | Source `Q_DeviceSubscriptions.id`, when the row came from the queue |
| `ThirdPartyId` | varchar(50) | YES | — | Device identifier |
| `IdentifierType` | varchar(20) | YES | — | What kind of identifier `ThirdPartyId` holds |
| `GeotabSerialNumber` | varchar(50) | YES | — | Geotab-assigned serial number |
| `SubPlan` | varchar(50) | YES | — | Subscription plan |
| `SourceStatus` | varchar(30) | YES | — | Status verbatim from the source system |
| `EffectiveDateTimeUtc` | datetime2 / timestamp | YES | — | When the instruction takes effect |
| `TerminationReason` | varchar(200) | YES | — | Reason, for Terminate instructions |
| `SourceReference` | varchar(500) | YES | — | Free-text provenance |
| `StagedDateTimeUtc` | datetime2 / timestamp | NO | Current UTC | When the row was staged. Lets the adapter sweep orphaned batches left by a crash between staging and merge. |

**Indexes:** `IX_stg_DeviceSubscriptions_BatchId` on `BatchId` and `IX_stg_DeviceSubscriptions_StagedDateTimeUtc` on `StagedDateTimeUtc`. Both exist on both providers.

[↑ Table Inventory](#table-inventory)

### DeviceSubscriptionHistory

Append-only audit. Written per **applied instruction**, not per state transition — a transition-only table would lose superseded instructions and plan changes, which are two of the things this table exists to record.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | bigint | NO | Auto-increment | Primary key |
| `ThirdPartyId` | varchar(50) | YES | — | Device identifier |
| `IdentifierType` | varchar(20) | YES | — | What kind of identifier `ThirdPartyId` holds |
| `GeotabSerialNumber` | varchar(50) | YES | — | Geotab-assigned serial number. For an applied Terminate, the serial number of the subscription it terminated, even when the instruction named only an identifier; otherwise as the instruction supplied it |
| `SubPlan` | varchar(50) | YES | — | Subscription plan |
| `SourceStatus` | varchar(30) | YES | — | Status verbatim from the source system |
| `OperationType` | tinyint / smallint | NO | — | `0` = Terminate, `1` = Subscribe |
| `Outcome` | tinyint / smallint | NO | — | `0` = Applied, `1` = NoOp, `2` = Superseded, `3` = Rejected |
| `PreviousStatus` | tinyint / smallint | YES | — | `SubscriptionStatus` before the instruction |
| `NewStatus` | tinyint / smallint | YES | — | `SubscriptionStatus` after the instruction |
| `Ordinal` | bigint | NO | — | Ordering key of the instruction |
| `EffectiveDateTimeUtc` | datetime2 / timestamp | YES | — | When the instruction took effect |
| `TerminationReason` | varchar(200) | YES | — | Reason, for Terminate instructions |
| `AppliedDateTimeUtc` | datetime2 / timestamp | NO | Current UTC | When the instruction was applied |
| `SourceReference` | varchar(500) | YES | — | Free-text provenance |

**Indexes:**
- `IX_DeviceSubscriptionHistory_ThirdPartyId` — Composite index on `ThirdPartyId`, `AppliedDateTimeUtc DESC`
- `IX_DeviceSubscriptionHistory_Terminations` — Index on `AppliedDateTimeUtc` filtered to `NewStatus = 0`, read by the termination limit's rolling window on every merge
- `IX_DeviceSubscriptionHistory_TerminateByIdentifier` — Composite index on `ThirdPartyId`, `Ordinal` filtered to applied and no-op Terminates (`OperationType = 0`, `Outcome` 0 or 1), read by the merge to find the newest Terminate already merged for a staged Subscribe's identifier
- `IX_DeviceSubscriptionHistory_TerminateBySerial` — The same on `GeotabSerialNumber`, `Ordinal`, for a staged Subscribe's serial number

[↑ Table Inventory](#table-inventory)

### DeviceSubscriptionGuardState

Single-row table (enforced by CHECK constraint: `id = 1`) holding the termination limit's state, following the `DIGInvalidRecordsCursor` precedent. Never DELETE from this table.

The termination limit counts terminations over a rolling window. The merge procedure reports a breach as result code `BrakeWouldTrip`, which is the same thing under its identifier name — the identifier keeps the older term, which the prose otherwise avoids. If a batch would push the count past the configured maximum, the merge applies **nothing** and reports the breach; the caller then sets `IsTripped`. The trip is **sticky** — it survives a restart and stays set until an operator clears it, because without persistence a restart would silently re-run the reconciliation the limit just refused.

| Column | MSSQL / PG Type | Nullable | Default | Description |
|--------|----------------|----------|---------|-------------|
| `id` | tinyint / smallint | NO | `1` | Must always be `1` |
| `IsTripped` | bit / boolean | NO | `false` | `true` once the termination limit has been tripped |
| `TrippedDateTimeUtc` | datetime2 / timestamp | YES | — | When the limit was tripped |
| `TrippedReason` | varchar(500) | YES | — | Why the limit was tripped |
| `TrippedTerminationCount` | int / integer | YES | — | Terminations the refused batch would have applied |
| `TrippedWindowCount` | int / integer | YES | — | Terminations already recorded in the rolling window |
| `RecordLastChangedUtc` | datetime2 / timestamp | NO | Current UTC | Last update timestamp |

**To clear a tripped termination limit**, after confirming the terminations were legitimate:

```sql
-- SQL Server:
UPDATE [gda].[DeviceSubscriptionGuardState]
SET [IsTripped] = 0, [TrippedDateTimeUtc] = NULL, [TrippedReason] = NULL,
    [TrippedTerminationCount] = NULL, [TrippedWindowCount] = NULL,
    [RecordLastChangedUtc] = SYSUTCDATETIME()
WHERE [id] = 1;

-- PostgreSQL:
UPDATE gda."DeviceSubscriptionGuardState"
SET "IsTripped" = false, "TrippedDateTimeUtc" = NULL, "TrippedReason" = NULL,
    "TrippedTerminationCount" = NULL, "TrippedWindowCount" = NULL,
    "RecordLastChangedUtc" = (now() AT TIME ZONE 'UTC')
WHERE "id" = 1;
```

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
| `gda.spClaimQDeviceSubscriptionsBatch` | `Q_DeviceSubscriptions` |

**Parameters:** every claim procedure above takes three — `@BatchSize INT`, `@StaleThresholdMinutes INT`, and the `RetryCount` ceiling at which a row is moved to that queue's fail table instead of being reclaimed forever. The ceiling is `@MaxClaimAttempts INT = NULL` on SQL Server and `p_max_claim_attempts integer` on PostgreSQL, which has **no default**, so a PostgreSQL caller must pass it explicitly. Passing `NULL` disables the cap on either provider. `@BatchSize` bounds the exhausted-row sweep as well as the claim itself, so both statements in these procedures are limited to the same number of rows per call.

`spClaimQDeviceSubscriptionsBatch` differs from the others in ordering by `OrderingOrdinal, id` rather than by `RecordCreationTimeUtc, id`. Both end in `id` because neither leading column is unique — creation timestamps do not distinguish rows inserted together — but only the subscription queue needs ordering by the sequence the instructions were **issued** in, because only it can carry both a Subscribe and a Terminate for the same device.

### spMerge_stg_DeviceSubscriptions

The **only** writer to `DeviceSubscriptions`. Merges a batch of staged rows, writes history, and clears that batch's staging rows.

| Parameter | MSSQL / PG Type | Default | Description |
|-----------|-----------------|---------|-------------|
| `@BatchId` / `p_batch_id` | uniqueidentifier / uuid | — | Scopes the merge to one staged batch |
| `@Mode` / `p_mode` | tinyint / smallint | — | `0` = Delta, `1` = Snapshot |
| `@SnapshotOrdinal` / `p_snapshot_ordinal` | bigint | `NULL` | Ordinal stamped on every row of a snapshot batch |
| `@ExpectedIdentifierTypes` / `p_expected_identifier_types` | varchar(500) / text | `'Vin'` | Comma-separated allow-list of accepted `IdentifierType` values. **Matched case-insensitively** — both sides are upper-cased before comparison, so `vin`, `Vin` and `VIN` are the same entry. **That is deliberate, and it is not an exception to the case-sensitive identifier rule:** what this compares is a *type token* supplied in configuration against the type label carried on the row, not a device identifier, so the `_CS_AS` collation pinned on the identifier columns has nothing to say about it. Writing `vin` here is a typography choice; writing a different `ThirdPartyId` casing is naming a different device. See the collation note under [`DeviceSubscriptions`](#devicesubscriptions) |
| `@MaxTerminationsPerWindow` / `p_max_terminations_per_window` | int / integer | `NULL` | Most terminations allowed within the window. `NULL` disables the limit. |
| `@TerminationWindowMinutes` / `p_termination_window_minutes` | int / integer | `60` | Rolling window the limit counts terminations over |

Returns one row per rejected staged row plus a final summary row — see [Merge ResultCode](#merge-resultcode-spmerge_stg_devicesubscriptions).

> **The procedure does not manage its own transaction.** The calling application owns the unit of work spanning stage → merge → queue-delete. This matches the convention used by the merge procedures in the MyGeotab API Adapter.

### spManageDigPartitions

Creates the period partitions the 13 fail tables need, covering everything from `@MinDateTimeUtc` up to and beyond the current period. Called by the adapter's `DatabaseMaintenanceService`; safe to run manually, and a no-op when every needed partition already exists.

| Parameter | MSSQL / PG Type | Default | Description |
|-----------|-----------------|---------|-------------|
| `@MinDateTimeUtc` / `"MinDateTimeUtc"` | datetime2(7) / timestamp | — | The date to create partitions from |
| `@PartitionInterval` / `"PartitionInterval"` | nvarchar(10) / text | — | `monthly`, `weekly` or `daily` |

On its first successful run it writes [`DigPartitionInfo`](#digpartitioninfo). Afterwards it **refuses** a different `@PartitionInterval`, or a `@MinDateTimeUtc` earlier than the recorded one, because existing partitions were cut on the recorded values.

Tables are discovered from the database catalog, so no table list is hardcoded and a record type added in a later release is covered without changing the procedure. A guard refuses any call that would create more than **2,000 periods for a single table**, which in practice means a mistyped `@MinDateTimeUtc` fails loudly instead of creating tens of thousands of partitions.

On PostgreSQL the first run also drains the `DEFAULT` partition: at install every existing row is in it, and PostgreSQL rejects a new period partition that would overlap rows sitting there, so the rows are moved out and the `DEFAULT` reattached within one transaction.

### spRetireDigPartitions

Removes partitions whose **entire** period precedes the cutoff. This is what reclaims space, and it is irreversible.

| Parameter | MSSQL / PG Type | Default | Description |
|-----------|-----------------|---------|-------------|
| `@OlderThan` / `"OlderThan"` | datetime2(7) / timestamp | — | The retention cutoff. `NULL`, or a value in the future, is refused. |
| `@Execute` / `"Execute"` | bit / boolean | `0` / `false` | `0` reports what would be retired and changes nothing; `1` performs the retirement. |

Returns one row per object acted on (`Object`, `Action`, `Detail`), in both modes — so a dry run and a real run produce the same shape of output.

> **`@Execute = 0` is a true dry run** and the safe way to see exactly what a retention window would remove before enabling automatic retention on a database whose history matters.

Because a partition is only retired once all of it precedes the cutoff, records can survive up to one partition interval beyond the nominal retention window. SQL Server additionally refuses to merge away the **last remaining boundary**, which would otherwise leave a single unbounded partition that could never be retired afterwards.

**The catch-all partition is retired by content rather than by period.** It has no range to compare against the cutoff, so on both providers it is emptied only when *every* row in it predates the cutoff, and left untouched otherwise — the records in it share one partition and go together. On PostgreSQL the `DEFAULT` partition is truncated in place rather than dropped, because the partitioning scheme requires it to exist; on SQL Server the leading partition is truncated and its boundary merged away like any other. Retiring it by content is the only route available, because a record that predates `InitialMinDateTimeUtc` can never be filed into a period afterwards: `spManageDigPartitions` refuses a `@MinDateTimeUtc` earlier than the recorded one.

### fnDigIndexSpaceStats (PostgreSQL only)

Returns `FreeSpace` and `TableLength` (both `bigint`, in bytes) for one index, measured by `public.pgstattuple()`. Takes the index's `oid`. Used by [`vwStatsForLevel2DigMaintenance`](#vwstatsforlevel2digmaintenance) and by nothing else.

The view calls this rather than `pgstattuple()` directly because this function resolves `pgstattuple()` only when it runs. That is what lets the schema be installed on a server where the `pgstattuple` extension is unavailable, lets the extension be installed later without re-creating the view, and stops `DROP EXTENSION pgstattuple` from dropping the view with it. It also reads each index once — as the MyGeotab API Adapter's equivalent view also does from its version 5.2.0.0, having read each index twice before.

---

## Views

Both views are scoped to the `gda` schema, on both providers. From version 5.2.0.0 the MyGeotab API Adapter's equivalents are scoped to its own schema on both providers too, so in a database the two adapters share, each maintains only its own indexes — see [Database Maintenance and Retention](README.md#37-database-maintenance-and-retention) in the README.

**Their thresholds are also constants in the adapter, and the two must agree.** A view decides which rows are offered and the adapter decides which of those it acts on, so a disagreement between them does not fail: it silently maintains nothing. The units differ by provider: **SQL Server's `Pct…` columns are percentages (0–100); PostgreSQL's `Pct…` and `…Ratio` columns are ratios (0–1).** The column names follow the MyGeotab API Adapter's, which is why a PostgreSQL column named `Pct…` holds a ratio.

### vwStatsForLevel1DigMaintenance

**SQL Server** — tables with more than 1,000 modifications since their statistics were last updated. The adapter runs `UPDATE STATISTICS` on those whose `PctModsSinceLastStatsUpdate` exceeds 10.

| Column | Type | Description |
|--------|------|-------------|
| `RowId` | bigint | Row number, in descending order of modifications |
| `SchemaName`, `TableName` | sysname | Always `gda` for `SchemaName` |
| `RecordCount` | bigint | Rows in the table, counted from its heap or clustered index |
| `UsedSpaceKB`, `ReservedSpaceKB`, `FreeSpaceKB` | bigint | Space across the table and all its indexes |
| `ModsSinceLastStatsUpdate` | bigint | The most modifications recorded against any of the table's statistics |
| `PctModsSinceLastStatsUpdate` | float | `ModsSinceLastStatsUpdate` as a **percentage** of `RecordCount` |
| `LastStatsUpdate` | datetime2 | The most recent statistics update |

**PostgreSQL** — tables with more dead tuples than 0.2 of their live tuples, more than 1,000 dead tuples, or more modifications since their last analyze than 0.1 of their live tuples. The adapter runs `VACUUM` and `ANALYZE` on the first two, and `ANALYZE` alone on the third.

| Column | Type | Description |
|--------|------|-------------|
| `RowId` | bigint | Row number |
| `SchemaName`, `TableName` | name | Always `gda` for `SchemaName`. Each partition of a partitioned table is listed as a table of its own |
| `LiveTuples`, `DeadTuples` | bigint | From `pg_stat_user_tables` |
| `PctDeadTuples` | numeric | Dead tuples as a **ratio** of live tuples; `NULL` for a table with no live tuples |
| `ModsSinceLastAnalyze` | bigint | Modifications since the table was last analyzed |
| `PctModsSinceLastAnalyze` | numeric | `ModsSinceLastAnalyze` as a **ratio** of live tuples; `NULL` for a table with no live tuples |

### vwStatsForLevel2DigMaintenance

**SQL Server** — index partitions more than 10 percent fragmented. The adapter rebuilds the whole index where `PctHighFragPartitions` exceeds 50; otherwise it rebuilds a partition more than 30 percent fragmented and reorganizes one more than 10 percent fragmented. A live `Q_*` queue's index is only ever reorganized, never rebuilt. Reading this view reads the pages above each `gda` index's leaf level (`sys.dm_db_index_physical_stats` in `LIMITED` mode, in-row data only) and reports each partition's leaf-level fragmentation - what `REORGANIZE` and `REBUILD` act on; it is scoped so that it reads **no other schema's** indexes, not merely so that it returns none.

| Column | Type | Description |
|--------|------|-------------|
| `RowId` | bigint | Row number, by table, index and partition |
| `SchemaName`, `TableName`, `IndexName` | sysname | Always `gda` for `SchemaName` |
| `PartitionNumber` | int | The index partition |
| `FragmentationPct` | float | Fragmentation of the partition, as a **percentage** |
| `IndexSizeKB` | bigint | Size of the partition |
| `TotalPartitions`, `HighFragPartitions` | int | Partitions of the whole index, and those more than 30 percent fragmented |
| `PctHighFragPartitions` | float | `HighFragPartitions` as a **percentage** of `TotalPartitions` |

**PostgreSQL** — every `gda` index, with no threshold of its own. The adapter runs `REINDEX` on those whose `IndexBloatRatio` exceeds 0.3 and whose `IndexSize` exceeds 1,000 bytes. Reading it reads every `gda` index in full, and **raises an error unless `pgstattuple` is installed and `geotabdigadapter_client` may execute it**; the adapter checks both first, and skips Level 2 with a warning when either is missing. An index on a partitioned table is not listed itself, but the index on each of its partitions is.

| Column | Type | Description |
|--------|------|-------------|
| `RowId` | bigint | Row number, largest index first |
| `SchemaName`, `TableName`, `IndexName` | name | Always `gda` for `SchemaName` |
| `IndexSizeText` | text | Size, formatted for reading |
| `IndexSize` | bigint | Size in bytes |
| `IndexBloatRatio` | double precision | Free space as a **ratio** of the index's size |

---

## Example SQL Queries

All examples show **PostgreSQL** syntax first, then **SQL Server** syntax. Columns with defaults (`id`, `RecordCreationTimeUtc`, `RecordLastChangedUtc`, `ProcessingStatus`, `RetryCount`) are omitted from INSERT statements — the database sets them automatically.

> [!IMPORTANT]
> **On SQL Server, run `SET QUOTED_IDENTIFIER ON;` first — for every example below, not just the subscription ones.** The `gda` schema has 31 filtered indexes: every queue table carries two of them (`IX_Q_<Name>_PendingWork` and `IX_Q_<Name>_Exhausted`), the device provisioning workflow's `ProvisionedDevices` carries one (`IX_ProvisionedDevices_PendingDIGReadiness`), and the subscription workflow's `DeviceSubscriptions` carries one (`IX_DeviceSubscriptions_GeotabSerialNumber_Active`) and `DeviceSubscriptionHistory` three (`IX_DeviceSubscriptionHistory_Terminations`, `IX_DeviceSubscriptionHistory_TerminateByIdentifier` and `IX_DeviceSubscriptionHistory_TerminateBySerial`). Ad-hoc DML against a table with a filtered index fails with **Msg 1934** unless the session has that option set. `sqlcmd` does not set it; SQL Server Management Studio and Azure Data Studio do, which is why the same statement can work in one tool and fail in another. Under `sqlcmd` the `SET` must also be in its own batch, before a `GO`.
>
> **Only the two subscription examples below repeat the `SET` inline.** That is to keep the other examples readable, not because they are exempt — every SQL Server example in this section needs it, including the `UPDATE`s at the end. Its absence further down is a formatting choice and not a signal.

### INSERT: Q_DeviceSubscriptions (Subscribe)

Makes a device transmittable in the device subscription workflow. `OperationType = 1` is Subscribe.
`OrderingOrdinal` is deliberately omitted so the column default draws it from
`gda.SeqDeviceSubscriptionOrdinal` — supplying it by hand can silently supersede later instructions.

```sql
-- PostgreSQL
INSERT INTO gda."Q_DeviceSubscriptions"
    ("OperationType", "ThirdPartyId", "IdentifierType", "GeotabSerialNumber", "SubPlan", "SourceReference")
VALUES
    (1, 'DEVICE-001', 'Vin', 'G9XXXXXXXXXX', NULL, 'Activation notice 2026-09-16');

-- SQL Server
SET QUOTED_IDENTIFIER ON;
GO

INSERT INTO [gda].[Q_DeviceSubscriptions]
    ([OperationType], [ThirdPartyId], [IdentifierType], [GeotabSerialNumber], [SubPlan], [SourceReference])
VALUES
    (1, 'DEVICE-001', 'Vin', 'G9XXXXXXXXXX', NULL, 'Activation notice 2026-09-16');
```

### INSERT: Q_DeviceSubscriptions (Terminate)

Stops transmission for a device. `OperationType = 0` is Terminate, and keying it by `GeotabSerialNumber`
alone is the safe form: the serial is the only identifier both directions of the workflow are guaranteed to
carry. The subscription row is retained with `SubscriptionStatus = 0`, not deleted.

```sql
-- PostgreSQL
INSERT INTO gda."Q_DeviceSubscriptions"
    ("OperationType", "GeotabSerialNumber", "TerminationReason", "SourceReference")
VALUES
    (0, 'G9XXXXXXXXXX', 'Customer cancelled', 'Termination notice 2026-09-16');

-- SQL Server
SET QUOTED_IDENTIFIER ON;
GO

INSERT INTO [gda].[Q_DeviceSubscriptions]
    ([OperationType], [GeotabSerialNumber], [TerminationReason], [SourceReference])
VALUES
    (0, 'G9XXXXXXXXXX', 'Customer cancelled', 'Termination notice 2026-09-16');
```

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
