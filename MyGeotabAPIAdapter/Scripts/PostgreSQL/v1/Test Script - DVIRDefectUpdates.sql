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
--      select * from public."DVIRLogs" order by "id" desc limit 1;
-- 4. Set the value of the dvirLogId variable in the declaration section below to the DVIRLog
--      Id obtained in Step 3.
-- 5. Execute the following query to obtain the Ids of the three defects associated with the
--      subject DVIRLog. Substitute <DVIRLogId> with the DVIRLog Id obtained in Step 3:
--      select * from public."DVIRDefects" where "DVIRLogId" = '<DVIRLogId>';
-- 6. Set the values of the dvirDefectId1, dvirDefectId2 and dvirDefectId3Minor variables in
--      the declaration section below to the GeotabId values in the rows returned in Step 5.
--      * NOTE: The Id specifed for dvirDefectId3Minor should be the one for the "minor" defect.
-- 7. Set the remarkUserId and repairUserId to appropriate user Ids (users that have the 
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
do $$ 
declare
   -- *** IMPORTANT ***
   -- Set the values of the following variables based on the instructions above:
   dvirLogId			character varying(50) := 'aA3HWJHWGiUiOIazxfnq0uA';
   dvirDefectId1		character varying(50) := 'ao_Js5b6wwUySrGnv9dSxiw';
   dvirDefectId2		character varying(50) := 'aSGITfEccM0Giet8tP6ATxA';
   dvirDefectId3Minor	character varying(50) := 'a4gWWoUXCjEK5VRLDEcK-gw';
   remarkUserId			character varying(50) := 'b124';
   repairUserId			character varying(50) := 'b124';
   

-- *** DO NOT CHANGE ANYTHING BELOW THIS LINE ***
-- ===========================================================================================
    timeStampUTC timestamp without time zone default timezone('utc', now());
   futureTimeStampUTC timestamp without time zone := timeStampUTC + (59 * interval '1 minute');
   remark text := '';
   repairStatusNotNecessary	character varying(50) := 'NotNecessary';
   repairStatusRepaired	character varying(50) := 'Repaired';
   testCount integer := 0;
   
begin 
-- Delete any existing records from the DVIRDefectUpdates table.
raise notice 'Deleting all existing records from the DVIRDefectUpdates table';
DELETE FROM public."DVIRDefectUpdates";

-- Delete any existing records from the FailedDVIRDefectUpdates table.
raise notice 'Deleting all existing records from the FailedDVIRDefectUpdates table';
DELETE FROM public."FailedDVIRDefectUpdates";

raise notice 'Inserting test records into the DVIRDefectUpdates table for DVIRLogId: % | DVIRDefectId: %', dvirLogId, dvirDefectId1;

-- NOTE: Provided that the values supplied in the input parameters at the top of this script are valid, it is expected that all records
-- added to the DVIRDefectUpdates table below will be removed from the table once processed by the MyGeotab API Adapter.

-- TEST 001-F: NOTHING TO DO - Null values supplied for all Remark and Repair Status fields.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, null, null, null, null, null, null, timeStampUTC);

-- TEST 002-F: INVALID DVIRLogId
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [INVALID DVIRLogId]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES ('TestInvalidDVIRLogId', dvirDefectId1, null, null, null, remark, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 003-F: INVALID DVIRDefectId
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [IdInvalid DVIRDefect]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, 'TestInvalidDVIRDefectId', null, null, null, remark, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 004-F: INVALID REMARK - Remark null.
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - INVALID REMARK - Remark null]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, null, null, null, null, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 005-F: INVALID REMARK - RemarkDateTime null.
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - INVALID REMARK - RemarkDateTime null]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, null, null, null, remark, null, remarkUserId, timeStampUTC);

-- TEST 006-F: INVALID REMARK - RemarkUserId null.
-- Expected Results:
--	 - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - INVALID REMARK - RemarkUserId null]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, null, null, null, remark, timeStampUTC, null, timeStampUTC);

-- TEST 007-F: INVALID REMARK - RemarkDateTime in future.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - INVALID REMARK - RemarkDateTime in future]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, null, null, null, remark, futureTimeStampUTC, remarkUserId, timeStampUTC);

-- TEST 008-F: INVALID REMARK - Invalid RemarkUserId.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - INVALID REMARK - Invalid RemarkUserId]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, null, null, null, remark, timeStampUTC, 'TestInvalidRemarkUserId', timeStampUTC);

