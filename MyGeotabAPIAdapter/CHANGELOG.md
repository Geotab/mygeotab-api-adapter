# Change Log

This file tracks changes to the MyGeotab API Adapter solution over time, listed in reverse chronological order.

## Get Notified About New Releases

Any time a new release of the MyGeotab API Adapter is published to GitHub, an update will be posted to Geotab's [Integrator's Hub](https://community.geotab.com/s/integrators-hub?language=en_US). Click the **Join Group** button on the page to join and then choose the desired notification frequency (Every Post, Daily Digest, Weekly Digest, etc.)

## Feedback

Help us prioritize future efforts and better understand how the API Adapter is used. If you would like to provide any feedback about the MyGeotab API Adapter solution, please feel free to complete the 100% voluntary [MyGeotab API Adapter - Usage Survey](https://docs.google.com/forms/d/e/1FAIpQLSeIv-6A4Ugu7aIoyJdXrqVWyOF7sB8nuHOV-FDAYqayaPlkJg/viewform?usp=header).

---

## Version 5.2.0.0

- **NOTE:** This build **includes a change to the adapter database** — a stored function change and a view change on PostgreSQL, and a stored procedure change and view changes on SQL Server. There are **no table, column or index changes**, and **no `appsettings.json` changes**. A database upgrade script must still be run on **both** engines, because the required database version moves from 5.1.0.0 to 5.2.0.0. To upgrade an existing installation, see the [Upgrade Guide](docs/upgrade-guides/v5.2.0.0.md).
  - Most of this release's functional work was done in the Geotab DIG Adapter — see its [CHANGELOG](../GeotabDIGAdapter/CHANGELOG.md) for details. **The Geotab DIG Adapter's `gda` schema does change in this release**; in the MyGeotab API Adapter's own `dbo` / `public` schema only the partition procedure or function and the maintenance views described below change.
- **Bug Fix (PostgreSQL):** Scoped the `spManagePartitions` function to the `public` schema. It previously looped over **every** partitioned table in the database, regardless of schema, and then assumed the table lived in `public` — stripping that prefix by text replacement and re-applying it when generating each `CREATE TABLE ... PARTITION OF ...` statement. A partitioned table in any other schema therefore produced a malformed statement referring to a relation that does not exist, which raised an error and aborted the function **before any of the adapter's own partitions were created**. Because the adapter executes this function on every startup and periodically thereafter, and partition management cannot be disabled, the adapter was left unable to maintain its partitions at all.
  - This is reachable whenever the adapter database is shared with another component that partitions tables of its own — including the Geotab DIG Adapter, which uses the `gda` schema in the same database.
  - Two further lookups in the same function — the parent table's owner, and the check for whether a partition already exists — matched on name alone and are now also schema-qualified, so a like-named table or partition in another schema cannot select the wrong owner or cause a needed partition to be skipped.
  - SQL Server's `spManagePartitions` stored procedure does not enumerate tables and never had this failure. It had a different shared-database defect, fixed in the next entry.
- **Bug Fix (PostgreSQL):** **A single record dated after the adapter's last partition could stop partition maintenance for good.** Such a record — from a device whose clock runs ahead, say, or one that arrived after partition maintenance had been failing for longer than its partitions lasted — is stored in the table's DEFAULT partition, and PostgreSQL refuses to create a partition while the DEFAULT partition holds a record that belongs in it. So once the period that record belongs to came due, `spManagePartitions` failed on every run, the run at startup included, which stops the adapter; recovering took a manual detach, create, move and re-attach on the affected table.
  - `spManagePartitions` now sets such records aside, creates the partitions, and files the records into them, all in the one transaction. The DEFAULT partition is not detached to do this, because `DVIRDefects2` refers to `DVIRLogs2`, and `DVIRDefectRemarks2` to `DVIRDefects2`, through foreign keys, and PostgreSQL will not detach a partition holding a record that another table refers to. The records are set aside in a temporary table instead — a referring table's first, and filed back last — so no foreign key is violated at any point.
  - SQL Server never refused: a record past the last boundary is stored in the last, open-ended partition, and is moved when the next boundary is created.
- **Bug Fix (SQL Server):** **On a database shared with the Geotab DIG Adapter, `spManagePartitions` stopped creating the adapter's own partition boundaries — and reported success.** Its check for whether a month's boundary already existed looked in *every* partition function in the database, not only the adapter's own. The Geotab DIG Adapter partitions its tables on the same monthly boundaries from its version 5.2.0.0, and runs its partition maintenance daily, so it creates each month's boundary first; `spManagePartitions` then found that boundary, skipped its own for the month, and finished normally. Every new row of every partitioned table went into one ever-growing partition, the monthly filegroups stayed empty, and the partition retirement utility could no longer retire a month. The check now looks only in the adapter's own partition function.
  - `Scripts/SQLServer/v2/MSSQL_5.2.0.0_spManagePartitionsSchemaScoping.sql` changes that one statement in the procedure the database already holds, rather than re-creating the procedure, so the data file path set in its `@filePath` variable is kept. If the procedure has been customised so that the statement is not found exactly once, the script stops with an error before it updates the database version — see the upgrade guide.
  - A MyGeotab API Adapter left at 5.1.0.0 beside a Geotab DIG Adapter at 5.2.0.0 in one SQL Server database has this defect; upgrade both.
- **Bug Fix (PostgreSQL):** **A database deadlock is now retried, as it already was on SQL Server.** The retry rules recognised a deadlock only by SQL Server's wording ("chosen as the deadlock victim"). PostgreSQL reports one as `40P01: deadlock detected`, which matched no rule, so the operation that hit it failed instead of being retried.
- **Bug Fix (SQL Server):** **A failure to open a database connection is now retried on every path, as it already was on PostgreSQL.** On a standalone connection the SQL Server wrapper's message matched no retry rule, so such a failure was retried only when its inner message happened to contain "timeout" — and a database maintenance statement, which from this release does not retry timeouts, failed its run outright when a new connection took too long to open.
- **Bug Fix (Level 2 database maintenance, both engines):** **Level 2 database maintenance stopped before maintaining anything, for most of the day, in the configuration that ships — and recorded each such run as a success.** The end of the daily maintenance window was calculated and then applied as a hard stop on the maintenance run **regardless of whether the maintenance window feature was enabled**. `EnableLevel2DatabaseMaintenanceWindow` is `false` by default, and it governed only whether a run was allowed to *start*; the window's end time was passed to the run either way. With the default `Level2DatabaseMaintenanceWindowStartTimeUTC` of `06:00` and `Level2DatabaseMaintenanceWindowMaxMinutes` of `60`, that end time is `07:00` UTC, so **any Level 2 run beginning after 07:00 UTC** paused the other services, queried the index-statistics view, reported the number of indexes it had found, and then stopped at the first one without reorganizing or rebuilding any of them.
  - Each of those runs was logged as *"Level 2 database maintenance completed successfully"* and written to `DBMaintenanceLogs2` with `Success` set to true. Because the interval until the next attempt is measured from that record, a run that maintained nothing also reset the clock — so the adapter waited `Level2DatabaseMaintenanceIntervalMinutes` and repeated the same no-op, paying for the pause and for the index-statistics query each time. On SQL Server that query reads every page of every index in the database, making it the most expensive operation the service performs.
  - The window end time is now applied **only when the maintenance window is enabled**. With the window disabled, a Level 2 run processes every candidate index it found, which is what a disabled window was always meant to mean. Behaviour with the window *enabled* is unchanged.
- **Bug Fix (Level 2 database maintenance):** **`Level2DatabaseMaintenanceWindowStartTimeUTC` given with a time-zone offset moved the window by the host's own offset.** The value was converted to the host's local time before its time of day was taken, so on a host not running UTC a window configured as, say, `…T06:00:00+03:00` did not start at 03:00 UTC. It is now always read as UTC: an offset is honoured, and a value without one is taken to be UTC. A value ending in `Z`, as the shipped one does, behaves as before.
  - **Effect of upgrading:** on installations that left the maintenance window disabled, Level 2 maintenance begins doing work it has not been doing. The first run after the upgrade may therefore take considerably longer than previous runs appeared to, and it pauses the other services while it runs. If you would rather it ran at a chosen time, set `EnableLevel2DatabaseMaintenanceWindow` to `true` and set the window to suit — that combination behaved correctly before this fix and is unchanged by it.
- **Bug Fix (Level 1 and Level 2 database maintenance, SQL Server only):** **Two maintenance thresholds were being applied at one hundredth of their intended values**, because the views return those two figures as percentages while the thresholds they are compared against were written as ratios.
  - `vwStatsForLevel1DBMaintenance` returns `PctModsSinceLastStatsUpdate` on a 0–100 scale; the threshold was `0.1`, intended as 10% but acting as **0.1%**. Statistics were therefore updated on essentially every table the view returned, rather than on tables actually modified enough to warrant it.
  - `vwStatsForLevel2DBMaintenance` returns `PctHighFragPartitions` on a 0–100 scale; the threshold was `0.5`, intended as 50% but acting as **0.5%**. This is the more consequential of the two, because it selects the more expensive branch: an entire index is rebuilt, rather than only the fragmented partitions of it. `100 / partitions > 0.5` holds for any index of fewer than 200 partitions, so in practice **a single fragmented partition caused a whole-index rebuild**.
  - The thresholds are now `10` and `50`, expressed in the same unit as the columns they are compared against, and the constant naming and log messages now state the unit. **PostgreSQL was never affected** — its views return ratios and its thresholds were already ratios.
  - **Effect of upgrading:** on SQL Server, statistics updates and whole-index rebuilds both become markedly less frequent, and Level 2 maintenance shifts toward rebuilding or reorganizing individual partitions. This is the intended behaviour and should reduce the time Level 2 maintenance takes and the length of the pause it imposes on the other services.
- **Bug Fix (Level 1 database maintenance, SQL Server only):** **`vwStatsForLevel1DBMaintenance` overstated every table's row count, and so understated how much of each table had been modified since its statistics were last updated.** It summed the row counts in a join of the per-index partition statistics to the table's statistics objects, which repeated every row count once per statistics object: the `RecordCount` it reported was the table's true row count multiplied by the number of its indexes and again by the number of its statistics, and `PctModsSinceLastStatsUpdate` was divided by the same factor. A table with three indexes and four statistics that had 30% of its rows modified was reported as 2.5% modified.
  - While the statistics threshold was itself applied at one hundredth of its intended value (the entry above), the two errors partly cancelled. Correcting the threshold alone would have left a table needing to be modified many times over before its statistics were updated, so both are fixed in this release: the 10% threshold now means 10% of the table's rows.
  - Each table's row count is now taken from its heap or clustered index alone, and its statistics are aggregated separately. `UsedSpaceKB`, `ReservedSpaceKB` and `FreeSpaceKB` were inflated by the number of statistics objects in the same way, and are corrected with it. **PostgreSQL was never affected**: its view reads one row per table.
  - The view is replaced by `Scripts/SQLServer/v2/MSSQL_5.2.0.0_spManagePartitionsSchemaScoping.sql`. Its result columns are unchanged.
- **Bug Fix (Level 1 and Level 2 database maintenance, SQL Server only):** **Both maintenance views reached beyond the adapter's own schema.** `vwStatsForLevel1DBMaintenance` and `vwStatsForLevel2DBMaintenance` had no schema filter, so on a database shared with the Geotab DIG Adapter this adapter updated statistics on, and reorganized and rebuilt indexes of, the DIG Adapter's `gda` tables as well as its own — on its own intervals, and inside its own pause, which does not pause the DIG Adapter's services. To find its Level 2 candidates it also read the physical statistics of every index in the database, `gda`'s included, in `DETAILED` mode.
  - Both views now return only `dbo` objects, and the Level 2 view reads index statistics table by table for `dbo` tables only, so nothing outside `dbo` is read at all. On a shared database, `gda` is left to the DIG Adapter, which from its own version 5.2.0.0 maintains it. Every object this adapter creates is in `dbo`, so none of its own is excluded. **PostgreSQL was never affected**: both of its views were already limited to the `public` schema.
  - Both views are replaced by `Scripts/SQLServer/v2/MSSQL_5.2.0.0_spManagePartitionsSchemaScoping.sql`. Their result columns are unchanged.
- **Improvement (Level 2 database maintenance, both engines):** **`vwStatsForLevel2DBMaintenance` read every index twice.** It called the `pgstattuple` extension once for each index's free space and again for its total size, and `pgstattuple` reads the whole index on every call — so every Level 2 run read every index in the schema twice, with the other adapter services paused. Both figures now come from a single call, which halves that work. The rows the view returns, and its columns, are unchanged; it is replaced by `Scripts/PostgreSQL/v2/PG_5.2.0.0_spManagePartitionsSchemaScoping.sql`.
  - **On SQL Server the view read every index twice for a different reason.** Its per-partition statistics were a common table expression referenced twice — once to total each index's partitions and once to join those totals back — and SQL Server evaluates such an expression once per reference, so `sys.dm_db_index_physical_stats` read every page of every index twice per run, with the other services paused. The totals are now window aggregates over a single reference; the rows and columns are unchanged. `Scripts/SQLServer/v2/MSSQL_5.2.0.0_spManagePartitionsSchemaScoping.sql` replaces the view.
- **Bug Fix (Level 2 database maintenance, SQL Server):** **`vwStatsForLevel2DBMaintenance` could offer a healthy index for rebuild.** It read `sys.dm_db_index_physical_stats` in `DETAILED` mode, which returns a row for every level of an index, and took the highest fragmentation among them - so a small upper level decided. Measured: an index whose leaf level was 0.08% fragmented was offered at 37.5%, over the 30% rebuild threshold. It now reads in `LIMITED` mode, and only the in-row data: each partition's leaf-level fragmentation, which is what `REORGANIZE` and `REBUILD` act on, with fragmentation and size taken from the same row. `LIMITED` reads only the pages above each leaf level, so the read is much cheaper as well. The Geotab DIG Adapter's view, which mirrors this one, had the same defect and has the same fix.
- **Bug Fix (Level 1 and Level 2 database maintenance, both engines):** **A Level 1 or Level 2 maintenance run that failed stopped the adapter.** Neither level handled an error from its own work, so any failure — a maintenance view that could not be read, or a statistics, vacuum or index statement the database refused — reached the maintenance service's last-resort error handler, which logs at `Fatal` and then **ends the adapter process**. Nothing recorded the failure, so an adapter restarted by a service manager reached the same point about five minutes after starting and stopped again. The records of the maintenance runs that had already completed in the same iteration were lost with it.
  - On PostgreSQL the likeliest cause is a missing grant: the Level 2 view calls the `pgstattuple` extension, so a database in which the adapter's login does not hold `EXECUTE` on `pgstattuple` stopped the adapter on the second maintenance iteration after every start.
  - A failed run is now logged at `Error` and written to `DBMaintenanceLogs2` with `Success` set to false, and the rest of the iteration continues: the other services are released from the Level 2 pause, the iteration's other maintenance records are written, and the failed level is not attempted again until `Level1DatabaseMaintenanceIntervalMinutes` or `Level2DatabaseMaintenanceIntervalMinutes` has elapsed, measured from when the failed run ENDED (a successful run's interval is measured from its start).
- **Bug Fix (database partition maintenance, both engines):** **A partition maintenance run that failed while the adapter was running stopped the adapter.** `spManagePartitions` runs when the adapter starts and then every 20 days, with the other services paused. Nothing handled an error from that periodic run, so any failure — a missing `EXECUTE` grant on the procedure or function, say, or a statement the database refused — reached the same last-resort error handler as a failed Level 1 or Level 2 run (the entry above), which logs at `Fatal` and **ends the adapter process**.
  - A failed periodic run is now logged at `Error` and written to `DBMaintenanceLogs2` with `Success` set to false, the other services are released from the pause, and the adapter carries on. **The run is attempted again after 60 minutes** rather than after another 20 days: the interval is measured from the latest run, successful or not, and waiting 20 days to retry a run that created nothing is not what the interval is for. Each attempt pauses the other services, so a failure that persists is retried about hourly — every two hours when each attempt runs to its one-hour limit, since the 60 minutes are counted from the end of the failed attempt — rather than on every iteration. Every run creates partitions through the end of the following calendar month, so the retries begin with days in hand before new records would need a partition that does not yet exist. Like a Level 1 or Level 2 statement, the periodic run is retried at most three times after a transient failure (see below).
  - **If partition maintenance still has not succeeded when fewer than two days of the partitions created by the last successful run remain, the adapter stops**, as a failed run at startup does, so that a cause that persists cannot carry it past the end of its partitions. Beyond them, every new record would land in the table's DEFAULT partition on PostgreSQL, or in the last, open-ended partition on SQL Server, and would have to be moved when its partition was created. A run falls due at least six days before this can happen, so a failure has at least six days of further attempts first.
  - **A failure of the run at startup still stops the adapter**, deliberately and as before. At startup the other services are waiting for partitioning to finish before they write anything, and the adapter must not write records before the partitions they belong in exist.
  - The README's Section 3.6 said that `spManagePartitions` runs once daily. It runs at startup and then every 20 days, and the section now says so.
- **Bug Fix (Level 1 and Level 2 database maintenance, both engines):** **A maintenance statement that kept timing out was retried almost without limit — Level 2's while every other service was paused.** Every Level 1 and Level 2 statement, and each read of the two maintenance views, ran under the retry policy the adapter uses for ordinary database transactions, which retries a timeout up to 1,000,000 times. That suits a short statement that a passing condition will release. An index rebuild too large to finish within the timeout, or one that cannot obtain the lock it needs, fails the same way every time, so Level 2 held the other services paused — and with them every data feed — for as long as the retrying went on.
  - Retrying was also the wrong remedy for a timeout. Beneath that policy, each database call retried a timeout ten more times itself, allowing each attempt 300 seconds longer than the last, up to an hour, and every attempt started the statement's work again from the beginning. So a statement that could not finish was abandoned only after many hours of attempts — about six and a half for each of the outer policy's attempts, at the shipped `TimeoutSecondsForDatabaseTasks` of 600 seconds — and one that needed, say, fifty minutes spent hours on shorter attempts before any was allowed long enough.
  - **Each of these statements and view reads now gets one attempt, allowed a full hour** — 3,600 seconds, the longest any single attempt was ever allowed — **and a timeout is not retried.** The run is recorded as failed, the other services resume, and the level is attempted again after its configured interval (see the entries above). A statement that can finish within the hour now does so on its first attempt, and one that cannot holds the other services paused for about an hour at most, rather than for as long as its retries go on. A failed Level 1 or Level 2 run is now attempted again an interval after it ENDED rather than after it started: timed from its start, a Level 2 run that failed at its one-hour limit was due again at once, and with the default 60-minute interval the feeds were paused about 60 minutes in every 65. A Level 2 run skipped because another service did not pause is now recorded as a failed run too, instead of being attempted again on every tick. A deadlock, or a failure to open a database connection, is still retried, at most three times; a connection lost part-way through a statement never was. The cases that can now fail where a retry might eventually have succeeded are a statement held up for more than an hour by another session's lock and, on PostgreSQL, a statement stopped sooner by a `lock_timeout` or `statement_timeout` that an administrator has set for the adapter's login or database, which counts as a timeout; either is attempted again at the next run. The periodic partition run (see the entry above) is treated the same way. `TimeoutSecondsForDatabaseTasks` no longer governs any of these statements.
  - The reads of the two maintenance views also no longer carry a deadline of `TimeoutSecondsForDatabaseTasks`. The service checked that deadline only once a read had completed, so with an hour to complete in, a read that took longer than `TimeoutSecondsForDatabaseTasks` would have been treated as cancelled after it had succeeded — and that stops the adapter.
- **Bug Fix (Level 2 database maintenance, both engines):** **A Level 2 run could not start in the part of an enabled maintenance window that falls after midnight UTC.** Only the current day's window was considered. At 00:10 UTC, a window that starts at 23:30 UTC and lasts an hour has not yet begun *today* — the window that is open began yesterday — so the adapter concluded that it was outside the window and did not start Level 2 until the next evening. Every window that starts before midnight UTC and ends after it was cut short at midnight in this way; in Toronto during daylight saving time, midnight UTC is 20:00 local.
  - The window that opened the previous day is now considered as well, and a run started in it stops starting new index operations at that window's end. A window that does not cross midnight UTC, and a disabled window, behave exactly as before.
- **Bug Fix:** Resolved an issue whereby every setting whose value must name one of a fixed set of alternatives was matched **case-sensitively**, against the exact spelling and nothing else. A configuration file saying `currenttime`, `postgresql` or `continuous` stopped the adapter from starting, while `CurrentTime`, `PostgreSQL` and `Continuous` did not — and nothing in the documentation said that these settings, alone among the settings around them, were case-sensitive. All of them now accept any casing, and ignore surrounding whitespace. The settings are `DatabaseSettings:DatabaseProviderType`, `AppSettings:GeneralFeedSettings:FeedStartOption`, and the `StatusData` and `FaultData` forms of `<Type>LocationServiceOperationMode`. Each of their error messages now also lists the values that would have been accepted, which none of them previously did.
  - Two further faults in the `DatabaseProviderType` check are fixed with it. The message reporting a value it could not use **named an empty field instead of the value supplied**, so the operator was told `Unsupported DataAccessProviderType ('')` — the one fact they could have acted on was the one omitted. And the value `NullValue`, a member of the underlying type but not a database provider, **was accepted**: no provider factory was registered, and the adapter carried on and failed later, elsewhere, with an unrelated symptom. Both are corrected, and any provider type added to the solution in future but not wired up is likewise refused at startup.
  - **One narrow case becomes less permissive.** A number is no longer accepted in place of a name. `DatabaseProviderType` previously took `0` and `1` as PostgreSQL and SQL Server, because the conversion accepted a number and applied it to the underlying type without checking the result named anything. No documented value is affected, and the other settings listed above never accepted a number.
- **Bug Fix (database scripts, both engines):** Every database upgrade script opens by checking that the database is at the version the script upgrades from, and refusing to run if it is not. **The check reported the mismatch correctly and then failed to stop the upgrade from running anyway — and on SQL Server, against a database whose version table held no row, it did not report at all.** Running a script against the wrong database version therefore applied that script's changes to a database they were never written for, and — because the version stamp at the end of the script also ran — left the database **reporting the version it had just refused to upgrade to**. On a multi-version upgrade, where the scripts are run in sequence, each later script then checked itself against that false version and proceeded in turn.
  - On **SQL Server** the refusal used `RETURN`, which ends only the batch it appears in. Every upgrade script's body lives in later batches, separated by `GO`, so `RETURN` had no effect on any of it. The refusal now uses `SET NOEXEC ON`, which suppresses the remainder of the script, with a matching `SET NOEXEC OFF` at the end.
  - **On SQL Server the check also did not fire at all when the version table held no row.** `IF @actualStartingDatabaseVersion <> @requiredStartingDatabaseVersion` evaluates to `UNKNOWN` rather than `TRUE` when the preceding `SELECT` returns nothing, so the refusal was skipped entirely. Against a database whose `dbo.MiddlewareVersionInfo2` was empty — a hand-built database, a schema-only restore, or one whose row was removed during troubleshooting — the script printed no error, applied its whole body to a database of unknown version, and stamped the version it had not upgraded from. The guard now tests for a missing row explicitly, with its own message naming the empty table, ahead of the version comparison. **PostgreSQL was never affected**: its check uses `IS DISTINCT FROM`, which is `true` when no row was found, so the same operator error was refused on one engine and silently accepted on the other.
  - On **PostgreSQL** the refusal raised an exception, but the scripts ran outside a transaction and `psql` by default reports an error and continues to the next statement. Each script is now wrapped in a single transaction, so a refusal rolls back every change it would have made. PostgreSQL applies schema changes transactionally, so this also means an upgrade that fails part-way through no longer leaves a half-applied schema behind.
  - **The fix is applied to the historical upgrade scripts as well as the current one**, because the documented upgrade path runs them in sequence and that is exactly where the problem could be reached. The scripts are otherwise unchanged, and a full sequential upgrade from 3.0.0.0 produces a byte-identical schema on both engines before and after the change.
  - Documented the `sqlcmd -b` and `psql ON_ERROR_STOP=1` invocations for the install and upgrade steps, which were previously shown with no command line at all. With the fix above, a refused upgrade is harmless either way; these flags are what make the refusal visible to an installer or CI step rather than being reported as success.
- **Bug Fix:** **A cache loaded from a MyGeotab data feed could stop loading before it held every record, and stay incomplete until a later update interval.** The loader treated any batch containing fewer records than the configured results limit as proof that the feed was exhausted. MyGeotab may apply the results limit before it filters the records it returns, so a short batch can arrive while further records remain. When that happened, the cache stopped early, and the table it populates stayed incomplete until the next update interval resumed the feed from its stored version — a window in which anything joining to that table silently lost rows. This was observed against a database whose `User` feed returned 4,731 of 5,962 records in its first batch: `Users2` held 4,731 records, and reached 5,962 only about a minute later.
  - Cache population now continues until the feed returns an empty batch. The initial load completes in a single cache operation and the following update interval adds nothing, so steady-state behaviour is unchanged. Each update that finds changes now makes one further call, which normally returns nothing: before, a batch shorter than the results limit ended the update on its own.
  - Two further conditions are now explicit. Retrieval by `Get` rather than `GetFeed` ends after its single call, because it has no version cursor to advance. And population ends with a warning if the feed returns records without advancing its version, which would otherwise repeat the same request indefinitely.
- Updated NuGet package references to their latest stable versions, including `Geotab.Checkmate.ObjectModel` (11.118.421 → 11.139.528), the `Microsoft.Extensions.Hosting` packages (10.0.7 → 10.0.11) and `NLog.Extensions.Logging` (6.1.2 → 6.2.0).
- **Documentation:** Clarified the supported Azure options for hosting the adapter database. Neither **Azure SQL Database** nor **Azure SQL Managed Instance** can host it — the former exposes only the `PRIMARY` filegroup while the adapter places each partition period on its own filegroup, and the latter permits neither a physical file path in `ALTER DATABASE ... ADD FILE` nor host file system access, both of which `spManagePartitions` requires. Because that procedure is re-executed on startup and periodically thereafter, the limitation affects normal operation and not merely the initial database setup. **SQL Server on an Azure VM** is the option to use. The previous wording named Managed Instance as supported.
- Updated required adapter database version to 5.2.0.0.
- Updated version to 5.2.0.0.

## Version 5.1.0.0

- **NOTE:** This build **includes changes to the schema of the adapter database and to the `appsettings.json` file**.
  - To upgrade an existing installation from version 5.0.0.3 to version 5.1.0.0, see the [Upgrade Guide](docs/upgrade-guides/v5.1.0.0.md).
- Added **ShipmentLog** data feed. The adapter can now pull [ShipmentLog](https://geotab.github.io/sdk/software/api/reference/#ShipmentLog) records from MyGeotab via `GetFeed<ShipmentLog>`.
  - Added `ShipmentLogs2` partitioned table (partitioned on `DateTime`), `stg_ShipmentLogs2` staging table, and `spMerge_stg_ShipmentLogs2` stored procedure/function (SQL Server + PostgreSQL). See [SCHEMA_REFERENCE.md](SCHEMA_REFERENCE.md) for column details.
  - Added `ShipmentLogProcessor2` feed processor. It depends on `DeviceProcessor2` and `UserProcessor2` (for Device and Driver foreign keys).
  - Added a `ShipmentLog` section to the `Feeds` configuration in `appsettings.json` with the new `EnableShipmentLogFeed` and `ShipmentLogFeedIntervalSeconds` settings. The feed is **disabled by default** (`EnableShipmentLogFeed: false`).
- Added packaged **partition-retirement utility scripts** automating the longer-term data-retention procedure in README Section 3.6 (standalone and on-demand — not part of database creation or upgrade, and never required for normal adapter operation):
  - `Scripts/SQLServer/v2/MSSQL_PartitionRetirementUtility.sql` installs three guarded stored procedures (`spAdapterPartitionRetirement_Preview` / `_SwitchOut` / `_Cleanup`) that discover partitions, boundaries, filegroups, non-aligned indexes/constraints and foreign keys from metadata, print-only by default (`@Execute = 0`), save every dropped definition in recovery markers before touching it, and confine all destructive action behind explicit confirmation parameters.
  - `Scripts/PostgreSQL/v2/PG_PartitionRetirementUtility.sql` installs `fnAdapterPartitionRetirement`, which detaches (never drops) retirable partitions of adapter-managed tables only, in foreign-key dependency order, reporting anything it skips.
  - Corrected the Section 3.6 manual SQL Server procedure: several tables carry additional non-aligned indexes and constraints beyond `UI_<TableName>_Id` (notably `Trips2`, whose primary key is nonclustered), and four tables' unique indexes enforce inbound foreign keys that must be dropped and recreated around partition switches.
  - Added an operator guide at [docs/partition-retirement-utility.md](docs/partition-retirement-utility.md): what happens to retired data, quick-start for both engines, FAQ, and a troubleshooting reference covering every message the utility can produce.
- **Bug Fix:** Resolved an issue whereby the adapter terminated when a MyGeotab API call completed without returning a result. Five call sites (`GetAsync<T>`, both `GetFeedAsync<T>` overloads, `GetVersionInformationAsync` and `AddAsync<T>`) threw a general exception on a null result; because that throw occurred outside the retry policy and was not a connectivity exception, it was treated as fatal and stopped the adapter, requiring a manual restart. Null results have been observed during MyGeotab-side maintenance and failover windows. These sites now raise a connectivity exception, so the adapter's existing connectivity-restoration logic pauses the feeds and resumes automatically once MyGeotab is reachable again — the same handling already applied to request timeouts.
- Added the missing `AuditLogs2` and `ShipmentLogs2` tables to the clear-database scripts (`Scripts/SQLServer/v2/MSSQL_ClearDatabaseScript.sql` and `Scripts/PostgreSQL/v2/PG_ClearDatabaseScript.sql`), which are used to empty an adapter database without recreating it. Without these entries, records in those two tables survived a clear operation.
- Documented **odometer-type diagnostics and source detection** in [SCHEMA_REFERENCE.md](SCHEMA_REFERENCE.md), covering which diagnostics carry odometer values and how to determine whether a reading originated from the engine bus or was derived from GPS.
- Corrected a number of inaccuracies in [SCHEMA_REFERENCE.md](SCHEMA_REFERENCE.md): the documented primary-key column order for the partitioned tables (the partitioning column is listed first, with the per-engine difference stated explicitly where PostgreSQL and SQL Server differ), the `MiddlewareVersionInfo2` entry (that table has no primary key or unique constraint), the `LogRecords2.DeviceId` description, and the PostgreSQL example queries (now schema-qualified). Also expanded the `IsAuxiliary1On` ... `IsAuxiliary8On` shorthand in the Geotab DIG Adapter [SCHEMA_REFERENCE.md](../GeotabDIGAdapter/SCHEMA_REFERENCE.md) into one row per column.
- Updated required adapter database version to 5.1.0.0.
- Updated version to 5.1.0.0.

## Version 5.0.0.3

- **NOTE:** There are **no changes to the MyGeotab API Adapter in version 5.0.0.3**. The version is updated for alignment with the broader MyGeotab API Adapter solution, in which a bug fix was made to a different component (the Geotab DIG Adapter — see its [CHANGELOG](../GeotabDIGAdapter/CHANGELOG.md) for details). The MyGeotab API Adapter binaries are functionally identical between v5.0.0.1 and v5.0.0.3; existing v5.0.0.1 installations do not need to be upgraded.
  - To upgrade an existing installation from version 5.0.0.1 to version 5.0.0.3 (for version alignment only), see the [Upgrade Guide](docs/upgrade-guides/v5.0.0.3.md).
- Updated version to 5.0.0.3.

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
