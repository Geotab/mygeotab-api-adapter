USE [geotabadapterdb]
GO

/****** Object:  Table [dbo].[DBPartitionInfo2]    Script Date: 2025-01-24 11:46:45 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DBPartitionInfo2](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[InitialMinDateTimeUTC] [datetime2](7) NOT NULL,
	[InitialPartitionInterval] [nvarchar](50) NOT NULL,
	[RecordCreationTimeUtc] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_DBPartitionInfo2] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- ==========================================================================================
-- Description: 
--		Manages FileGroups, files and partitions. FileGroups with associated files are 
--		created on a monthly basis. Partitions are created on a basis defined by the 
--		@PartitionInterval parameter (value can be 'monthly', 'weekly', or 'daily'). The 
--		@MinDateTimeUTC parameter value is used as a starting point the first time this
--		procedure is executed. On subsequent executions, FileGroup, file and partition
--		creation will start from the last existing partition date to ensure no gaps. 
--
--		FileGroups, files and partitions will always be created through until the end of the
--		next calendar month from the DateTime at which this procedure is executed. Executing 
--		this procedure periodically (e.g. daily, weekly, etc.) will ensure that partitions are
--		created before any data needs to be added.
--
-- Notes:
-- 		1: Be sure to set the @filePath variable declared below to the correct data file
--		   path for your environment.
--   	2: Be sure to alter the "USE [geotabadapterdb]" statement above if you have
--         changed the database name to something else.
-- ==========================================================================================
CREATE PROCEDURE [dbo].[spManagePartitions]
    @MinDateTimeUTC DATETIME,
	@PartitionInterval NVARCHAR(10)
AS
BEGIN
	-- Adjust these variable values as needed:
	DECLARE @filePath NVARCHAR(260) = 'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\DATA\';
	DECLARE @fileSizeMB INT = 8;
	DECLARE @fileGrowthMB INT = 64;

	-- Do NOT adjust values of any of the variables below.
    DECLARE @initialMinDateTimeUTC DATETIME;
    DECLARE @initialPartitionInterval NVARCHAR(50);	
	DECLARE	@minDateUTC DATE = @MinDateTimeUTC;
	DECLARE @minDateTimeUTCString NVARCHAR(23) = CONVERT(NVARCHAR(23), @MinDateTimeUTC, 121);
	DECLARE @maxDateUTC DATE = EOMONTH(GETUTCDATE(), 1);
	DECLARE @maxDateUTCString NVARCHAR(23) = CONVERT(NVARCHAR(23), @maxDateUTC, 121);
	DECLARE	@currentDateUTC DATE = @minDateUTC;
	DECLARE	@databaseName NVARCHAR(128) = DB_NAME();
	DECLARE @yearMonthSuffix VARCHAR(7);
	DECLARE @fileGroupNamePrefix NVARCHAR(128) = 'FG_' + @databaseName + '_';
	DECLARE @fileGroupName NVARCHAR(128);
	DECLARE @datafileNameSuffix NVARCHAR(128) = '_DataFile';
	DECLARE @datafileName NVARCHAR(128);
	DECLARE @partitionFunctionName NVARCHAR(128) = 'DateTimePartitionFunction_MyGeotabApiAdapter';
	DECLARE @partitionSchemeName NVARCHAR(128) = 'DateTimePartitionScheme_MyGeotabApiAdapter';
	DECLARE @partitionValue DATE;
    DECLARE @intervalFormat NVARCHAR(10);
    DECLARE @nextIntervalIncrement NVARCHAR(10);
	DECLARE	@sql NVARCHAR(MAX);
	DECLARE @storedProcedureName NVARCHAR(128) = OBJECT_NAME(@@PROCID);
	DECLARE @storedProcedureStart_time DATETIME;
	DECLARE @start_time DATETIME;
	DECLARE @end_time DATETIME;
	DECLARE @start_time_string VARCHAR(30);
	DECLARE @duration_string VARCHAR(30);

	BEGIN TRY
		-- ======================================================================================
		-- Log start of stored procedure execution.
		SET @storedProcedureStart_time = GETDATE();
		SET @start_time = GETDATE();
		SET @start_time_string = CONVERT(VARCHAR, @start_time, 121);
		RAISERROR ('Executing stored procedure ''%s''. Start: %s', 0, 1, @storedProcedureName, @start_time_string) WITH NOWAIT;		
		RAISERROR ('> @MinDateTimeUTC: %s', 0, 1, @minDateTimeUTCString) WITH NOWAIT;
		RAISERROR ('> @maxDateUTC: %s', 0, 1, @maxDateUTCString) WITH NOWAIT;
		RAISERROR ('> @filePath: ''%s''', 0, 1, @filePath) WITH NOWAIT;


	    -- ======================================================================================
		-- STEP 1: Validate input parameter values.
		RAISERROR ('Step 1 [Validating input parameter values]...', 0, 1) WITH NOWAIT;

		-- @MinDateTimeUTC cannot be greater than @maxDateUTC.
		IF @minDateUTC > @maxDateUTC
		BEGIN
			RAISERROR('ERROR: The @MinDateTimeUTC cannot be greater than @maxDateUTC. [@MinDateTimeUTC: %s; @maxDateUTC: %s]', 16, 1, @minDateTimeUTCString, @maxDateUTCString);
			RETURN;
		END

		-- @PartitionInterval must be 'monthly', 'weekly', or 'daily'.
		IF @PartitionInterval NOT IN ('daily', 'weekly', 'monthly')
		BEGIN
			RAISERROR('ERROR: Invalid value for @PartitionInterval. Allowed values: monthly, weekly, daily.', 16, 1);
			RETURN;
		END;
		
        -- Check if the DBPartitionInfo2 table contains any rows. If so, validate inputs against the table.
        IF EXISTS (SELECT 1 FROM [dbo].[DBPartitionInfo2])
        BEGIN
            -- Retrieve the existing metadata.
            SELECT TOP(1) @initialMinDateTimeUTC = InitialMinDateTimeUTC,
                @initialPartitionInterval = InitialPartitionInterval
            FROM [dbo].[DBPartitionInfo2]
            ORDER BY id;

            -- The value supplied to the @MinDateTimeUTC parameter cannot be earlier than the @initialMinDateTimeUTC.
            IF @MinDateTimeUTC < @initialMinDateTimeUTC
            BEGIN
				DECLARE @initialMinDateTimeUTCString NVARCHAR(23) = CONVERT(NVARCHAR(23), @initialMinDateTimeUTC, 121);
 				RAISERROR('ERROR: The @MinDateTimeUTC cannot be greater than @initialMinDateTimeUTC. [@MinDateTimeUTC: %s; @initialMinDateTimeUTC: %s]', 16, 1, @minDateTimeUTCString, @initialMinDateTimeUTCString);				
				RETURN;			
            END

			-- The value supplied to the @PartitionInterval parameter cannot differ from the @InitialPartitionInterval.
            IF @PartitionInterval <> @initialPartitionInterval
            BEGIN
 				RAISERROR('ERROR: The @PartitionInterval cannot differ from @initialPartitionInterval. [@PartitionInterval: %s; @initialPartitionInterval: %s]', 16, 1, @PartitionInterval, @initialPartitionInterval);	
            END
        END
		ELSE
		BEGIN
			-- Insert a row into DBPartitionInfo2 if this is the first time this procedure is being executed..
			INSERT INTO [dbo].[DBPartitionInfo2] (InitialMinDateTimeUTC, InitialPartitionInterval, RecordCreationTimeUtc)
			VALUES (@MinDateTimeUTC, @PartitionInterval, SYSUTCDATETIME());
		END;		

		-- Determine interval settings based on @PartitionInterval.
		SET @nextIntervalIncrement = CASE 
			WHEN @PartitionInterval = 'monthly' THEN 'MONTH'
			WHEN @PartitionInterval = 'weekly' THEN 'WEEK'
			WHEN @PartitionInterval = 'daily' THEN 'DAY'
		END;

		-- Determine interval format based on @nextIntervalIncrement.
		SET @intervalFormat = CASE 
			WHEN @PartitionInterval = 'monthly' THEN 'yyyyMM'
			WHEN @PartitionInterval = 'weekly' THEN 'yyyyMMdd-Wk'
			WHEN @PartitionInterval = 'daily' THEN 'yyyyMMdd'
		END;

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('Step 1 completed. Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


	    -- ======================================================================================
		-- STEP 2: Check for existing partitions and adjust @minDateUTC if needed.
		RAISERROR ('Step 2 [Checking for existing partitions]...', 0, 1) WITH NOWAIT;

		IF EXISTS (
			SELECT 1 
			FROM sys.partition_functions pf
			LEFT JOIN sys.partition_range_values prv
				ON pf.function_id = prv.function_id
			WHERE name = '' + @partitionFunctionName + ''
		)
		BEGIN
			-- Get the latest existing partition date.
			DECLARE @latestExistingPartitionDateUTC DATE;
			SELECT TOP(1) @latestExistingPartitionDateUTC = CONVERT(DATE, prv.value)
			FROM sys.partition_functions pf
			LEFT JOIN sys.partition_range_values prv
				ON pf.function_id = prv.function_id
			WHERE name = '' + @partitionFunctionName + ''
			ORDER BY prv.value DESC;

			-- Set @minDateUTC to be one day greater than the latest existing partition date.
			SET @minDateUTC = DATEADD(DAY, 1, @latestExistingPartitionDateUTC);
			SET @currentDateUTC = @minDateUTC
			DECLARE @latestExistingPartitionDateUTCString VARCHAR(30) = CONVERT(NVARCHAR(23), @latestExistingPartitionDateUTC, 121);
			DECLARE @minDateUTCString VARCHAR(30) = CONVERT(NVARCHAR(23), @minDateUTC, 121);
			RAISERROR ('> Existing partitions found up to ''%s''. Changing @minDateUTC to: ''%s''.', 0, 1, @latestExistingPartitionDateUTCString, @minDateUTCString) WITH NOWAIT;
		END

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('Step 2 completed. Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- Step 3: Create a FileGroup and File for each month in the DateTime range (only for
		-- months where the FileGroup and File have not yet been created).
		RAISERROR ('Step 3 [Creating FileGroups and Files]...', 0, 1) WITH NOWAIT;
		
		-- Adjust @currentDateUTC based on the @PartitionInterval:
		IF @PartitionInterval = 'daily'
			-- Reset @currentDateUTC to the first day of the starting month for daily partitioning.
			SET @currentDateUTC = DATEFROMPARTS(YEAR(@minDateUTC), MONTH(@minDateUTC), 1);
		ELSE IF @PartitionInterval = 'weekly'
			-- Reset @currentDateUTC to the first day of the week for weekly partitioning (ISO 8601 weeks start on Monday).
			SET @currentDateUTC = DATEADD(DAY, 1 - DATEPART(WEEKDAY, @minDateUTC), @minDateUTC);
		ELSE
			-- No @currentDateUTC adjustment needed for monthly partitioning.
			SET @currentDateUTC = @minDateUTC;

		WHILE @currentDateUTC <= @maxDateUTC
		BEGIN
			SET @yearMonthSuffix = FORMAT(@currentDateUTC, 'yyyyMM');
			SET @fileGroupName = @fileGroupNamePrefix + @yearMonthSuffix;
			SET @datafileName = @fileGroupName + @datafileNameSuffix;

			-- If the FileGroup for the subject year and month does not already exist, create it.
			IF NOT EXISTS (SELECT * FROM sys.filegroups WHERE name = '' + @fileGroupName + '')
			BEGIN
				-- Create a new FileGroup for the subject year and month.
				SET @sql = N'ALTER DATABASE ' + QUOTENAME(@databaseName) + N' ADD FILEGROUP ' + QUOTENAME(@fileGroupName);
				RAISERROR ('> Creating FileGroup ''%s''. SQL: %s', 0, 1, @fileGroupName, @sql) WITH NOWAIT;
				EXEC sp_executesql @sql;

				-- Add a data file to the FileGroup.
				SET @sql = N'ALTER DATABASE ' + QUOTENAME(@databaseName) + N' ADD FILE (NAME = ' 
						+ QUOTENAME(@datafileName) + N', FILENAME = ''' 
						+ @filePath + @fileGroupName + N'.ndf'', SIZE = ' + CONVERT(NVARCHAR(10), @fileSizeMB) + 'MB, FILEGROWTH = ' + CONVERT(NVARCHAR(10), @fileGrowthMB) + 'MB) 
						TO FILEGROUP ' + QUOTENAME(@fileGroupName);
				RAISERROR ('> Adding data file ''%s'' to FileGroup ''%s''. SQL: %s', 0, 1, @datafileName, @fileGroupName, @sql) WITH NOWAIT;
				EXEC sp_executesql @sql;
			END

			-- Increment the current date based on the partition interval.
			IF @PartitionInterval = 'daily'
				SET @currentDateUTC = DATEADD(DAY, 1, @currentDateUTC);
			ELSE IF @PartitionInterval = 'weekly'
				SET @currentDateUTC = DATEADD(WEEK, 1, @currentDateUTC);
			ELSE IF @PartitionInterval = 'monthly'
				SET @currentDateUTC = DATEADD(MONTH, 1, @currentDateUTC);
		END

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('Step 3 completed. Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- Step 4: Create the partition function if it doesn't already exist.
		RAISERROR ('Step 4 [Creating partition function if needed]...', 0, 1) WITH NOWAIT;

		-- Reset the date variables for partition function creation.
		SET @CurrentDateUTC = @minDateUTC;

		IF NOT EXISTS (SELECT * FROM sys.partition_functions WHERE name = '' + @partitionFunctionName + '')
		BEGIN
			SET @SQL = N'CREATE PARTITION FUNCTION ' + QUOTENAME(@partitionFunctionName) + ' (DATETIME2(7)) AS RANGE RIGHT FOR VALUES (''' + FORMAT(@currentDateUTC, 'yyyy-MM-dd') + ''')';
			RAISERROR ('> Creating partition function: %s', 0, 1, @SQL) WITH NOWAIT;
			EXEC sp_executesql @SQL;
		END

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('Step 4 completed. Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- Step 5: Create the partition scheme if it doesn't already exist with the initial
		-- mapping to the first FileGroup.
		RAISERROR ('Step 5 [Creating partition scheme if needed]...', 0, 1) WITH NOWAIT;

		IF NOT EXISTS (SELECT * FROM sys.partition_schemes WHERE name = '' + @partitionSchemeName + '')
		BEGIN
			SET @yearMonthSuffix = FORMAT(@currentDateUTC, 'yyyyMM');
			SET @fileGroupName = @fileGroupNamePrefix + @yearMonthSuffix;
			SET @datafileName = @fileGroupName + @datafileNameSuffix;

			SET @SQL = N'CREATE PARTITION SCHEME ' + QUOTENAME(@partitionSchemeName) + 
					   ' AS PARTITION ' + QUOTENAME(@partitionFunctionName) + 
					   ' TO (''PRIMARY'', ''' + @fileGroupName + ''')';
			RAISERROR ('> Creating partition scheme: %s', 0, 1, @SQL) WITH NOWAIT;
			EXEC sp_executesql @SQL;
		END

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('Step 5 completed. Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;


		-- ======================================================================================
		-- Step 6: Create a partition for each interval in the DateTime range.
		RAISERROR ('Step 6 [Creating partitions]...', 0, 1) WITH NOWAIT;

		-- Adjust @currentDateUTC based on the @PartitionInterval:
		IF @PartitionInterval = 'daily'
			-- Reset @currentDateUTC to the first day of the starting month for daily partitioning.
			SET @currentDateUTC = DATEFROMPARTS(YEAR(@minDateUTC), MONTH(@minDateUTC), 1);
		ELSE IF @PartitionInterval = 'weekly'
			-- Reset @currentDateUTC to the first day of the week for weekly partitioning (ISO 8601 weeks start on Monday).
			SET @currentDateUTC = DATEADD(DAY, 1 - DATEPART(WEEKDAY, @minDateUTC), @minDateUTC);
		ELSE
			-- No @currentDateUTC adjustment needed for monthly partitioning.
			SET @currentDateUTC = @minDateUTC;

		WHILE @currentDateUTC <= @maxDateUTC
		BEGIN
			-- Calculate partition value and corresponding FileGroup.
			SET @partitionValue = @currentDateUTC;
			SET @yearMonthSuffix = FORMAT(@currentDateUTC, 'yyyyMM');
			SET @fileGroupName = @fileGroupNamePrefix + @yearMonthSuffix;

			-- If the partition for the subject date does not already exist, create it.
			IF NOT EXISTS (SELECT * FROM sys.partition_range_values WHERE value = @partitionValue)
			BEGIN
				-- Alter the partition scheme to map the partition to the correct FileGroup before the split.
				SET @SQL = N'ALTER PARTITION SCHEME ' + QUOTENAME(@partitionSchemeName) + 
						   ' NEXT USED ' + QUOTENAME(@fileGroupName);
				RAISERROR ('> Mapping partition scheme to FileGroup: %s', 0, 1, @SQL) WITH NOWAIT;
				EXEC sp_executesql @SQL;

				-- Alter the partition function to add a new partition for the subject date.
				SET @SQL = N'ALTER PARTITION FUNCTION ' + QUOTENAME(@partitionFunctionName) + 
						   '() SPLIT RANGE (''' + CONVERT(NVARCHAR, @partitionValue, 120) + N''')';
				RAISERROR ('> Adding new partition: %s', 0, 1, @SQL) WITH NOWAIT;
				EXEC sp_executesql @SQL;
			END

            -- Increment the current date based on the chosen interval.
            SET @currentDateUTC = 
                CASE 
                    WHEN @nextIntervalIncrement = 'MONTH' THEN DATEADD(MONTH, 1, @currentDateUTC)
                    WHEN @nextIntervalIncrement = 'WEEK' THEN DATEADD(WEEK, 1, @currentDateUTC)
                    WHEN @nextIntervalIncrement = 'DAY' THEN DATEADD(DAY, 1, @currentDateUTC)
                END;			
		END

		-- Log.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @start_time, @end_time));
		RAISERROR ('Step 6 completed. Duration: %s milliseconds', 0, 1, @duration_string) WITH NOWAIT;
		SET @start_time = @end_time;
		
		
		-- ======================================================================================
		-- Log end of stored procedure execution.
		SET @end_time = GETDATE();
		SET @duration_string = CONVERT(VARCHAR, DATEDIFF(MILLISECOND, @storedProcedureStart_time, @end_time));
		RAISERROR ('Stored procedure ''%s'' executed successfully. Total Duration: %s milliseconds', 0, 1, @storedProcedureName, @duration_string) WITH NOWAIT;		
	END TRY
	BEGIN CATCH
	    -- Rethrow the error
        THROW;
	END CATCH
END;
GO
