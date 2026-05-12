# Change Log

This file tracks changes to the MyGeotab API Adapter solution over time, listed in reverse chronological order.

## Get Notified About New Releases

Any time a new release of the MyGeotab API Adapter is published to GitHub, an update will be posted to Geotab's [Integrator's Hub](https://community.geotab.com/s/integrators-hub?language=en_US). Click the **Join Group** button on the page to join and then choose the desired notification frequency (Every Post, Daily Digest, Weekly Digest, etc.)

## Feedback

Help us prioritize future efforts and better understand how the API Adapter is used. If you would like to provide any feedback about the MyGeotab API Adapter solution, please feel free to complete the 100% voluntary [MyGeotab API Adapter - Usage Survey](https://docs.google.com/forms/d/e/1FAIpQLSeIv-6A4Ugu7aIoyJdXrqVWyOF7sB8nuHOV-FDAYqayaPlkJg/viewform?usp=header).

---

## Version 5.0.0.1

- **NOTE:** There are **no database schema or configuration file changes from version 5.0.0 to version 5.0.0.1**. It is safe to upgrade from version 5.0.0 to version 5.0.0.1 by simply replacing the application files. See the [Upgrade Guide](docs/upgrade-guides/v5.0.0.1.md).
- Bug Fix: Resolved an issue with `DatabaseMaintenanceService2` whereby Level 2 database maintenance (index rebuild) on SQL Server fails for non-partitioned indexes (such as those on staging tables) with the error `Cannot specify partition number in the alter index statement as the index '...' is not partitioned.` The `RebuildMSSQLIndexPartitionAsync` method now checks `TotalPartitions` before generating partition-specific syntax, matching the existing pattern in `ReorganizeMSSQLIndexPartitionAsync`.
- Updated version to 5.0.0.1.

## Version 5.0.0

- **NOTE:** This build **includes changes to the `appsettings.json` file** (deprecated VSS/OVDS AddOns section removed) and **requires a database upgrade script to be run** (version alignment only — no schema changes).
  - To upgrade an existing installation from version 4.1.2 to version 5.0.0, see the [Upgrade Guide](docs/upgrade-guides/v5.0.0.md).
- **Added the Geotab DIG Adapter** to the solution. While the MyGeotab API Adapter pulls data *out of* MyGeotab, the Geotab DIG Adapter pushes telemetry data from custom (non-Geotab) telematics devices *into* MyGeotab via the [Data Intake Gateway (DIG)](https://github.com/Geotab/data-intake-gateway). See the [GeotabDIGAdapter README](../GeotabDIGAdapter/README.md) for details.
- **Updated the solution from .NET 9.0 to .NET 10.0 LTS.** If deploying the solution, there are no issues as the solution is self-contained. If copying/cloning the source code, it will be necessary to install .NET 10.0 SDK on the development machine.
- Added comprehensive [README.md](README.md), [SCHEMA_REFERENCE.md](SCHEMA_REFERENCE.md) and [CHANGELOG.md](CHANGELOG.md) documentation to the MyGeotabAPIAdapter project, intended to replace the original [Solution and Implementation Guide](https://docs.google.com/document/d/1Y_9FnHPldeX4_aPViUUOi_8y2UJU1lKcfb1SBnu-lj8/edit?usp=sharing) Google Doc going forward.
  - Makes it easier for those using LLMs (Large Language Models) or AI coding assistants. 
- Removed all remaining VSS/OVDS references from the solution. The `AddOns` configuration section was removed from `appsettings.json` and orphaned code references (enum values, interface properties, compiler suppressions) were cleaned up. The VSS/OVDS feature was deprecated in version 4.0.0.
- Updated NuGet packages to the latest stable release.
- Updated required adapter database version to 5.0.0.0.
- Updated version to 5.0.0.0.

## Version 4.1.2

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 4.1.1 to version 4.1.2, see [MyGeotab API Adapter — Upgrade Guide — from v4.1.1 to v4.1.2](https://docs.google.com/document/d/1APD5jekMSd-9GOO-WroEAjRnVr898VXx__o7imgQyxw/edit?usp=sharing)
- Bug Fix: Resolved an issue with DatabaseMaintenanceService2 whereby Level 2 database maintenance may fail in some PostgreSQL environments where the `geotabadapter_client` user lacks execute permissions on `pgstattuple`. Resolves [Issue #25](https://github.com/Geotab/mygeotab-api-adapter/issues/25).
- Updated version to 4.1.2.0.

## Version 4.1.1

- **NOTE:** There are **no database schema or configuration file changes from version 4.1.0 to version 4.1.1**. It is safe to upgrade from version 4.1.0 to version 4.1.1 by simply downloading the new version and overwriting the respective `appsettings.json` and `nlog.config` files with those that were configured for version 4.1.0.
  - To upgrade an existing installation from version 4.1.0 to version 4.1.1, see [MyGeotab API Adapter — Upgrade Guide — from v4.1.0 to v4.1.1](https://docs.google.com/document/d/1HbLBJU-ToeDhG_82Eal7rRrR9cOeIChIcSZUOXiLoE0/edit?usp=sharing)
- Bug Fix: Resolved an issue with DatabaseMaintenanceService2 whereby Level 1 and 2 database maintenance activities were not being triggered. Resolves [Issue #24](https://github.com/Geotab/mygeotab-api-adapter/issues/24).
- Updated version to 4.1.1.0.

## Version 4.1.0

- **NOTE:** This build **includes changes to the schema of the adapter database and to the appsettings.json file**.
  - To upgrade an existing installation from version 4.0.1.1 to version 4.1.0, see [MyGeotab API Adapter — Upgrade Guide — from v4.0.1.1 to v4.1.0](https://docs.google.com/document/d/1ISM97Rqvhw-Tv2bjNmOYifNmUkGEzMdZDv3pjsAKUR0/edit?usp=sharing)
- Added AuditLogs2 table with associated data feed.
- Modified Trips2 table:
  - Added **Odometer** and **EngineHours** columns.
- Modified Devices2 table:
  - Added **CustomProperties** column.
- Modified Users2 table:
  - Added **Designation** column.
- Bug Fix: Resolved an issue whereby records in the DiagnosticIds2 table were not restored if erroneously deleted by an external user/process, causing StatusData and FaultData records to be dropped instead of written to the adapter database.
  - Modified `spMerge_stg_Diagnostics2` stored procedure/function (SQL Server + PostgreSQL):
    - Added DiagnosticIds2 self-healing logic that independently restores missing DiagnosticIds2 rows for active Diagnostics2 rows, regardless of whether the MERGE operation detects changes.
  - Added two-phase diagnostic cache miss recovery to StatusDataProcessor2 and FaultDataProcessor2:
    - Phase 1: When a batch contains records referencing unknown diagnostics, the processors now defer those records, force a refresh of the in-memory DiagnosticIds2 cache from the adapter database, and retry the lookup before skipping.
    - Phase 2: Unknown diagnostic Ids are tracked and signaled to DiagnosticProcessor2, which triggers an out-of-cycle sync from the MyGeotab API to ensure newly-appearing diagnostics are captured promptly.
  - Improved log messages for diagnostic lookup failures to include the diagnostic Id value for easier troubleshooting.
- Updated required adapter database version to 4.1.0.0.
- Updated NuGet packages to the latest stable release.
  - Geotab.Checkmate.ObjectModel updated from version 11.109.349 to 11.118.421.
- Updated version to 4.1.0.0.

## Version 4.0.1.1

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 4.0.0 to version 4.0.1.1, see [MyGeotab API Adapter — Upgrade Guide — from v4.0.0 to v4.0.1.1](https://docs.google.com/document/d/1xtDNRXPZDlPv4ieuZWZtgBPoWBkESlwBAFJv5ja-ytU/edit?usp=sharing)
- Bug Fix: Modified GenericGeotabGUIDCacheableDbObjectCache2 class:
  - Added logic to prevent a duplicate-in-cache exception that will cause the application to crash.
- Bug Fix: Modified database partitioning procedure/function (spManagePartitions):
  - SQL Server version: Added check to throw error if filePath is invalid to prevent creation of a partition with no associated FileGroup.
  - PostgreSQL version: No changes other than DB version upgrade.
- Updated required adapter database version to 4.0.1.0.
- Updated NuGet packages to the latest stable release.
  - Geotab.Checkmate.ObjectModel updated from version 11.98.302 to 11.109.349.
- Updated version to 4.0.1.1.

## Version 4.0.1

- This release was immediately replaced with version 4.0.1.1 because the required database version was not correctly updated from 3.14.0.0 to 4.0.1.0. Ignore this release and go from version 4.0.0 directly to version 4.0.1.1.

## Version 4.0.0

- **NOTE:** This build **includes changes to the appsettings.json file**.
  - To upgrade an existing installation from version 3.14.0 to version 4.0.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.14.0 to v4.0.0](https://docs.google.com/document/d/1FAnjQ9NIL72b7Hy9QwxVGfDSPzwX5oTTpSSXz5SHKaY/edit?usp=sharing)
- **DEPRECATED THE ORIGINAL DATA MODEL.** Related changes include the following:
  - Removed the Data Optimizer. Its capabilities are included (and vastly improved) in the API Adapter's Data Enhancement Services.
  - Removed v1 database scripts.
  - Removed the VSS Add-On.
  - Removed Azure deployment scripts.
  - Modified the `appsettings.json` file:
    - Removed the `UseDataModel2` setting.
    - Removed the `DebugData` section.
  - Cleaned-up the solution by modifying code as needed and removing items no longer needed as a result of the above.
- Updated README.md file.
- Updated NuGet packages to the latest stable release.
  - Geotab.Checkmate.ObjectModel updated from version 11.83.265 to 11.98.302.
- Updated version to 4.0.0.0.

## Version 3.14.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.13.0 to version 3.14.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.13.0 to v3.14.0](https://docs.google.com/document/d/1UKYJYleso6MzVbLwlZ5W6nYT0-6vTQcjaHS0WusORhA/edit?usp=sharing)
- Bug Fix: Modified database partitioning procedure/function (spManagePartitions):
  - SQL Server version: Fixed issue that could cause monthly partitions to become daily.
  - PostgreSQL version: Changed function to ensure that daily/weekly/monthly partitions cover the entire next month.
- Updated version to 3.14.0.0.

## Version 3.13.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.12.0 to version 3.13.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.12.0 to v3.13.0](https://docs.google.com/document/d/1a2Atck0ljmX6Es8nFXKIbwwB7OSz9cWxEn5WT-9hYCw/edit?usp=sharing)
- Migrated DVIRLog Manipulator to DM2.
  - Added `upd_DVIRDefectUpdates2` and `fail_DVIRDefectUpdateFailures2` tables.
- Bug Fix: Modified the PostgreSQL version of the `spMerge_stg_Trips2` function to include the missing **Distance** column when updating the Trips2 table. See the [Re-Extract Trips Data if Necessary section](https://docs.google.com/document/d/1a2Atck0ljmX6Es8nFXKIbwwB7OSz9cWxEn5WT-9hYCw/edit?tab=t.0#heading=h.bond32v5s0ya) in the upgrade guide for more information.
- Bug Fix: Added logic to ensure that database partitioning happens, if needed, on application startup *before* any other services write data to the adapter database.
- Added a section to PostgreSQL scripts to change ownership of all tables to `geotabadapter_client` to capture any that were not set previously and avoid any possible related issues.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.13.0.0.

## Version 3.12.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.11.0 to version 3.12.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.11.0 to v3.12.0](https://docs.google.com/document/d/1FdZrWIPjhgnbBSixDeW8SBI695SLxMR0LccqNw-RCfQ/edit?usp=sharing)
- Added DutyStatusLogs2 table with associated data feed.
- Removed physical foreign key relationships associated with User, Device and Rule Ids to accommodate historic data and certain edge cases.
- Added handling for specific KnownIds including `NoDeviceId`, `NoDriverId`, `NoRuleId`, `NoUserId`, `NoZoneId` and `UnknownDriverId` via the introduction of sentinel records with placeholder values.
- Made the `ExceptionEvents2.RuleId` column nullable.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.12.0.0.

## Version 3.11.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.10.0 to version 3.11.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.10.0 to v3.11.0](https://docs.google.com/document/d/1egXnNLzc5XHkn1bcrulMJ0yHu-pvtkpy2bgSEIx0Nrc/edit?usp=sharing)
- Added ability to install the MyGeotab API Adapter application (DM2) as a service (in Windows or Linux).
- Updated `spMerge_stg_DVIRDefects2` and `spMerge_stg_DVIRLogs2` stored procedures / functions to avoid FK violations caused when records must be deleted and re-inserted to move between database partitions.
- Modified GeotabTripDbTripObjectMapper (DM1) to ignore Trips with null Device.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.11.0.0.

## Version 3.10.0

- **NOTE:** This build **includes changes to the schema of the adapter database and to the appsettings.json files**.
  - To upgrade an existing installation from version 3.9.0 to version 3.10.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.9.0 to v3.10.0](https://docs.google.com/document/d/1CGNg_UCBTSChUM4-J-9kYqFpEd__aRIxQNPJ7wYvfvU/edit?usp=sharing)
- Added FuelAndEnergyUsed2 table with associated data feed.
- Modified the FaultData2 table:
  - Added `EffectOnComponent`, `FaultDescription`, `FlashCodeId`, `FlashCodeName`, `Recommendation` and `RiskOfBreakdown` columns.
- Modified the `appsettings.json` file:
  - Added a section with settings for the new **FuelAndEnergyUsed** feed.
  - Added a `PopulateEffectOnComponentAndRecommendation` setting to the FaultData feed section.
- Updated NuGet packages to the latest stable release.
  - Geotab.Checkmate.ObjectModel updated from version 11.68.266 to 11.83.265.
- Updated version to 3.10.0.0.

## Version 3.9.0

- **NOTE:** This build **includes changes to the schema of the adapter database and to the appsettings.json files**.
  - To upgrade an existing installation from version 3.8.0 to version 3.9.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.8.0 to v3.9.0](https://docs.google.com/document/d/1s0W3Jq1MOiZoGOC_MOkAQCg9MENV__r02ZoL9iGJ7eQ/edit?usp=sharing)
- Added pseudo feed for DutyStatusAvailability along with associated DutyStatusAvailabilities2 table.
- Modified ExceptionEvent feed — setting `IncludeInvalidated`, `IncludeDismissedEvents` and `IncludeDeleted` all to true.
- Modified `Diagnostics2.DiagnosticName` column from `nvarchar(255)` to `nvarchar(max)` to accommodate new Diagnostics with long names.
- Modified the `appsettings.json` files:
  - Renamed `OverrideSetings` to `OverrideSettings` (corrected typo).
- Added macOS publish profile.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.9.0.0.

## Version 3.8.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.7.0 to version 3.8.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.7.0 to v3.8.0](https://docs.google.com/document/d/1fw0mS-kmI30vMrMzvv2uAKVvCAJSjzu4z6WoZm4OgOU/edit?usp=sharing)
- Modified Devices2 table:
  - Added `TmpTrailerGeotabId` and `TmpTrailerId` columns.
- Added DefectSeverities2 and RepairStatuses2 reference data tables.
- Added data feed for DVIRLogs along with associated DVIRLogs2, DVIRDefects2 and DVIRDefectRemarks2 tables.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.8.0.0.

## Version 3.7.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.6.0 to version 3.7.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.6.0 to v3.7.0](https://docs.google.com/document/u/0/d/1LfyVdD-JmJnL66PamOTHfdhtae-jrigNvzWu-r1A6LI/edit)
- Added DeviceStatusInfo2 table with associated data feed.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.7.0.0.

## Version 3.6.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.5.0 to version 3.6.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.5.0 to v3.6.0](https://docs.google.com/document/u/0/d/1tYT-l2ggv7x_MU1AzLL7EpJzjbUJpZa35yj5cRfrJ9Q/edit)
- Added DriverChanges2 table with associated data feed.
- Added handling for database foreign key violations caused by race conditions.
- Bug Fix: Updated `BackgroundServiceAwaiter.WaitForConnectivityRestorationIfNeededAsync` method to return bool indicating whether wait was needed.
- Enhanced GenericGeotabObjectFeeder:
  - Added `Rollback` method to simplify feed version rollback logic.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.6.0.0.

## Version 3.5.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.4.0 to version 3.5.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.4.0 to v3.5.0](https://docs.google.com/document/d/1rUvOuZNI1NhQMkGdHAevqynYZgvLWu7w6sBpLsGwVsQ/edit?usp=sharing)
- Added ChargeEvents2 table with associated data feed.
- Modified Rules2 table: Added **Condition** column populated with the hierarchical tree of Condition(s) defining the logic of a Rule in JSON form.
- Modified FaultDataLocationService2 and StatusDataLocationService2 to include Polly retry wrappers around transactionless bulk updates.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.5.0.0.

## Version 3.4.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.3.0 to version 3.4.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.3.0 to v3.4.0](https://docs.google.com/document/d/1x8CKSt8jgh6-HFlUbDNQvd90Y4eHnWHYKN8HsiLE9mo/edit?usp=sharing)
- Added BinaryData2 table with associated data feed.
- Updated NuGet packages to the latest stable release.
  - Geotab.Checkmate.ObjectModel updated from version 11.62.237 to 11.68.266.
- Updated version to 3.4.0.0.

## Version 3.3.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.2.0 to version 3.3.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.2.0 to v3.3.0](https://docs.google.com/document/d/18g7kfPNakibekgB9MBKiBdE2DV_SryNowy-lVa8KSbA/edit?usp=sharing)
- Added ExceptionEvents2 table with associated data feed.
- **Updated the solution from .NET 8.0 to .NET 9.0.** If deploying the solution, there are no issues as the solution is self-contained. If copying/cloning the source code, it will be necessary to install .NET 9.0 SDK on the development machine.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.3.0.0.

## Version 3.2.0

- **NOTE:** This build **includes changes to the schema of the adapter database**.
  - To upgrade an existing installation from version 3.1.0 to version 3.2.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.1.0 to v3.2.0](https://docs.google.com/document/u/0/d/1s6o-4lsLwzAB2F7R6qLMSTclLChBtC8izqI4cQnyvDI/edit)
- Added Rules2 table with associated data feed.
- Added Trips2 table with associated data feed.
- Added documentation, including query examples for both SQL Server and PostgreSQL.
- Modified Database Maintenance Service to pause other services before partitioning the database to resolve possible deadlock-related issues.
- Bug fix: Removed certain unique constraints on staging tables that can prove problematic (`PK_stg_Devices2`, `PK_stg_Users2`, `PK_stg_Zones2`, `UK_stg_Trips2_DeviceId_Start_EntityStatus`).
- Modified database cleanup scripts to exclude staging tables from count results.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.2.0.0.

## Version 3.1.0

- **NOTE:** This build **includes changes to the schema of the adapter database and appsettings.json file**.
  - To upgrade an existing installation from version 3.0.0 to version 3.1.0, see [MyGeotab API Adapter — Upgrade Guide — from v3.0.0 to v3.1.0](https://docs.google.com/document/u/0/d/1fl1t14XFBobSgwH8RxQiV0LDKMGkWZgNNdjsiNZPkCE/edit)
- Added Groups2 table with associated data feed and related group columns to the Devices2, Users2 and Zones2 tables.
- Enhanced cache update/refresh process for reference data types by migrating from in-memory application logic to utilizing staging tables with merge procedures.
  - Dramatically improves initial startup performance of the application and is especially noticeable with larger fleets.
  - Also resolves an issue that was encountered on rare occasions where duplicate records got into one of the reference data tables.
- Modified DutyStatusLog feed to include a DutyStatusLogSearch with the `IncludeModifications` option set to true.
- Bug fix: Added Polly `asyncRetryPolicyForDatabaseTransactions` wrapper around database calls using standalone connections.
- Bug fix: Updated `vwStatsForLevel1DBMaintenance` in PostgreSQL scripts to only select from the `public` schema.
- Updated NuGet packages to the latest stable release.
- Updated version to 3.1.0.0.

## Version 3.0.0

- **NOTE:** This build **includes changes to the schema of the adapter database and appsettings.json file**.

### Data Model 2 (DM2)

Version 3.0.0 represents the next evolution of the MyGeotab API Adapter solution. Key points are as follows:

- A new data model — Data Model 2 (DM2) — has been added:
  - DM2 is normalized and designed for greater performance and scalability.
  - Database is partitioned (monthly, weekly, or daily).
  - Includes Automated Database Maintenance.
  - Supports both SQL Server and PostgreSQL.
    - Starting with version 3.0.0, Oracle database will not be supported moving forward due to very low usage combined with a high cost to develop and maintain.
  - Data Optimizer deprecated — **location interpolation capabilities** moved directly into the adapter database with exponentially faster performance.
- MyGeotab API Adapter supports both the original data model and DM2:
  - Initial version 3.0.0 release of DM2 includes support for a subset of the Geotab entities currently supported with the original data model.
  - Additional entities will be ported over to DM2 in the coming months.
  - Once DM2 supports all of the original data model entities, the original data model will be deprecated.

### Other Updates

- Updated CleanDatabaseScripts to more efficiently clear data and use system tables/catalog to obtain approximate counts.
- Updated DatabaseResilienceHelper to use ExceptionHelper to include StackTrace in exception logs. Enhanced internal exception handling logic to include retry for PK constraint violation exceptions.
- Modified BaseRepository — added QueryAsync method for executing parameterized queries.
- Added DatabaseValidator to validate adapter database version on application startup (DM2 only).
- Modified BaseRepository and GenericEntityPersister to allow for the optional use of standalone database connections (with no transactions and outside of units of work).
- Added capability for BackgroundServices to pause for database maintenance (DM2 only).
- Added BackgroundServiceAwaiter to consolidate wait logic on behalf of individual BackgroundServices.
- Removed unnecessary trace method entry/exit logging.
- Modified DatabaseResilienceHelper — added retry for "current transaction aborted" exceptions.
- Modified `StringHelper.IsValidIdentifierForDatabaseObject` method to allow for dashes (as used in weekly partition names).
- Updated PrerequisiteServiceChecker and ServiceTracker classes with better logging logic for pauses.
- Modified GenericGeotabObjectCacher to use `AddOrUpdate` instead of `TryAdd`.

## Earlier Versions

For information relating to earlier versions of the MyGeotab API Adapter solution, refer to the [Change Log](https://docs.google.com/document/d/1t8AunsFvW7NZtXaQ_9Q85qi5dR1GTfVVTYIcwXoRG1E/edit?tab=t.0#heading=h.rgd7wj49j9nw) section in the original guide.
