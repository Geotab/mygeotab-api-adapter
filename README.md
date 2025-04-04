﻿# MyGeotab API Adapter

The MyGeotab API Adapter solution serves as both an example of proper integration via data feeds and the potential foundation for those seeking to develop new integrations with the Geotab platform. It is a collection of .NET 8.0 (C#) Background Services that use the MyGeotab API data feeds to pull the most common data sets from a MyGeotab database and stream the data into tables within a SQL Server or PostgreSQL database; this could account for half the work in terms of a unidirectional integration where the data from the database is further processed for integration into an external system.

- A good overview can be found in the [MyGeotab API Adapter](https://docs.google.com/presentation/d/1PhsDhZwj23i2oWXrqZozf4h0svUEHZLnFXtzMYyk4kQ/edit?usp=sharing) presentation.
- For detailed information, refer to:
	- [MyGeotab API Adapter - Solution and Implementation Guide](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit?usp=sharing)
	- [MyGeotab API Adapter DM2 — Solution and Implementation Guide](https://docs.google.com/document/d/1Y_9FnHPldeX4_aPViUUOi_8y2UJU1lKcfb1SBnu-lj8/edit?usp=sharing) 🆕
- Developers seeking to learn about the API Adapter source code and database may find the [MyGeotab API Adapter - Developer Overview](https://docs.google.com/presentation/d/1agH1x6EYRjNDemzoLixPwPpakwxepAhEJSrktZ5ek_Y/edit?usp=sharing) presentation helpful.
- Is there a Geotab entity type that you would like to add to the API Adapter? Please refer to the [MyGeotab API Adapter — How to Add a Data Feed](https://docs.google.com/document/d/10sGCVsJgYxr7UBxY7lPrDOy4jPzW-bzNEnVfqDVEfBs/edit?usp=sharing) guide for step-by-step instructions.
- Want to access the above materials, but don't have a Gmail address or aren't permitted to use one? No problem - you can [create a Google account without using Gmail](https://accounts.google.com/signup/v2/webcreateaccount?flowName=GlifWebSignIn&flowEntry=SignUp&nogm=true).

## Data Model 2 (DM2) - The Next Evolution
> [!IMPORTANT]
> As of version 3.0, the MyGeotab API Adapter solution has begun its evolution to using a **new data model**. It is expected to take several months from the initial version 3.0 release to port-over the various Geotab entity types that are currently supported. Once complete, the **existing data model and the Data Optimizer will be deprecated** after a reasonable period to provide integrators time to modify any integrations that they need to migrate to the new data model.
> 
> For more information:
> - [IMPORTANT: Version 3.x - Data Model 2 (DM2)](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit?tab=t.0#heading=h.uv8dery26ouj)
> - [Data Optimizer Deprecated (Capabilities Moved to Core API Adapter in Version 3.x)](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit?tab=t.0#heading=h.mtzkh180set0)
> - [MyGeotab API Adapter DM2 — Solution and Implementation Guide](https://docs.google.com/document/d/1Y_9FnHPldeX4_aPViUUOi_8y2UJU1lKcfb1SBnu-lj8/edit?usp=sharing)

## Prerequisites

The solution requires:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or higher. Note: If simply deploying a published release, the deployment package is self-contained (including all dependencies), so .NET version(s) of the host machine should not be a concern.
- The following (included via NuGet packages):
	- Geotab.Checkmate.ObjectModel
	- Dapper
	- FastMember
	- Microsoft.Data.SqlClient
	- NLog.Extensions.Logging
	- Npgsql
	- Polly
- MyGeotab credentials with all “View” clearances enabled on any MyGeotab database with which the MyGeotab API Adapter is to be used. It is recommended that a Service Account be set-up for this purpose. See the [Service Account Guidelines](https://docs.google.com/document/d/1KXJY3S6xyTjp9-qLgxo4PTedQjEuxrqKDlVWgfcC_lc/edit#heading=h.flbpi6nh4xjx) document for more details.
- If **PostgreSQL** is the chosen database provider, access to a PostgreSQL 16 (or greater) server on which the adapter database is deployed.
	- If the adapter and database will reside on separate servers, it may be necessary to ensure that appropriate security and networking steps are undertaken to ensure the ability of the adapter to interact with the database.
	- Although not a strict requirement, it is recommended to have access to a tool such as [pgAdmin](https://www.pgadmin.org/) to view data that the adapter writes to the database.
- If **SQL Server** is the chosen database provider, access to a MS SQL Server instance on which the adapter database is deployed. While developed using SQL Server 2019 Developer (version 15.0.2000.5), given that the solution uses only simple tables and views, it is likely to work on other SQL Server versions without any issues.
	- If the adapter and database will reside on separate servers, it may be necessary to ensure that appropriate security and networking steps are undertaken to ensure the ability of the adapter to interact with the database.
	- Although not a strict requirement, it is recommended to have access to a tool such as [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver15) to view data that the adapter writes to the database.

## Getting started

The [MyGeotab API Adapter](https://docs.google.com/presentation/d/1PhsDhZwj23i2oWXrqZozf4h0svUEHZLnFXtzMYyk4kQ/edit?usp=sharing) presentation and [MyGeotab API Adapter - Solution and Implementation Guide](https://docs.google.com/document/d/12TIgTCuWVF_AYc3evsIms9VOecc1NT4P9Kn-eVg-H7k/edit?usp=sharing) or [MyGeotab API Adapter DM2 — Solution and Implementation Guide](https://docs.google.com/document/d/1Y_9FnHPldeX4_aPViUUOi_8y2UJU1lKcfb1SBnu-lj8/edit?usp=sharing) contain all the information needed to get started with the MyGeotab API Adapter. Information related to the supplemental Data Optimizer can be found in the [MyGeotab API Adapter and Data Optimizer](https://docs.google.com/presentation/d/1PC9Wm73EwuLgBQwxXnH4oiIY5JtfxQAUQTkqdHeDUlA/edit?usp=sharing) presentation and the [MyGeotab API Adapter - Data Optimizer - Solution and Implementation Guide](https://docs.google.com/document/d/1t8AunsFvW7NZtXaQ_9Q85qi5dR1GTfVVTYIcwXoRG1E/edit?usp=sharing).

```shell
> git clone https://github.com/Geotab/mygeotab-api-adapter.git mygeotab-api-adapter
```

# Deploy to Azure

An example process has been developed to facilitate rapid and semi-autonomous deployment of the MyGeotab API Adapter solution to the Microsoft Azure cloud platform. For more information, please refer to the [MyGeotab API Adapter - Guide for Deploying to Microsoft Azure](https://docs.google.com/document/d/1yfZhsy4gFTnRqHDGeo4xgxCft4FPDadSEebqCQSbJ88/edit?usp=sharing) guide. Click the button below to launch the Microsoft Azure Cloud Shell which is required early in the deployment process outlined in the guide.

## Disclaimer

Utilizing the deployment process outlined in this example **may result in charges** incurred for utilization of Microsoft Azure resources. Geotab is not liable under any circumstances for any charges incurred as a result of following this deployment example.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#cloudshell)