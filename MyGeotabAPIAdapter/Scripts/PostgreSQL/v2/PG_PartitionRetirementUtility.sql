-- ================================================================================
-- DATABASE TYPE: PostgreSQL
--
-- NOTES:
--   1: This is a STANDALONE, ON-DEMAND utility script. It is NOT part of database
--      creation or upgrade and is never required for normal adapter operation.
--      Installing it only adds one function; it does not read or modify any data
--      at installation time.
--   2: Connect to the adapter database (default name: geotabadapterdb) before
--      running this script.
--   3: The function implements the partition-retirement procedure documented in
--      README Section 3.6 (Automated Database Maintenance > Longer-Term Data
--      Retention Strategy). Step numbers in its output refer to the numbered
--      steps of that section's PostgreSQL procedure.
--   4: Requirements: run as the owner of the adapter tables (default:
--      geotabadapter_client) or as a superuser.
--   5: On databases with very many retirable partitions (hundreds or more),
--      advance the cutoff in stages (e.g. one month at a time) rather than in a
--      single call: all detaches of one call run in one transaction, and each
--      detached partition holds locks in that transaction, so a very large batch
--      can exhaust the server's lock table ("out of shared memory" - raise
--      max_locks_per_transaction, or simply use staged cutoffs).
--
-- DESCRIPTION:
--   public."fnAdapterPartitionRetirement"("OlderThan", "Execute" DEFAULT false)
--
--   Retires (detaches from the live tables) whole partitions whose entire range
--   lies before the "OlderThan" cutoff, using metadata only — the function
--   discovers the partitioned tables and partition bounds at execution time, so
--   it works for monthly, weekly or daily partition intervals and for any
--   database at schema version 3.13.0.0 or later.
--
--   SCOPE: only partitioned tables that carry the adapter's partition-management
--   signature are touched. A parent qualifies when BOTH hold:
--     - it has a DEFAULT child partition named "<Parent>_default" (the shape
--       spManagePartitions creates for every adapter table), AND
--     - every one of its non-DEFAULT children is named "<Parent>_yyyyMM" or
--       "<Parent>_yyyyMMdd" (the adapter's period naming).
--   Any other partitioned table in the public schema — for example one belonging
--   to another application sharing the database — is reported as skipped and
--   never modified.
--
--   PostgreSQL partition retirement needs no index work (every partition carries
--   its own local indexes), so a single function covers the whole README
--   procedure: it performs README step 3 (DETACH PARTITION) and reports the
--   manual steps that remain. Detached partitions become ordinary standalone
--   tables, KEEPING their name, data and indexes — this function never deletes
--   data. Foreign keys the standalone inherited as a partition are removed from
--   it after the detach (an archived table must never pin the live tables'
--   partitions — with them left in place, a detached referencing table would
--   permanently block retirement of the partitions it references). Backing up
--   (README step 4) and dropping (README step 5) the detached tables remain
--   deliberate, manual decisions.
--
--   FOREIGN KEYS: parents are processed in foreign-key dependency order
--   (referencing tables before the tables they reference), so partitions of a
--   chain retire together cleanly. If a partition still cannot be detached
--   because rows that would remain (for example in the referencing table's
--   DEFAULT partition) reference rows inside it, that partition is reported as
--   skipped with the database error - nothing is half-done - and can be retired
--   in a later run once the referencing rows have been retired or removed.
--
--   Usage (returns one report row per action):
--     SELECT * FROM public."fnAdapterPartitionRetirement"('2023-01-01');        -- dry run
--     SELECT * FROM public."fnAdapterPartitionRetirement"('2023-01-01', true);  -- execute
--
--   Safety properties:
--     - "Execute" defaults to false: the function only reports the exact
--       statements it would run. Pass true to perform them.
--     - When executing, the function refuses to run while any other session is
--       connected to the database (stop the adapter first — README step 1;
--       DETACH PARTITION briefly needs an ACCESS EXCLUSIVE lock on the parent).
--     - Only partitions whose entire range lies before "OlderThan" are detached.
--       The current and future partitions and each table's DEFAULT partition can
--       never be affected. Rows in the DEFAULT partition (older than the first
--       partition boundary) cannot be retired by time period — see the README.
--     - Re-runnable: detached partitions no longer appear as partitions, so an
--       interrupted run can simply be repeated. All detaches performed by one
--       call commit together with that call's transaction; a partition skipped
--       for a foreign-key violation rolls back only its own detach.
-- ================================================================================

-- FUNCTION: public.fnAdapterPartitionRetirement(timestamp without time zone, boolean)

-- DROP FUNCTION IF EXISTS public."fnAdapterPartitionRetirement"(timestamp without time zone, boolean);

CREATE OR REPLACE FUNCTION public."fnAdapterPartitionRetirement"(
    "OlderThan" timestamp without time zone,
    "Execute" boolean DEFAULT false)
    RETURNS TABLE("Step" text, "Object" text, "Action" text, "Detail" text)
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
    other_sessions integer;
    rec RECORD;
    skiprec RECORD;
    from_str TEXT;
    to_str TEXT;
    to_ts TIMESTAMP;
    approx_rows TEXT;
    stmt TEXT;
    candidates integer := 0;
    detached integer := 0;
    fk_skipped integer := 0;
BEGIN
    -- ======================================================================================
    -- Validate input parameter values.
    IF "OlderThan" IS NULL THEN
        RAISE EXCEPTION 'ERROR: OlderThan is required. Example: SELECT * FROM public."fnAdapterPartitionRetirement"(''2023-01-01'');';
    END IF;
    IF "OlderThan" > (CURRENT_TIMESTAMP AT TIME ZONE 'UTC') THEN
        RAISE EXCEPTION 'ERROR: OlderThan is in the future. Refusing: this would retire current or future partitions.';
    END IF;

    IF "Execute" THEN
        RAISE NOTICE 'EXECUTE MODE: partitions entirely older than % are being detached.', "OlderThan";
    ELSE
        RAISE NOTICE 'DRY RUN: no changes are made. The report lists what Execute => true would do.';
    END IF;

    -- ======================================================================================
    -- README step 1: the adapter must be stopped. When executing, refuse while any other
    -- session is connected to this database (DETACH PARTITION briefly needs an ACCESS
    -- EXCLUSIVE lock on each parent table).
    SELECT count(*) INTO other_sessions
    FROM pg_stat_activity
    WHERE datname = current_database()
      AND pid <> pg_backend_pid()
      AND backend_type = 'client backend';

    IF "Execute" AND other_sessions > 0 THEN
        RAISE EXCEPTION 'ERROR: % other session(s) are connected to this database. Stop the adapter and close other connections first (README Section 3.6, Longer-Term Data Retention Strategy, PostgreSQL step 1).', other_sessions;
    END IF;

    "Step" := '1 (stop adapter)';
    "Object" := current_database();
    "Action" := CASE WHEN other_sessions = 0 THEN 'OK - no other sessions connected' ELSE other_sessions || ' other session(s) connected' END;
    "Detail" := CASE WHEN other_sessions = 0 THEN 'The adapter must remain stopped until retirement is complete.' ELSE 'Execution would refuse: stop the adapter and close other connections first (README PostgreSQL step 1).' END;
    RETURN NEXT;


    -- ======================================================================================
    -- Scope: report (and never touch) partitioned tables in public that do NOT carry the
    -- adapter's partition-management signature - a DEFAULT child named "<Parent>_default"
    -- AND every non-DEFAULT child named "<Parent>_yyyyMM" / "<Parent>_yyyyMMdd".
    FOR skiprec IN
        SELECT parent.relname AS parent_name
        FROM pg_catalog.pg_partitioned_table pt
        JOIN pg_catalog.pg_class parent ON parent.oid = pt.partrelid
        JOIN pg_catalog.pg_namespace pn ON pn.oid = parent.relnamespace AND pn.nspname = 'public'
        WHERE NOT (
            EXISTS (
                SELECT 1 FROM pg_catalog.pg_inherits i2
                JOIN pg_catalog.pg_class dc ON dc.oid = i2.inhrelid
                WHERE i2.inhparent = parent.oid
                  AND dc.relname = parent.relname || '_default'
                  AND pg_get_expr(dc.relpartbound, dc.oid) = 'DEFAULT'
            )
            AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_inherits i3
                JOIN pg_catalog.pg_class nc ON nc.oid = i3.inhrelid
                WHERE i3.inhparent = parent.oid
                  AND pg_get_expr(nc.relpartbound, nc.oid) <> 'DEFAULT'
                  AND nc.relname !~ ('^' || parent.relname || '_[0-9]{6}([0-9]{2})?$')
            )
        )
        ORDER BY parent.relname
    LOOP
        "Step" := '2 (identify)';
        "Object" := 'public."' || skiprec.parent_name || '"';
        "Action" := 'SKIPPED - not adapter-managed';
        "Detail" := 'This partitioned table does not carry the adapter partition-management signature (a "<Parent>_default" DEFAULT child and "<Parent>_<period>" child naming), so this utility never touches it.';
        RETURN NEXT;
    END LOOP;

    -- ======================================================================================
    -- README steps 2 + 3: identify partitions entirely older than OlderThan on every
    -- adapter-managed partitioned table, and detach them (or report what would be
    -- detached). Parents are ordered by foreign-key dependency - referencing tables
    -- before the tables they reference - so partition chains retire together cleanly;
    -- a detach that still violates a foreign key (for example, referencing rows that
    -- would remain in the referencing table's DEFAULT partition) is skipped and
    -- reported without aborting the rest of the run.
    FOR rec IN
        WITH RECURSIVE adapter_parents AS (
            SELECT pt.partrelid AS parent_oid, parent.relname AS parent_name
            FROM pg_catalog.pg_partitioned_table pt
            JOIN pg_catalog.pg_class parent ON parent.oid = pt.partrelid
            JOIN pg_catalog.pg_namespace pn ON pn.oid = parent.relnamespace AND pn.nspname = 'public'
            WHERE EXISTS (
                SELECT 1 FROM pg_catalog.pg_inherits i2
                JOIN pg_catalog.pg_class dc ON dc.oid = i2.inhrelid
                WHERE i2.inhparent = parent.oid
                  AND dc.relname = parent.relname || '_default'
                  AND pg_get_expr(dc.relpartbound, dc.oid) = 'DEFAULT'
            )
            AND NOT EXISTS (
                SELECT 1 FROM pg_catalog.pg_inherits i3
                JOIN pg_catalog.pg_class nc ON nc.oid = i3.inhrelid
                WHERE i3.inhparent = parent.oid
                  AND pg_get_expr(nc.relpartbound, nc.oid) <> 'DEFAULT'
                  AND nc.relname !~ ('^' || parent.relname || '_[0-9]{6}([0-9]{2})?$')
            )
        ),
        fk_edges AS (
            -- Parent-level foreign keys between adapter-managed parents (PostgreSQL
            -- decomposes them into per-partition legs; conparentid = 0 selects the
            -- top-level constraints). Self-references are ignored for ordering.
            SELECT c.conrelid AS referencing_oid, c.confrelid AS referenced_oid
            FROM pg_catalog.pg_constraint c
            WHERE c.contype = 'f'
              AND c.conparentid = 0
              AND c.conrelid <> c.confrelid
              AND c.conrelid IN (SELECT parent_oid FROM adapter_parents)
              AND c.confrelid IN (SELECT parent_oid FROM adapter_parents)
        ),
        fk_rank_walk AS (
            -- depth 0 = parents no other adapter parent references; a referenced parent
            -- ranks one deeper than its deepest referencer, so ascending rank order is
            -- referencing-first. Depth is capped defensively (a foreign-key cycle would
            -- otherwise recurse forever; cyclic parents fall to the end via COALESCE).
            SELECT ap.parent_oid, 0 AS depth
            FROM adapter_parents ap
            WHERE NOT EXISTS (SELECT 1 FROM fk_edges e WHERE e.referenced_oid = ap.parent_oid)
            UNION ALL
            SELECT e.referenced_oid, w.depth + 1
            FROM fk_rank_walk w
            JOIN fk_edges e ON e.referencing_oid = w.parent_oid
            WHERE w.depth < 32
        ),
        fk_rank AS (
            SELECT parent_oid, MAX(depth) AS rank FROM fk_rank_walk GROUP BY parent_oid
        )
        SELECT ap.parent_name,
               child.relname AS child_name,
               child.oid AS child_oid,
               pg_get_expr(child.relpartbound, child.oid) AS bounds,
               child.reltuples AS reltuples,
               pg_size_pretty(pg_total_relation_size(child.oid)) AS total_size,
               COALESCE(fr.rank, 999) AS fk_rank
        FROM adapter_parents ap
        JOIN pg_catalog.pg_inherits i ON i.inhparent = ap.parent_oid
        JOIN pg_catalog.pg_class child ON child.oid = i.inhrelid
        LEFT JOIN fk_rank fr ON fr.parent_oid = ap.parent_oid
        ORDER BY COALESCE(fr.rank, 999), ap.parent_name, child.relname
    LOOP
        approx_rows := CASE WHEN rec.reltuples < 0 THEN 'unknown' ELSE rec.reltuples::bigint::text END;

        -- The DEFAULT partition is never retirable by time period (README note). reltuples
        -- is an estimate: -1 means never analyzed, so it is reported as unknown rather
        -- than silently treated as empty.
        IF rec.bounds = 'DEFAULT' THEN
            IF rec.reltuples <> 0 THEN
                "Step" := '2 (identify)';
                "Object" := 'public."' || rec.child_name || '"';
                "Action" := 'NOT RETIRABLE - DEFAULT partition';
                "Detail" := 'Approx. ' || approx_rows || ' row(s) older than the earliest partition boundary reside here and cannot be retired by time period (see the README note).';
                RETURN NEXT;
            END IF;
            CONTINUE;
        END IF;

        -- Parse the partition's upper bound from its FOR VALUES clause.
        from_str := substring(rec.bounds FROM 'FROM \(''([^'']+)''\)');
        to_str   := substring(rec.bounds FROM 'TO \(''([^'']+)''\)');
        IF to_str IS NULL THEN
            "Step" := '2 (identify)';
            "Object" := 'public."' || rec.child_name || '"';
            "Action" := 'SKIPPED - unrecognized partition bounds';
            "Detail" := rec.bounds;
            RETURN NEXT;
            CONTINUE;
        END IF;

        BEGIN
            to_ts := to_str::timestamp;
        EXCEPTION WHEN OTHERS THEN
            "Step" := '2 (identify)';
            "Object" := 'public."' || rec.child_name || '"';
            "Action" := 'SKIPPED - partition bounds are not timestamps';
            "Detail" := rec.bounds;
            RETURN NEXT;
            CONTINUE;
        END;

        -- Only partitions whose ENTIRE range lies before OlderThan are retirable.
        IF to_ts <= "OlderThan" THEN
            candidates := candidates + 1;
            stmt := format('ALTER TABLE public.%I DETACH PARTITION public.%I;', rec.parent_name, rec.child_name);

            IF "Execute" THEN
                BEGIN
                    EXECUTE stmt;
                    detached := detached + 1;
                    RAISE NOTICE '> Detached partition %: %', rec.child_name, stmt;
                    -- A detached table KEEPS the foreign keys it inherited as a
                    -- partition. Left in place, they would pin the referenced live
                    -- tables' partitions forever (an archived table must never
                    -- block live retirement), so foreign keys pointing back at
                    -- partitioned adapter tables are removed from the standalone.
                    -- Its data and indexes are untouched.
                    FOR skiprec IN
                        SELECT con.conname
                        FROM pg_catalog.pg_constraint con
                        WHERE con.conrelid = rec.child_oid
                          AND con.contype = 'f'
                          AND con.conparentid = 0   -- top-level only; its per-partition legs drop with it
                          AND con.confrelid IN (SELECT pt2.partrelid FROM pg_catalog.pg_partitioned_table pt2)
                    LOOP
                        EXECUTE format('ALTER TABLE public.%I DROP CONSTRAINT %I;', rec.child_name, skiprec.conname);
                        RAISE NOTICE '>   Removed foreign key % from detached table % (archived tables must not pin live partitions).', skiprec.conname, rec.child_name;
                    END LOOP;
                    "Step" := '3 (detach)';
                    "Object" := 'public."' || rec.child_name || '"';
                    "Action" := 'DETACHED - now a standalone table';
                    "Detail" := stmt || ' [range ' || COALESCE(from_str, 'MINVALUE') || ' to ' || to_str || '; approx. ' || approx_rows || ' row(s); ' || rec.total_size || '; foreign keys onto live adapter tables removed]';
                    RETURN NEXT;
                EXCEPTION WHEN foreign_key_violation THEN
                    fk_skipped := fk_skipped + 1;
                    RAISE NOTICE '> SKIPPED %: foreign key violation (%)', rec.child_name, SQLERRM;
                    "Step" := '3 (detach)';
                    "Object" := 'public."' || rec.child_name || '"';
                    "Action" := 'SKIPPED - foreign key violation';
                    "Detail" := 'Rows that would remain live (possibly in the referencing table''s DEFAULT partition) reference rows in this partition, so it was not detached. Retire or remove those referencing rows first (a later run with the same or wider cutoff, or the manual procedure in README Section 3.6). Database error: ' || SQLERRM;
                    RETURN NEXT;
                END;
            ELSE
                "Step" := '3 (detach)';
                "Object" := 'public."' || rec.child_name || '"';
                "Action" := 'DRY RUN - would detach';
                "Detail" := stmt || ' [range ' || COALESCE(from_str, 'MINVALUE') || ' to ' || to_str || '; approx. ' || approx_rows || ' row(s); ' || rec.total_size || ']';
                RETURN NEXT;
            END IF;
        END IF;
    END LOOP;


    -- ======================================================================================
    -- Summary and the manual steps that remain (README steps 4-6).
    "Step" := 'Summary';
    "Object" := current_database();
    "Action" := CASE
        WHEN candidates = 0 THEN 'Nothing to retire - no partition lies entirely before ' || "OlderThan"::date
        WHEN "Execute" THEN detached || ' partition(s) detached' || CASE WHEN fk_skipped > 0 THEN ', ' || fk_skipped || ' skipped (foreign key violations - see above)' ELSE '' END
        ELSE candidates || ' partition(s) would be detached'
    END;
    "Detail" := CASE
        WHEN candidates = 0 THEN 'No changes were made.'
        WHEN "Execute" THEN 'The detached tables keep their names, data and indexes (foreign keys onto live adapter tables removed). No data was deleted.'
        ELSE 'Re-run with Execute => true to perform the statements above.'
    END;
    RETURN NEXT;

    -- Large batches hold one lock per touched relation for the whole transaction - see
    -- NOTE 5 in the script header.
    IF candidates >= 200 THEN
        "Step" := 'Summary';
        "Object" := current_database();
        "Action" := 'ADVICE - large batch (' || candidates || ' partitions)';
        "Detail" := 'All detaches of one call run in a single transaction and each holds locks until commit. If this call fails with "out of shared memory", advance OlderThan in stages (e.g. one month at a time) or raise max_locks_per_transaction.';
        RETURN NEXT;
    END IF;

    IF candidates > 0 THEN
        "Step" := '4 (backup - manual)';
        "Object" := '-';
        "Action" := 'Back up the detached tables if the data must be kept';
        "Detail" := 'E.g. pg_dump --table, or COPY ... TO, or copy to an archive database (README PostgreSQL step 4).';
        RETURN NEXT;

        "Step" := '5 (drop - manual)';
        "Object" := '-';
        "Action" := 'Drop the detached tables to reclaim disk space';
        "Detail" := 'DROP TABLE public."<detached table>"; (README PostgreSQL step 5). This utility never drops them for you.';
        RETURN NEXT;

        "Step" := '6 (restart - manual)';
        "Object" := '-';
        "Action" := 'Restart the adapter and ANALYZE the affected parent tables';
        "Detail" := 'E.g. ANALYZE public."StatusData2"; (README PostgreSQL step 6).';
        RETURN NEXT;
    END IF;

    RETURN;
END;
$BODY$;

ALTER FUNCTION public."fnAdapterPartitionRetirement"(timestamp without time zone, boolean)
    OWNER TO geotabadapter_client;

GRANT EXECUTE ON FUNCTION public."fnAdapterPartitionRetirement"(timestamp without time zone, boolean) TO geotabadapter_client;

REVOKE ALL ON FUNCTION public."fnAdapterPartitionRetirement"(timestamp without time zone, boolean) FROM PUBLIC;

