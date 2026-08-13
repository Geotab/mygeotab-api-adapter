# Partition-Retirement Utility — Operator Guide

This guide covers the packaged partition-retirement utility scripts introduced in README Section 3.6 (Automated Database Maintenance > Longer-Term Data Retention Strategy):

- **SQL Server:** `Scripts/SQLServer/v2/MSSQL_PartitionRetirementUtility.sql` — installs `spAdapterPartitionRetirement_Preview`, `spAdapterPartitionRetirement_SwitchOut` and `spAdapterPartitionRetirement_Cleanup`.
- **PostgreSQL:** `Scripts/PostgreSQL/v2/PG_PartitionRetirementUtility.sql` — installs `fnAdapterPartitionRetirement`.

The scripts are standalone and on-demand: they are not part of database creation or upgrade, are never required for normal adapter operation, and installing them changes no data. They automate the manual retirement procedure documented in README Section 3.6 (their messages cite that section's step numbers) while leaving every destructive decision explicit.

The sections below give a quick-start for each engine, an FAQ, and a troubleshooting reference that lists **every** error, warning, refusal and skip message the utility can produce, with its meaning and the action to take.

## What happens to your data

The single most important thing to understand: **retirement never copies, exports or deletes your data — it re-labels it.** On both engines the core operation is metadata-only:

- **SQL Server** uses `ALTER TABLE ... SWITCH PARTITION`, which reassigns a partition's pages to a staging table named `<Table>_retired_<period>` (for example `StatusData2_retired_202501`) on the same filegroup. No rows are read or written; the switch is atomic and effectively instant regardless of row count.
- **PostgreSQL** uses `ALTER TABLE ... DETACH PARTITION`, after which the partition becomes an ordinary standalone table keeping its original name (for example `StatusData2_202501`), data and indexes.

Each retired period gets its own table — one staging/detached table per partitioned table per period; periods are never combined. A multi-period run therefore leaves many small tables in place until they are archived and dropped (a 12-month retirement across all partitioned tables can produce a few hundred), which is normal and harmless. Consolidate them during archiving if you prefer — the utility itself never merges data.

After a retirement run, the retired rows therefore sit in **ordinary tables inside the same database**. From that point the lifecycle is entirely in your hands:

1. **Archive (optional, your tools).** If the data must be kept, export or back up those tables with whatever you already use — on SQL Server: native `BACKUP DATABASE` (staging tables are included like any other table), `bcp` out, `SELECT INTO` an archive database, SSIS; on PostgreSQL: `pg_dump --table`, `COPY ... TO`, or copy to an archive database. The utility deliberately performs no export itself and involves no physical file handling.
2. **Drop (explicit, gated).** Disk space is reclaimed only when the retired tables are dropped. On SQL Server that is the `Cleanup` procedure — which refuses without `@ConfirmDataArchivedOrNotNeeded = 1` and independently refuses any staging table still holding rows dated on or after `@OlderThan`. On PostgreSQL you `DROP TABLE` the detached tables yourself; the function never drops them for you.

If a run is interrupted at any point, nothing is lost: data is always either in the live table or in a retired/staging table, and every phase is re-runnable (see the FAQ).

## Quick start — SQL Server

The happy path retires all periods older than a cutoff, archives the staged data, and reclaims the disk space. Example cutoff: `'2025-07-01'` retires every whole partition period that lies entirely before July 2025.

1. **Install the procedures** (one-time; repeat only when a newer script version ships). Open `MSSQL_PartitionRetirementUtility.sql`, adjust the `USE [geotabadapterdb]` statement if your database has a different name, and run it as a member of `db_owner`. Installation only creates the three procedures.
2. **Preview — any time, read-only.** Safe to run while the adapter is up:

```sql
EXEC [dbo].[spAdapterPartitionRetirement_Preview] @OlderThan = '2025-07-01';
```

   Review the reports: retirable periods and their state, per-table row counts, the non-aligned indexes/constraints and foreign keys the switch-out will drop and recreate, anything it will refuse or skip, staging tables already awaiting archive, and filegroups ready for removal.
3. **Dry-run the switch-out — any time, read-only.** With the default `@Execute = 0` the procedure only prints the exact statements it would run:

```sql
EXEC [dbo].[spAdapterPartitionRetirement_SwitchOut] @OlderThan = '2025-07-01';
```

4. **Take a database backup** (strongly recommended before the first-ever retirement run), then **stop the adapter** and execute the switch-out:

```sql
EXEC [dbo].[spAdapterPartitionRetirement_SwitchOut] @OlderThan = '2025-07-01', @Execute = 1;
```

   Execution refuses while any other session is connected to the database (see the FAQ on permissions if it reports that `VIEW SERVER STATE` is not held). When it completes, every retired period's rows sit in `<Table>_retired_<period>` staging tables — no data has been deleted.
5. **Restart the adapter if you wish, and archive at your leisure.** The staging tables are ordinary tables the adapter never touches, so the adapter can run normally while you export or back them up (step 1 of "What happens to your data" above). Skip archiving entirely if the data is not needed.
6. **Stop the adapter again and clean up.** Dry-run first if you want to see the plan (`@Execute = 0`), then:

```sql
EXEC [dbo].[spAdapterPartitionRetirement_Cleanup] @OlderThan = '2025-07-01',
    @ConfirmDataArchivedOrNotNeeded = 1, @Execute = 1;
```

   This drops the staging tables (the only destructive action — hence the confirmation flag), verifies each retired period's partition is empty in every partitioned table, merges the emptied boundaries, and removes the freed files and filegroups. Disk space is released here.
7. **Restart the adapter.** Automated partition management (`spManagePartitions`) is unaffected by removed historical boundaries.

## Quick start — PostgreSQL

Example cutoff: `'2025-07-01'` detaches every partition whose entire range lies before July 2025.

1. **Install the function** (one-time; repeat only when a newer script version ships). Connect to the adapter database (default `geotabadapterdb`) and run `PG_PartitionRetirementUtility.sql` as the adapter table owner (default `geotabadapter_client`) or a superuser.
2. **Dry-run — any time, read-only.** Safe to run while the adapter is up:

```sql
SELECT * FROM public."fnAdapterPartitionRetirement"('2025-07-01');
```

   The report lists exactly what would be detached (with approximate row counts and sizes), anything skipped and why, and the manual steps that will remain.
3. **Take a database backup** (strongly recommended before the first-ever retirement run), then **stop the adapter** and execute:

```sql
SELECT * FROM public."fnAdapterPartitionRetirement"('2025-07-01', true);
```

   Execution refuses while any other session is connected (`DETACH PARTITION` briefly needs an `ACCESS EXCLUSIVE` lock on each parent). Each detached partition becomes a standalone table keeping its name, data and indexes; foreign keys it carried onto live adapter tables are removed so an archived table can never block future retirement. No data is deleted.
4. **Restart the adapter, then archive at your leisure.** The detached tables are ordinary tables the adapter never touches. Export them if the data must be kept — `pg_dump --table`, `COPY ... TO`, or copy to an archive database.
5. **Drop the detached tables to reclaim disk space** — the function never drops them for you:

```sql
DROP TABLE public."StatusData2_202501";  -- repeat per detached table
```

6. **Run `ANALYZE` on the affected parent tables** (for example `ANALYZE public."StatusData2";`) so the planner's statistics reflect the reduced tables.

> **Note:** On databases with hundreds of retirable partitions, advance the cutoff in stages (for example one month at a time) rather than in a single call — all of one call's detaches share a single transaction and its lock budget (see the FAQ on lock limits).

## FAQ

**Can the utility delete or lose my data?**

The only statement in the entire utility that deletes data is the staging-table `DROP` inside the SQL Server `Cleanup` procedure, and it is double-gated: it refuses without `@ConfirmDataArchivedOrNotNeeded = 1`, and it independently refuses any staging table that still holds even one row dated on or after `@OlderThan`, whatever the table's name suggests. On PostgreSQL the utility contains no destructive statement at all — dropping detached tables is a manual step it only ever prints instructions for. Everything else is metadata movement: rows are always either in the live table or in a retired/staging table in the same database. Only whole periods strictly older than the cutoff are ever touched; both engines refuse a cutoff in the future, so current and future partitions can never be affected.

**What permissions do I need?**

*SQL Server:* run the procedures as a member of `db_owner`. Removing files and filegroups (the last step of `Cleanup`) additionally requires `ALTER` permission on the database. The connected-session safety check requires `VIEW SERVER STATE` — note this is a **server-level** permission that `db_owner` does **not** include. Without it, execution refuses; either have `VIEW SERVER STATE` granted to your login, or — after yourself ensuring the adapter is stopped and nothing else is connected — pass `@AcknowledgeSessionCheckUnavailable = 1` to accept responsibility for that check.

*PostgreSQL:* run the function as the owner of the adapter tables (default `geotabadapter_client`) or as a superuser. The install script grants `EXECUTE` to `geotabadapter_client` and revokes it from `PUBLIC`.

**Do I have to stop the adapter? When exactly?**

Only while an *executing* run is in progress. `Preview` and every dry run (`@Execute = 0` / `Execute => false`) are read-only and safe at any time. Executing runs refuse while any other session is connected to the database — that is what enforces "stop the adapter first". The window can be split: on SQL Server you may run `SwitchOut` in one maintenance window, restart the adapter, archive the staging tables while the adapter runs normally, and run `Cleanup` in a later window (each executing procedure enforces its own session gate). On PostgreSQL the adapter can be restarted as soon as the detaches commit; archiving and dropping the detached tables need no exclusive access.

**What happens if a run is interrupted (timeout, lost connection, server restart)?**

Nothing needs manual repair — re-run the same procedure with the same `@OlderThan`.

*SQL Server:* before `SwitchOut` drops any foreign key, constraint or non-aligned index, it saves the object's full `CREATE` statement as an extended property on its table (marker names start with `PartitionRetirement_Fk_`, `PartitionRetirement_Cns_` or `PartitionRetirement_Idx_`), and it removes each marker only after the object is recreated. A re-run therefore always restores whatever an interrupted run left missing — the recreate phase works from the markers, on any table, whether or not that table has switches in the re-run. `Preview` lists pending markers under "PENDING recreations from an interrupted run", and `Cleanup` refuses to execute while any marker is pending (so you cannot be told to restart the adapter while indexes or foreign keys are missing). Empty staging tables left by an interrupted run are reused after their column shape is verified against the live table.

*PostgreSQL:* all detaches of one call commit or roll back together with that call's transaction, and a partition skipped for a foreign-key violation rolls back only its own detach. Detached partitions no longer appear as partitions, so simply repeating an interrupted call completes the remaining work.

**Why was a table or partition skipped or refused?**

Every skip and refusal is deliberate and reported with its reason — the run never leaves a table half-done. The troubleshooting tables below list every message; in brief, the categories are:

- *SQL Server refusals (whole run, nothing modified):* cutoff missing or in the future; database not partitioned by the adapter layout; partition function backing more than one scheme; tables enabled for CDC, change tracking or replication; a staging table whose column shape no longer matches its live table; a staging table that contains rows while the live partition also does; an object definition too long to fit a recovery marker; another session connected.
- *SQL Server per-table skips (rest of the run proceeds):* computed columns; no clustered index (heap); rows that would stay live referencing rows that would be switched out (the referenced table is skipped so foreign-key recreation cannot fail; skips can cascade along foreign-key chains).
- *SQL Server Cleanup skips:* a boundary is not merged while any table still holds rows in that partition; a staging table is never dropped while it holds rows dated on or after `@OlderThan`.
- *PostgreSQL:* partitioned tables that do not carry the adapter's partition-management signature are never touched; each table's `DEFAULT` partition is never retirable; a detach that would violate a foreign key is skipped and reported with the database error.

**Will the utility keep working as the adapter schema evolves (new tables, new columns)?**

Yes — by design. The utility contains no list of tables, columns or indexes; everything is discovered from the database catalogs at execution time, on every run. On SQL Server the tables in scope are whatever tables sit on the adapter's partition scheme; staging tables are cloned from the live column metadata; the clustered structure is cloned by shape (clustered primary key, clustered unique constraint, or plain clustered index — whatever the table actually has); and the non-aligned indexes, constraints and foreign keys to drop and recreate are enumerated with their full definitions each time. On PostgreSQL, parents qualify by the partition-management signature that every adapter-created partitioned table carries automatically. New partitioned tables, columns, indexes and foreign keys added by future adapter versions are therefore covered without any change to the utility.

The safety net for the unexpected: if a future schema introduced a shape the utility does not support (the refusal and skip categories above — computed columns, heaps, CDC/change tracking/replication, and so on), it fails closed — the affected table or run is refused or skipped with a named reason, and nothing is left half-done. One practical habit: after upgrading the adapter, re-run the utility install script that ships with the new version, so the installed procedures match the newest supported shapes (installation only re-creates the procedures; it touches no data).

**How long will a retirement run take?**

The switches and detaches themselves are metadata-only — effectively instant regardless of row count. What scales with data size:

- *SQL Server:* recreating the dropped non-aligned indexes and constraints (scales with each live table's remaining size) and `WITH CHECK` revalidation of recreated foreign keys (scales with the referencing table's size). These dominate the maintenance window on large tables. The procedure already minimizes this cost: each object is dropped once, all of the table's periods are switched, and each object is recreated once — so retiring six months in one run costs roughly the same index work as retiring one. `Cleanup`'s boundary merges are metadata-only because they only run against verified-empty partitions; the file-removal step may run `DBCC SHRINKFILE ... EMPTYFILE`, which is usually quick on an empty file.
- *PostgreSQL:* each detach completes in milliseconds (it briefly needs an `ACCESS EXCLUSIVE` lock on the parent). The remaining costs are yours: archiving time, `DROP TABLE`, and the closing `ANALYZE`.

**Do I have to archive the retired data?**

No. Archiving is optional and entirely tool-agnostic — the retired rows sit in ordinary tables (see "What happens to your data"). If the data is not needed, proceed straight to the drop step: on SQL Server that is `Cleanup` with `@ConfirmDataArchivedOrNotNeeded = 1` (the flag name deliberately covers both cases — archived, *or not needed*); on PostgreSQL, `DROP TABLE`.

**How would I restore archived data later?**

Restore from your exports into a reporting or archive database and query it there — the exported data is ordinary table data, so restoring it is ordinary too (`RESTORE`/import on SQL Server, `pg_restore`/`COPY ... FROM` on PostgreSQL). Do **not** plan on switching data back *into* the live partitioned tables: switching in is far stricter than switching out (on SQL Server the staging table would additionally need identical nonclustered indexes, matching foreign keys and a trusted `CHECK` constraint matching the partition range), and the README explicitly recommends restoring from exports instead. Keeping the archive queryable outside the adapter database also keeps future retirement runs simple.

**Cleanup refused to drop a staging table, saying it contains rows dated on or after `@OlderThan`. What now?**

That is the data gate doing its job: whatever a staging table's *name* says, `Cleanup` never drops one whose *contents* extend to the cutoff or later. This state does not arise from normal utility operation — it suggests rows were added to the staging table after the switch, or a table was renamed into the staging pattern. Inspect the table (for example `SELECT MAX(<partitioning column>) FROM <staging table>`), work out where the newer rows came from, and only then decide: archive and drop it manually, or leave it. `Cleanup` simply proceeds without it and reports it as `REFUSED`.

**Cleanup finished but did not remove some files or filegroups. Why?**

Two benign reasons:

- SQL Server deallocates dropped tables *deferred* — for a short time after `Cleanup` drops the staging tables, their former filegroup can still show allocated pages. The sweep only removes filegroups that are provably unmapped **and** empty, so it leaves such a filegroup alone. Simply re-run `Cleanup` a little later (it is idempotent); the filegroup is picked up once deallocation completes.
- If a file still holds pages at removal time, `Cleanup` handles it itself: on error 5042 it runs `DBCC SHRINKFILE ... EMPTYFILE` and retries once — you will see that in the output.

Also note the sweep's deliberate scoping: only filegroups matching the adapter's naming signature — `FG_..._<yyyyMM>` **with** a data file whose logical name is `<FilegroupName>_DataFile` — are ever considered. Filegroups you created yourself are never touched, whatever their name.

**What do the staging-table / period suffixes mean?**

`SwitchOut` names staging tables `<Table>_retired_<suffix>`, where the suffix identifies the retired period:

- `yyyyMM` (for example `202501`) — a monthly period.
- `yyyyMMdd` (for example `20250113`) — a weekly or daily period; also the deterministic fallback for a *monthly* boundary when more than one boundary falls in the same calendar month (possible when the initial partition layout was created mid-month). The fallback is computed over all boundaries of the partition function, so a boundary's suffix never changes as the cutoff moves.
- `pre_yyyyMMdd` (for example `pre_20240101`) — the catch-all: rows older than the *first* partition boundary (they live in partition 1 on the `PRIMARY` filegroup). The date is the first boundary, i.e. the exclusive upper bound of what the table contains.

Suffixes are generated culture-independently — the session language cannot affect them.

**Can the oldest rows (before the first partition boundary) be retired?**

*SQL Server:* yes — when the first boundary itself is older than the cutoff, `SwitchOut` includes a `pre_<first boundary>` period that switches partition 1 out. There is no dedicated file or filegroup for partition 1 (it resides on `PRIMARY`), so `Cleanup` merges nothing and removes nothing for it — dropping the staging table is what frees the space.

*PostgreSQL:* no — rows older than the earliest boundary live in each table's `<Parent>_default` partition, and a `DEFAULT` partition cannot be detached by time period. The function reports such rows (`NOT RETIRABLE - DEFAULT partition`) so they are visible; retiring them requires the manual approach in README Section 3.6.

**PostgreSQL: the call failed with "out of shared memory". What happened?**

All detaches of one call run in a single transaction, and each detached partition holds locks in that transaction until commit — a very large batch can exhaust the server's lock table. The function warns in advance (`ADVICE - large batch`) when a call would detach 200 or more partitions. Either advance the cutoff in stages (for example one month at a time — each call is a smaller transaction) or raise `max_locks_per_transaction` on the server. Nothing is half-done after such a failure: the transaction rolled back entirely, and the call can simply be repeated.

**Does retirement interfere with the adapter's automated partition management?**

No. `spManagePartitions` (and its PostgreSQL counterpart) stores only the initial partition configuration in `DBPartitionInfo2` and creates *future* partitions as time advances — it never revisits historical boundaries, so removing them is invisible to it. The utility never modifies `DBPartitionInfo2`, and the next automated maintenance run proceeds normally after retirement.

## Troubleshooting — SQL Server

Every error, warning, refusal and skip message the three procedures can produce, with its meaning and the action to take. Messages are quoted by their leading text (`<...>` marks values filled in at run time); anything printed that is not listed here is progress narration — statement echoes prefixed `--` and indented completion notes such as `dropped ...` / `merged boundary ...` — and needs no action.

### Parameter and layout validation (all three procedures)

| Message (leading text) | Meaning | Action |
|---|---|---|
| `ERROR: @OlderThan is required.` | The cutoff parameter was omitted. | Supply `@OlderThan`; the message shows an example call. |
| `ERROR: @OlderThan is in the future. Refusing:` | The cutoff would make current or future partitions retirable. | Pass a cutoff in the past — typically the start of your retention window. |
| `ERROR: Partition function 'DateTimePartitionFunction_MyGeotabApiAdapter' does not exist in this database.` | This database is not a partitioned MyGeotab API Adapter database — wrong database, or a pre-partitioning schema. | Connect to the adapter database; the utility requires the partitioned layout (schema version 3.13.0.0 or later). |
| `ERROR: Partition function '<name>' is not RANGE RIGHT.` | The partition function is not the layout `spManagePartitions` creates. | Do not use the utility against this layout; follow the manual procedure in README Section 3.6. |
| `ERROR: Partition function '<name>' backs more than one partition scheme.` | A second partition scheme was created on the shared function; merging boundaries would also rearrange the other scheme's tables, so the utility refuses entirely. | Follow the manual procedure in README Section 3.6, or remove/migrate the additional scheme before using the utility. |

### Session gate (SwitchOut and Cleanup, executing runs only)

| Message (leading text) | Meaning | Action |
|---|---|---|
| `ERROR: VIEW SERVER STATE permission not held - cannot verify that no other sessions are connected.` | The safety check needs the server-level `VIEW SERVER STATE` permission, which `db_owner` does not include. Nothing was changed. | Either have `VIEW SERVER STATE` granted to your login, or — after yourself ensuring the adapter is stopped and nothing else is connected — re-run with `@AcknowledgeSessionCheckUnavailable = 1`. |
| `WARNING: VIEW SERVER STATE permission not held - proceeding WITHOUT the connected-session check` | You passed `@AcknowledgeSessionCheckUnavailable = 1`; the run continues with you responsible for exclusivity. | None — provided the adapter really is stopped and nothing else is connected. |
| `ERROR: Other sessions are connected to this database: session_id=<...>` | The gate found other connections (the adapter, SSMS query windows, monitoring tools); the listed sessions identify them. Nothing was changed. | Stop the adapter and close the listed connections, then re-run. |
| `WARNING: VIEW SERVER STATE permission not held - cannot report sessions connected to this database.` (Preview) | Preview could not display the connected-sessions report. | Informational. Executing runs will need the permission or the acknowledgement flag. |

### SwitchOut — refusals (whole run stops; nothing has been modified)

| Message (leading text) | Meaning | Action |
|---|---|---|
| `ERROR: Staging table(s) <names> already contain rows while the corresponding live partition also contains rows.` | An impossible-from-this-utility state: a completed `SWITCH` always empties its source, so something else put rows in the staging table (or refilled the partition). | Investigate manually before any further action — do not drop either table until the origin of the rows is understood. |
| `ERROR: Table(s) <names> are enabled for CDC, change tracking or replication, which prevents partition switching.` | These features make `SWITCH` (or the index drops it needs) fail or misbehave, so the run refuses up front. Preview reports the same tables in advance. | Disable the feature(s) for the affected tables and re-run, or retire those tables via the manual procedure in README Section 3.6. |
| `ERROR: Existing empty staging table(s) <names> no longer match their live table's column shape` | An interrupted earlier run left empty staging tables, and the live table's schema has changed since (for example an adapter upgrade added a column) — reusing them would fail the `SWITCH`. | Drop the stale empty staging table(s) named in the message and re-run. |
| `ERROR: The definition(s) of <names> are too long to save in a recovery marker.` | An index/constraint/foreign-key `CREATE` statement exceeds what the interruption-recovery marker can hold. Refused before anything was dropped. | Retire the affected table(s) via the manual procedure in README Section 3.6. |
| `ERROR: The name(s) of <names> are too long to form a recovery marker name.` | As above, for the marker name itself (128-character limit). | As above. |

### SwitchOut — per-table skips (printed as `-- SKIPPING table ...`; the rest of the run proceeds)

| Skip reason (leading text) | Meaning | Action |
|---|---|---|
| `Table has computed column(s), which the staging-table clone does not support.` | The generated staging table cannot reproduce computed columns, so this table is left untouched. Its rows stay live and its periods' boundary merges stay safely blocked in Cleanup. | Retire this table via the manual procedure in README Section 3.6 (create the staging table by scripting the real table), then Cleanup proceeds normally. |
| `Table has no clustered index (heap), which the staging-table clone does not support.` | No clustered structure exists to clone. Same consequences as above. | Same as above. |
| `Rows in <table> that would remain live reference rows this run would switch out (foreign key <name>).` | Rows staying live (for example newer child rows) reference rows inside the retirement window. Recreating the foreign key `WITH CHECK` after the switch would fail, so the *referenced* table is skipped this run. Skips can cascade along foreign-key chains. | Usually just re-run in a later window, once the referencing rows have aged past the cutoff (or use a cutoff that retires both sides' periods together). For finer control, handle the table via the manual procedure. |

### SwitchOut — outcome messages

| Message (leading text) | Meaning | Action |
|---|---|---|
| `Nothing to switch out: no partitioned table holds rows in a period entirely older than @OlderThan` | No live rows exist in any whole period before the cutoff — or every candidate table was skipped (skip messages precede this). | Verify the cutoff against Preview's "Retirable periods" report; resolve any skip reasons listed above it. If periods show as already switched out, proceed to Cleanup. |
| `Switch-out complete. The staged rows remain in the database in the [<Table>_retired_<period>] tables listed above.` | Success. No data was deleted. | Archive the staging tables if the data must be kept, then run Cleanup with `@ConfirmDataArchivedOrNotNeeded = 1`. |
| `DRY RUN complete - no changes were made.` | The run only printed the statements `@Execute = 1` would perform. | Review the statements; re-run with `@Execute = 1` when satisfied. |

### Cleanup — refusals and warnings

| Message (leading text) | Meaning | Action |
|---|---|---|
| `ERROR: Pending recovery marker(s) from an interrupted SwitchOut exist` | A `SwitchOut` was interrupted and objects (indexes/constraints/foreign keys) are still missing. Cleanup refuses so you are never told to restart the adapter in that state. | Re-run `SwitchOut` with the same `@OlderThan` — it restores everything from the markers — then run Cleanup. |
| `WARNING: Pending recovery marker(s) from an interrupted SwitchOut exist. Execution would REFUSE` | Dry-run form of the refusal above. | Same as above. |
| `ERROR: Refusing to drop staging tables without confirmation.` | Dropping staging tables permanently deletes the staged rows, so it requires the explicit flag. The report above the message lists exactly what would be dropped. | Back up / export the staged data if it must be kept, then re-run with `@ConfirmDataArchivedOrNotNeeded = 1`. |
| `NOTE: Execution would REFUSE at this point - the staging tables above still exist and @ConfirmDataArchivedOrNotNeeded = 0.` | Dry-run form of the refusal above. | Same as above. |
| `-- README step 6: REFUSING to drop <staging table> - it contains at least one row dated on or after @OlderThan` | The data gate: the table's contents extend to the cutoff or later, whatever its name says, so it is never dropped by this procedure (also shown as `REFUSED` in the result summary). Cleanup continues without it. | Inspect the table's contents and establish where the newer rows came from before deciding anything manually — see the FAQ entry on this refusal. |
| `ERROR: Session(s) connected mid-run before the boundary merges:` | Between the staging-table drops and the boundary merges, another session connected — typically a service manager auto-restarting the adapter. Drops completed; **no merge has started**. | Stop the adapter (disable auto-restart for the maintenance window) and re-run Cleanup — completed work is simply not repeated. |

### Cleanup — skips and notes

| Message (leading text) | Meaning | Action |
|---|---|---|
| `-- README step 7: SKIPPING boundary <date> - partition <n> still holds rows in: <tables>` | A retirable period still has live rows in the listed tables (not yet switched out — often tables SwitchOut skipped), so its boundary is not merged: merging a non-empty partition would physically move rows. Also shown as `SKIPPED - partition not empty` in the result summary. | Run `SwitchOut` for that period (resolving any skip reasons first), then re-run Cleanup. |
| `file <name> not yet empty - running DBCC SHRINKFILE EMPTYFILE and retrying` | File removal hit error 5042; the procedure is remediating automatically. | None — informational. |
| `NOTE: The filegroup list above is a dry-run prediction.` | In a dry run the merges have not actually happened, so the "removable filegroups" list is best-effort. The executing sweep re-derives everything from the live catalogs and is the only authority. | None. If an executing run removes fewer filegroups than predicted, see the FAQ entry on deferred deallocation. |
| `Cleanup complete. Restart the adapter when all retirement work is finished` | Success. | Restart the adapter (README step 9). |

### Preview — how to read its reports

Preview is always read-only; each result set's `Report` column names what it shows. Reports worth knowing how to act on:

| Report / status value | Meaning | Action |
|---|---|---|
| `Retirable periods` → `Awaiting SwitchOut` | The period still holds live rows. | Run SwitchOut. |
| `Retirable periods` → `Switched out - staging awaiting archive + Cleanup` | Rows are in staging tables. | Archive if needed, then Cleanup. |
| `Retirable periods` → `Empty - Cleanup can merge/remove` | Nothing left in the live partition or staging. | Cleanup will merge the boundary and remove the filegroup. |
| `Staging tables present` → `Not a candidate at this @OlderThan` | The staging table's period ends after the supplied cutoff, so this Cleanup would leave it alone. | Use a later cutoff when it should be included. |
| `UNRECOGNIZED '_retired_'-pattern tables (Cleanup will NOT touch these)` | A table name resembles the staging pattern but does not parse back to a live partitioned table plus a valid period suffix (for example, its base table was renamed or dropped, or the name was hand-crafted). Cleanup ignores such tables entirely. | If it is a leftover you own, verify its contents and handle it manually; the utility will never drop it. |
| `PENDING recreations from an interrupted run (SwitchOut will restore these)` | Recovery markers exist — an executing SwitchOut was interrupted before recreating these objects. | Re-run SwitchOut with the same `@OlderThan`. |
| `Tables with CDC / change tracking / replication (SwitchOut will refuse these)` | Advance warning of the corresponding SwitchOut refusal. | See that refusal's row above. |

## Troubleshooting — PostgreSQL

The function communicates three ways: hard errors abort the call (`RAISE EXCEPTION`); progress is narrated as notices (`RAISE NOTICE` lines such as `> Detached partition ...`, `>   Removed foreign key ...`, and the `EXECUTE MODE:` / `DRY RUN:` banner); and the authoritative outcome is the returned report — one row per action with `Step`, `Object`, `Action` and `Detail` columns. The notices mirror report rows, so the tables below cover the errors and every report `Action` value.

### Errors (the call aborts; the transaction rolls back)

| Error (leading text) | Meaning | Action |
|---|---|---|
| `ERROR: OlderThan is required.` | The cutoff parameter was omitted or NULL. | Supply the cutoff; the message shows an example call. |
| `ERROR: OlderThan is in the future. Refusing:` | The cutoff would make current or future partitions retirable. | Pass a cutoff in the past — typically the start of your retention window. |
| `ERROR: <n> other session(s) are connected to this database.` | An executing call refuses while anything else is connected — `DETACH PARTITION` briefly needs an `ACCESS EXCLUSIVE` lock on each parent. Nothing was changed. | Stop the adapter and close other connections (psql sessions, monitoring tools), then re-run. Dry runs are unaffected. |
| `ERROR: out of shared memory` (raised by PostgreSQL, usually with `HINT: You might need to increase "max_locks_per_transaction"`) | A very large batch exhausted the server's lock table — all of one call's detaches share a single transaction, and each detached relation holds locks until commit. The whole call rolled back; nothing is half-done. | Advance the cutoff in stages (for example one month at a time), or raise `max_locks_per_transaction`. See the FAQ. |

### Report rows (`Action` column values)

| Action value | Meaning | Action to take |
|---|---|---|
| `OK - no other sessions connected` (Step 1) | The exclusivity precondition holds. | Keep the adapter stopped until retirement is complete. |
| `<n> other session(s) connected` (Step 1) | Reported by a dry run: execution would refuse in this state. | Stop the adapter and close other connections before executing. |
| `SKIPPED - not adapter-managed` | A partitioned table in `public` does not carry the adapter's partition-management signature (a `<Parent>_default` DEFAULT child plus `<Parent>_<period>` child naming), so the utility never touches it. Expected for tables belonging to other applications sharing the database. | None for foreign tables. If an *adapter* table appears here, its partition layout has been altered — investigate before retiring it manually. |
| `NOT RETIRABLE - DEFAULT partition` | Rows older than the earliest partition boundary live in the table's `DEFAULT` partition, which can never be detached by time period. Reported so the rows stay visible. | Expected. If those rows must be retired, use the manual procedure in README Section 3.6. |
| `SKIPPED - unrecognized partition bounds` | The child's `FOR VALUES` clause is not the `FROM (...) TO (...)` range shape the adapter creates. | The partition was not created by the adapter's partition management — investigate and handle it manually. |
| `SKIPPED - partition bounds are not timestamps` | The bounds parsed, but not as timestamps. | Same as above. |
| `DRY RUN - would detach` | A retirable candidate; `Detail` shows the exact statement, range, approximate rows and size. | Execute with `Execute => true` when satisfied. |
| `DETACHED - now a standalone table` | Success: the partition is now an ordinary table keeping its name, data and indexes; foreign keys it carried onto live adapter tables were removed so an archived table can never block future retirement. | Archive if the data must be kept, then `DROP TABLE` to reclaim space (report steps 4–5). |
| `SKIPPED - foreign key violation` | Rows that would remain live (often in the referencing table's own `DEFAULT` partition) reference rows in this partition, so its detach was rolled back; the rest of the run continued. `Detail` includes the database error. | Usually just re-run in a later window with the same or a wider cutoff, once the referencing rows have been retired or removed; or use the manual procedure. |
| `Nothing to retire - no partition lies entirely before <date>` (Summary) | No candidates at this cutoff. | Verify the cutoff against a dry run's report. |
| `<n> partition(s) detached` (Summary) | Outcome of an executing call; a trailing `, <n> skipped (foreign key violations - see above)` counts skipped detaches. | If skips are reported, see the `SKIPPED - foreign key violation` rows above it. |
| `<n> partition(s) would be detached` (Summary) | Outcome of a dry run. | Re-run with `Execute => true` to perform it. |
| `ADVICE - large batch (<n> partitions)` | The call touches 200 or more partitions — a heads-up about the single-transaction lock budget *before* you hit `out of shared memory`. | Prefer staged cutoffs; or raise `max_locks_per_transaction`. |
| `Back up the detached tables if the data must be kept` (Step 4, manual) | Reminder of the remaining manual archive step. | `pg_dump --table`, `COPY ... TO`, or copy to an archive database — if the data is needed. |
| `Drop the detached tables to reclaim disk space` (Step 5, manual) | Reminder that the utility never drops them for you. | `DROP TABLE public."<detached table>";` per table, after archiving (or deciding not to). |
| `Restart the adapter and ANALYZE the affected parent tables` (Step 6, manual) | Closing step. | Restart the adapter; run `ANALYZE` on each affected parent. |