-- TEST 009-S: VALID REMARK
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (dvirDefectId1) in the MyGeotab database.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - VALID REMARK]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, null, null, null, remark, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 010-S: VALID REMARK 2
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (dvirDefectId1) in the MyGeotab database.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - VALID REMARK 2]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, null, null, null, remark, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 011-S: VALID REMARK
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (dvirDefectId2) in the MyGeotab database.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 2 - VALID REMARK]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId2, null, null, null, remark, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 012-S: VALID REMARK
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (dvirDefectId3Minor) in the MyGeotab database.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 3 - VALID REMARK]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId3Minor, null, null, null, remark, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 013-F: INVALID REPAIR STATUS CHANGE - RepairStatus Not 'Repaired' or 'NotNecessary'.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, timeStampUTC, 'NotRepaired', repairUserId, null, null, null, timeStampUTC);

-- TEST 014-F: INVALID REPAIR STATUS CHANGE - RepairDateTime null.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, null, repairStatusRepaired, repairUserId, null, null, null, timeStampUTC);

-- TEST 015-F: INVALID REPAIR STATUS CHANGE - RepairStatus null.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, timeStampUTC, null, repairUserId, null, null, null, timeStampUTC);

-- TEST 016-F: INVALID REPAIR STATUS CHANGE - RepairUserId null.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, timeStampUTC, repairStatusRepaired, null, null, null, null, timeStampUTC);

-- TEST 017-F: INVALID REMARK WITH VALID REPAIR STATUS CHANGE - RemarkUserId null
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - INVALID REMARK WITH VALID REPAIR STATUS CHANGE - RemarkUserId null]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, timeStampUTC, repairStatusRepaired, repairUserId, remark, timeStampUTC, null, timeStampUTC);

-- TEST 018-S: VALID REMARK WITH VALID REPAIR STATUS CHANGE - Both in a single record.
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (dvirDefectId1) in the MyGeotab database.
--   - The subject DVIRDefect (dvirDefectId1) should be marked as Repaired in the MyGeotab database.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - VALID REMARK WITH VALID REPAIR STATUS CHANGE (Update to Repaired)]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, timeStampUTC, repairStatusRepaired, repairUserId, remark, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 019-S: VALID REMARK WITH VALID REPAIR STATUS CHANGE - Both in a single record.
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (dvirDefectId2) in the MyGeotab database.
--   - The subject DVIRDefect (dvirDefectId2) should be marked as Repaired in the MyGeotab database.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 2 - VALID REMARK WITH VALID REPAIR STATUS CHANGE (Update to Repaired)]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId2, timeStampUTC, repairStatusRepaired, repairUserId, remark, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 020-S: VALID REMARK WITH VALID REPAIR STATUS CHANGE - Both in a single record.
-- Expected Results:
--   - A new DefectRemark should be added to the subject DVIRDefect (dvirDefectId3Minor) in the MyGeotab database.
--   - The subject DVIRDefect (dvirDefectId3Minor) should be marked as NotNecessary in the MyGeotab database.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 3 - VALID REMARK WITH VALID REPAIR STATUS CHANGE (Update to NotNecessary)]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId3Minor, timeStampUTC, repairStatusNotNecessary, repairUserId, remark, timeStampUTC, remarkUserId, timeStampUTC);

-- TEST 021-F: ATTEMPT SECONDARY REPAIR STATUS CHANGE - Try to change the RepairStatus from Repaired to NotNecessary.
-- Expected Results:
--   - A copy of this record should be written to the FailedDVIRDefects table.
testCount := testCount + 1;
remark := concat('MyGeotab API Adapter - Test ', testCount, ' [Defect 1 - ATTEMPT SECONDARY REPAIR STATUS CHANGE - Try to change the RepairStatus from Repaired to NotNecessary]');
INSERT INTO public."DVIRDefectUpdates" ("DVIRLogId", "DVIRDefectId", "RepairDateTime", "RepairStatus", "RepairUserId", "Remark", "RemarkDateTime", "RemarkUserId", "RecordCreationTimeUtc")
	VALUES (dvirLogId, dvirDefectId1, timeStampUTC, repairStatusNotNecessary, repairUserId, remark, timeStampUTC, remarkUserId, timeStampUTC);

raise notice 'Completed insertion of % test records into the DVIRDefectUpdates table for DVIRLogID: % | dvirDefectId1: %', testCount, dvirLogId, dvirDefectId1;
end $$;

-- Select all records from the DVIRDefectUpdates table for display in the Data Output tab.
SELECT * FROM public."DVIRDefectUpdates";
