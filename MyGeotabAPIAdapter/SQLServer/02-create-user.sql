USE [master];
GO

CREATE LOGIN [geotabadapter_client] WITH
    PASSWORD = N'$(DB_PASSWORD)',
    DEFAULT_DATABASE = [geotabadapterdb],
    DEFAULT_LANGUAGE = [us_english],
    CHECK_EXPIRATION = OFF,
    CHECK_POLICY = OFF;
GO

USE [geotabadapterdb];
GO

CREATE USER [geotabadapter_client] FOR LOGIN [geotabadapter_client] WITH DEFAULT_SCHEMA = [dbo];
GO

ALTER ROLE [db_datareader] ADD MEMBER [geotabadapter_client];
GO

ALTER ROLE [db_datawriter] ADD MEMBER [geotabadapter_client];
GO