# MyGeotab API Adapter

The MyGeotab API Adapter solution serves as both an example of proper integration via data feeds and the potential foundation for those seeking to develop new integrations with the Geotab platform. It is a .NET 6.0 (C#) Worker Service that uses the MyGeotab API data feeds to pull the most common data sets from a MyGeotab database and stream the data into tables within a PostgreSQL, SQL Server, Oracle or SQLite database; this could account for half the work in terms of a unidirectional integration where the data from the database is further processed for integration into an external system.

- A good overview can be found in the [MyGeotab API Adapter](https://docs.google.com/presentation/d/1PhsDhZwj23i2oWXrqZozf4h0svUEHZLnFXtzMYyk4kQ/edit?usp=sharing) presentation.
- For detailed information, refer to the [MyGeotab API Adapter - Solution and Implementation Guide](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit?usp=sharing).
- Want to access the above materials, but don't have a Gmail address or aren't permitted to use one? No problem - you can [create a Google account without using Gmail](https://accounts.google.com/signup/v2/webcreateaccount?flowName=GlifWebSignIn&flowEntry=SignUp&nogm=true).

## Data Optimizer - Taking the Adapter to the Next Level!

As detailed in the [Database Maintenance](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit#heading=h.ki9kegy47bi5) section of main guide, the adapter database has been designed as a staging database, serving as an intermediary between the Geotab platform and the final repository where the extracted data will ultimately be stored for further processing and consumption by downstream applications.

The **Data Optimizer** is a second application that takes the adapter solution to the next level, following the [Suggested Strategy](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit#heading=h.aqakdi5gf7v6) outlined in the main guide. It migrates data from the adapter database into an “optimizer database” which can then be queried directly by applications. The Data Optimizer includes additional services that enhance the data and overcome certain challenges such as linking data points with different timestamps from different tables. For example, the StatusData and FaultData tables have added Latitude, Longitude, Speed, Bearing and Direction columns that can optionally be populated using LogRecords and interpolation techniques.

Full details pertaining to the Data Optimizer can be found in the [MyGeotab API Adapter - Data Optimizer - Solution and Implementation Guide](https://docs.google.com/document/d/1t8AunsFvW7NZtXaQ_9Q85qi5dR1GTfVVTYIcwXoRG1E/edit?usp=sharing).

## Prerequisites

The solution requires:

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download) or higher
- The following (included via NuGet packages):
	- Geotab.Checkmate.ObjectModel
	- Dapper
	- Microsoft.Data.SqlClient
	- NLog.Extensions.Logging
	- Npgsql
	- Oracle.ManagedDataAccess.Core
	- System.Data.SQLite.Core
- MyGeotab credentials with all “View” clearances enabled on any MyGeotab database with which the MyGeotab API Adapter is to be used. It is recommended that a Service Account be set-up for this purpose. See the [Service Account Guidelines](https://docs.google.com/document/d/1KXJY3S6xyTjp9-qLgxo4PTedQjEuxrqKDlVWgfcC_lc/edit#heading=h.flbpi6nh4xjx) document for more details.
- If **PostgreSQL** is the chosen database provider, access to a PostgreSQL 11 (or greater) server on which the adapter database is deployed.
	- If the adapter and database will reside on separate servers, it may be necessary to ensure that appropriate security and networking steps are undertaken to ensure the ability of the adapter to interact with the database.
	- Although not a strict requirement, it is recommended to have access to a tool such as [pgAdmin](https://www.pgadmin.org/) to view data that the adapter writes to the database.
- If **SQL Server** is the chosen database provider, access to a MS SQL Server instance on which the adapter database is deployed. While developed using SQL Server 2019 Developer (version 15.0.2000.5), given that the solution uses only simple tables and views, it is likely to work on other SQL Server versions without any issues.
	- If the adapter and database will reside on separate servers, it may be necessary to ensure that appropriate security and networking steps are undertaken to ensure the ability of the adapter to interact with the database.
	- Although not a strict requirement, it is recommended to have access to a tool such as [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver15) to view data that the adapter writes to the database.
- If **Oracle** is the chosen database provider, access to an Oracle instance on which the adapter database is deployed. While developed using Oracle Database XE (18c), given that the solution uses only simple tables and views, it is likely to work on other Oracle versions without any issues.
- If **SQLite** is the chosen database provider, the database (GeotabAdapterDB.db) is already included along with the source code. See [SQLite](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit#heading=h.51wefejcn2s3) for more information. SQLite version 3.27.2 was used to create the database.
	- It is recommended to use the [DB Browser for SQLite](https://sqlitebrowser.org/) to view data that the adapter writes to the database.

## Getting started

The [MyGeotab API Adapter](https://docs.google.com/presentation/d/1PhsDhZwj23i2oWXrqZozf4h0svUEHZLnFXtzMYyk4kQ/edit?usp=sharing) presentation and [MyGeotab API Adapter - Solution and Implementation Guide](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit?usp=sharing) contain all the information needed to get started with the MyGeotab API Adapter. Information related to the supplemental Data Optimizer can be found in the [MyGeotab API Adapter - Data Optimizer - Solution and Implementation Guide](https://docs.google.com/document/d/1t8AunsFvW7NZtXaQ_9Q85qi5dR1GTfVVTYIcwXoRG1E/edit?usp=sharing).

```shell
> git clone https://github.com/Geotab/mygeotab-api-adapter.git mygeotab-api-adapter
```

# Deploy to Azure

An example process has been developed to facilitate rapid and semi-autonomous deployment of the MyGeotab API Adapter solution to the Microsoft Azure cloud platform. For more information, please refer to the [MyGeotab API Adapter - Guide for Deploying to Microsoft Azure](https://docs.google.com/document/d/1yfZhsy4gFTnRqHDGeo4xgxCft4FPDadSEebqCQSbJ88/edit?usp=sharing) guide. Click the button below to launch the Microsoft Azure Cloud Shell which is required early in the deployment process outlined in the guide.

## Disclaimer

Utilizing the deployment process outlined in this example **may result in charges** incurred for utilization of Microsoft Azure resources. Geotab is not liable under any circumstances for any charges incurred as a result of following this deployment example.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#cloudshell)