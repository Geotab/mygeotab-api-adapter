-- ===========================================================================================
-- DVIRDefectUpdates Test Script:
--    
-- *** INSTRUCTIONS ***
-- 1. Using the MyGeotab UI or Geotab Drive, create a new DVIRLog with three defects - 
--      one of which must be classified as a "minor" defect.
-- 2. Run the MyGeotab API Adapter with the "EnableDVIRLogFeed" setting in appsettings.config
--      set to true. Watch the console window to see that the new DVIRLog is captured and
--      written to the adapter database.
-- 3. Execute the following query to get the Id of the new DVIRLog:
--      select * from DVIRLogs order by id desc limit 1;
-- 4. Set the value of the @dvirLogId variable in the declaration section below to the DVIRLog
--      Id obtained in Step 3.
-- 5. Execute the following query to obtain the Ids of the three defects associated with the
--      subject DVIRLog. Substitute <DVIRLogId> with the DVIRLog Id obtained in Step 3:
--      select * from DVIRDefects where DVIRLogId = '<DVIRLogId>';
-- 6. Set the values of the @dvirDefectId1, @dvirDefectId2 and @dvirDefectId3Minor variables in
--      the declaration section below to the GeotabId values in the rows returned in Step 5.
--      * NOTE: The Id specifed for @dvirDefectId3Minor should be the one for the "minor" defect.
-- 7. Set the @remarkUserId and @repairUserId to appropriate user Ids (users that have the 
--      ability to add repair remarks and update DVIRLogs). These can both be the same Id.
-- 8. With the MyGeotab API Adapter running, execute this script.
--
-- *** EXPECTED RESULTS ***
-- Results for the individual tests are indicated in the comments associated with each test,
-- but as a summary, here is what the result should be:
-- 1. The console and log file should contain a message indicating: "Of the 21 records from
--      DVIRDefectUpdates table, 7 were successfully processed and 14 failed. Copies of any
--      failed records have been inserted into the FailedDVIRDefectUpdates table for reference."
-- 2. In the MyGeotab UI, upon refreshing the subject DVIRLog, it should be noted that:
--      - The first defect has 3 repair remarks and is marked as Repaired.
--      - The second defect has 2 repair remarks and is marked as Repaired.
--      - The third defect has 2 repair remarks and is marked as Not necessary.
--      - The DVIRLog is ready for certification. 
-- 3. The DVIRDefectUpdates table should no longer contain any rows.
-- ===========================================================================================

-- Create in-memory temp table for variables 
PRAGMA temp_store = 2;
CREATE TEMP TABLE _Variables(Name TEXT PRIMARY KEY, RealValue REAL, IntegerValue INTEGER, BlobValue BLOB, TextValue TEXT);

-- Declare variables.
INSERT INTO _Variables (Name) VALUES ('@dvirLogId');
INSERT INTO _Variables (Name) VALUES ('@dvirDefectId1');
INSERT INTO _Variables (Name) VALUES ('@dvirDefectId2');
INSERT INTO _Variables (Name) VALUES ('@dvirDefectId3Minor');
INSERT INTO _Variables (Name) VALUES ('@remarkUserId');
INSERT INTO _Variables (Name) VALUES ('@repairUserId');

-- *** IMPORTANT ***
-- Set the values of the following variables based on the instructions above:
UPDATE _Variables SET TextValue = 'aBocq_sRBcEuc-gEuSK2LjA' WHERE Name = '@dvirLogId';
UPDATE _Variables SET TextValue = 'atdHGkQD21EmhIdX-PeE8rA' WHERE Name = '@dvirDefectId1';
UPDATE _Variables SET TextValue = 'a1Fus7qLDC0O5CNcZOrs2fQ' WHERE Name = '@dvirDefectId2';
UPDATE _Variables SET TextValue = 'agFKnn-TOlUOt0dqCIfgbAA' WHERE Name = '@dvirDefectId3Minor';
UPDATE _Variables SET TextValue = 'b124' WHERE Name = '@remarkUserId';
UPDATE _Variables SET TextValue = 'b124' WHERE Name = '@repairUserId';


-- *** DO NOT CHANGE ANYTHING BELOW THIS LINE ***
-- ===========================================================================================

-- Declare variables.
INSERT INTO _Variables (Name) VALUES ('@timeStampUTC');
INSERT INTO _Variables (Name) VALUES ('@futureTimeStampUTC');
INSERT INTO _Variables (Name) VALUES ('@remark');
INSERT INTO _Variables (Name) VALUES ('@repairStatusNotNecessary');
INSERT INTO _Variables (Name) VALUES ('@repairStatusRepaired');
INSERT INTO _Variables (Name) VALUES ('@testCount');

-- Initialize variables.
UPDATE _Variables SET TextValue = DATETIME('now') WHERE Name = '@timeStampUTC';
UPDATE _Variables SET TextValue = DATE(DATETIME('now'),'+1 day') WHERE Name = '@futureTimeStampUTC';
UPDATE _Variables SET TextValue = '' WHERE Name = '@remark';
UPDATE _Variables SET TextValue = 'NotNecessary' WHERE Name = '@repairStatusNotNecessary';
UPDATE _Variables SET TextValue = 'Repaired' WHERE Name = '@repairStatusRepaired';
UPDATE _Variables SET IntegerValue = 0 WHERE Name = '@testCount';

/* Getting variable value (use within expression) */
--... (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = 'VariableName' LIMIT 1) ...

-- Delete any existing records from the DVIRDefectUpdates table.
select 'Deleting all existing records from the DVIRDefectUpdates table';
delete from DVIRDefectUpdates;

