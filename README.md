# MyGeotab API Adapter

The MyGeotab API Adapter solution serves as both an example of proper integration via data feeds and the potential foundation for those seeking to develop new integrations with the Geotab platform. It is a .NET Core (C#) Worker Service that uses the MyGeotab API data feeds to pull the most common data sets from a MyGeotab database and stream the data into tables within a PostgreSQL, SQL Server or SQLite database; this could account for half the work in terms of a unidirectional integration where the data from the database is further processed for integration into an external system.

For detailed information, refer to the [MyGeotab API Adapter - Solution and Implementation Guide](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit?usp=sharing).

## Disclaimer

This tool is not intended for use by U.S. Federal Government customers or otherwise in the FedRamp environment.

## Prerequisites

The solution requires:

- [.Net Core 3.1 SDK](https://dot.net/core) or higher
- The following (included via NuGet packages):
	- Geotab.Checkmate.ObjectModel
	- Dapper
	- Microsoft.Data.SqlClient
	- NLog.Extensions.Logging
	- Npgsql
	- System.Data.SQLite.Core
- MyGeotab credentials with all “View” clearances enabled on any MyGeotab database with which the MyGeotab API Adapter is to be used. It is recommended that a Service Account be set-up for this purpose. See the [Service Account Guidelines](https://docs.google.com/document/d/1KXJY3S6xyTjp9-qLgxo4PTedQjEuxrqKDlVWgfcC_lc/edit#heading=h.flbpi6nh4xjx) document for more details.
- If **PostgreSQL** is the chosen database provider, access to a PostgreSQL 11 (or greater) server on which the adapter database is deployed.
	- If the adapter and database will reside on separate servers, it may be necessary to ensure that appropriate security and networking steps are undertaken to ensure the ability of the adapter to interact with the database.
	- Although not a strict requirement, it is recommended to have access to a tool such as [pgAdmin](https://www.pgadmin.org/) to view data that the adapter writes to the database.
- If **SQL Server** is the chosen database provider, access to a MS SQL Server instance on which the adapter database is deployed. While developed using SQL Server 2019 Developer (version 15.0.2000.5), given that the solution uses only simple tables and views, it is likely to work on other SQL Server versions without any issues.
	- If the adapter and database will reside on separate servers, it may be necessary to ensure that appropriate security and networking steps are undertaken to ensure the ability of the adapter to interact with the database.
	- Although not a strict requirement, it is recommended to have access to a tool such as [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver15) to view data that the adapter writes to the database.
- If **SQLite** is the chosen database provider, the database (GeotabAdapterDB.db) is already included along with the source code. See [SQLite](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit#heading=h.51wefejcn2s3) for more information. SQLite version 3.27.2 was used to create the database.
	- It is recommended to use the [DB Browser for SQLite](https://sqlitebrowser.org/) to view data that the adapter writes to the database.

## Getting started

The [MyGeotab API Adapter - Solution and Implementation Guide](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit?usp=sharing) contains all the information needed to get started.

```shell
> git clone https://github.com/Geotab/mygeotab-api-adapter.git mygeotab-api-adapter
```