-- Delete any existing records from the FailedDVIRDefectUpdates table.
select 'Deleting all existing records from the FailedDVIRDefectUpdates table';
delete from FailedDVIRDefectUpdates;

select 'Inserting test records into the DVIRDefectUpdates table for DVIRLogId: ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1);

-- NOTE: Provided that the values supplied in the input parameters at the top of this script are valid, it is expected that all records
-- added to the DVIRDefectUpdates table below will be removed from the table once processed by the MyGeotab API Adapter.

-- TEST 001-F: NOTHING TO DO - Null values supplied for all Remark and Repair Status fields.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = '' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, null, null, null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 002-F: INVALID DVIRLogId
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - INVALID DVIRLogId' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ('TestInvalidDVIRLogId',(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 003-F: INVALID DVIRDefectId
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - INVALID DVIRDefect' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1), 'TestInvalidDVIRDefectId', null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 004-F: INVALID REMARK - Remark null.
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - INVALID REMARK - Remark null' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 005-F: INVALID REMARK - RemarkDateTime null.
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - INVALID REMARK - RemarkDateTime null' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 006-F: INVALID REMARK - RemarkUserId null.
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - INVALID REMARK - RemarkUserId null' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 007-F: INVALID REMARK - RemarkDateTime in future.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - INVALID REMARK - RemarkDateTime in future' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@futureTimeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 008-F: INVALID REMARK - Invalid RemarkUserId.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - INVALID REMARK - Invalid RemarkUserId' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), 'TestInvalidRemarkUserId', (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 009-S: VALID REMARK
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (@dvirDefectId1) in the MyGeotab database.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - VALID REMARK' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 010-S: VALID REMARK 2
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (@dvirDefectId1) in the MyGeotab database.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - VALID REMARK 2' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 011-S: VALID REMARK
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (@dvirDefectId2) in the MyGeotab database.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 2 - VALID REMARK' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId2' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 012-S: VALID REMARK
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (@dvirDefectId3Minor) in the MyGeotab database.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 3 - VALID REMARK' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId3Minor' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 013-F: INVALID REPAIR STATUS CHANGE - RepairStatus Not 'Repaired' or 'NotNecessary'.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = '' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), 'NotRepaired', (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairUserId' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 014-F: INVALID REPAIR STATUS CHANGE - RepairDateTime null.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = '' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairStatusRepaired' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairUserId' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 015-F: INVALID REPAIR STATUS CHANGE - RepairStatus null.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = '' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairUserId' LIMIT 1), null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 016-F: INVALID REPAIR STATUS CHANGE - RepairUserId null.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = '' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairStatusRepaired' LIMIT 1), null, null, null, null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 017-F: INVALID REMARK WITH VALID REPAIR STATUS CHANGE - RemarkUserId null
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - INVALID REMARK WITH VALID REPAIR STATUS CHANGE - RemarkUserId null' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairStatusRepaired' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), null, (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 018-S: VALID REMARK WITH VALID REPAIR STATUS CHANGE - Both in a single record.
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (@dvirDefectId1) in the MyGeotab database.
--   - The subject DVIRDefect (@dvirDefectId1) should be marked as Repaired in the MyGeotab database.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - VALID REMARK WITH VALID REPAIR STATUS CHANGE (Update to Repaired)' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairStatusRepaired' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 019-S: VALID REMARK WITH VALID REPAIR STATUS CHANGE - Both in a single record.
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (@dvirDefectId2) in the MyGeotab database.
--   - The subject DVIRDefect (@dvirDefectId2) should be marked as Repaired in the MyGeotab database.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 2 - VALID REMARK WITH VALID REPAIR STATUS CHANGE (Update to Repaired)' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId2' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairStatusRepaired' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 020-S: VALID REMARK WITH VALID REPAIR STATUS CHANGE - Both in a single record.
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (@dvirDefectId3Minor) in the MyGeotab database.
--   - The subject DVIRDefect (@dvirDefectId3Minor) should be marked as NotNecessary in the MyGeotab database.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 3 - VALID REMARK WITH VALID REPAIR STATUS CHANGE (Update to NotNecessary)' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId3Minor' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairStatusNotNecessary' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

-- TEST 021-F: ATTEMPT SECONDARY REPAIR STATUS CHANGE - Try to change the RepairStatus from Repaired to NotNecessary.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
UPDATE _Variables SET IntegerValue = (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) + 1 WHERE Name = '@testCount';
UPDATE _Variables SET TextValue = 'MyGeotab API Adapter - Test ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' - Defect 1 - ATTEMPT SECONDARY REPAIR STATUS CHANGE - Try to change the RepairStatus from Repaired to NotNecessary' WHERE Name = '@remark';
insert into DVIRDefectUpdates (DVIRLogId, DVIRDefectId, RepairDateTime, RepairStatus, RepairUserId, Remark, RemarkDateTime, RemarkUserId, RecordCreationTimeUtc)
	values ((SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1),(SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirDefectId1' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairStatusNotNecessary' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@repairUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remark' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@remarkUserId' LIMIT 1), (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@timeStampUTC' LIMIT 1));

select 'Completed insertion of ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@testCount' LIMIT 1) || ' test records into the DVIRDefectUpdates table for DVIRLogID: ' || (SELECT coalesce(RealValue, IntegerValue, BlobValue, TextValue) FROM _Variables WHERE Name = '@dvirLogId' LIMIT 1);

-- Delete in-memory temp table for variables.
DROP TABLE _Variables;

-- Select all records from the DVIRDefectUpdates table for display in the Data Output tab.
select * from DVIRDefectUpdates;
