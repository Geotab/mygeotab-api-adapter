# MyGeotab API Adapter

The MyGeotab API Adapter is a free and open source application that can be used in Windows or Linux environments. It downloads data from a MyGeotab database and writes it to a local SQL Server or PostgreSQL database. 

For developers, the MyGeotab API Adapter solution serves as both an example of proper integration via data feeds and the potential foundation for those seeking to develop new integrations with the Geotab platform. It is a cross-platform .NET 9.0 (C#) application that uses the MyGeotab API data feeds to pull the most common data sets from a MyGeotab database and stream the data into tables within a SQL Server or PostgreSQL database; this could account for a significant portion of the work in terms of a unidirectional integration where the data from the database is further processed for integration into an external system.

## Getting Started

A good overview can be found in the [MyGeotab API Adapter](https://docs.google.com/presentation/d/1PhsDhZwj23i2oWXrqZozf4h0svUEHZLnFXtzMYyk4kQ/edit?usp=sharing) presentation.

Here are some videos to help get started quickly and augment the documentation below. While SQL Server is the database of choice in these videos, the processes are similar for PostgreSQL, so the videos are still relevant regardless of the database provider chosen.
- ▶️ [How to Download the MyGeotab API Adapter](https://drive.google.com/file/d/18ybU8AdUZLjv4LWG90l-0D4X6-g5E6a9/view?usp=sharing) (3:25)
- ▶️ [How to Set Up the MyGeotab API Adapter Database](https://drive.google.com/file/d/1GgkOSNGG9SvmEs9oyIzVcYYSc6z7HBxc/view?usp=drive_link) (4:20)
- ▶️ [How to Deploy and Configure the MyGeotab API Adapter Application](https://drive.google.com/file/d/1p0t37xHBWudFviYmmV-bWteUAbg77xgH/view?usp=sharing) (8:15)
- ▶️ [How to Start the MyGeotab API Adapter Application](https://drive.google.com/file/d/17ElhV8cPYJbbloXdci98L_8e2KSMW0Lu/view?usp=sharing) (2:15)
- ▶️ [How to Install the MyGeotab API Adapter as a Windows Service](https://drive.google.com/file/d/14CdkaAwkSVwsX5MavN1F71LTVDhOpd2P/view?usp=drive_link) (2:29)
- ▶️ [How to Upgrade the MyGeotab API Adapter](https://drive.google.com/file/d/1eYDU7cw49S2hHYZYfOp9p26Yszq67wz6/view?usp=sharing) (7:11)

For more detailed information, refer to the official guide: 
- [MyGeotab API Adapter DM2 — Solution and Implementation Guide](https://docs.google.com/document/d/1Y_9FnHPldeX4_aPViUUOi_8y2UJU1lKcfb1SBnu-lj8/edit?usp=sharing)

Want to access the above materials, but don't have a Gmail address or aren't permitted to use one? No problem - you can [create a Google account without using Gmail](https://accounts.google.com/signup/v2/webcreateaccount?flowName=GlifWebSignIn&flowEntry=SignUp&nogm=true).

## Prerequisites for Using the MyGeotab API Adapter

The solution requires:
- MyGeotab credentials with all “View” clearances enabled on any MyGeotab database with which the MyGeotab API Adapter is to be used. It is recommended that a Service Account be set-up for this purpose and assigned to the **Company Group**. See the [Service Account Guidelines](https://docs.google.com/document/d/1KXJY3S6xyTjp9-qLgxo4PTedQjEuxrqKDlVWgfcC_lc/edit#heading=h.flbpi6nh4xjx) document for more details.
- If **PostgreSQL** is the chosen database provider, access to a PostgreSQL 16 (or greater) server on which the adapter database is deployed.
	- If the adapter and database will reside on separate servers, it may be necessary to ensure that appropriate security and networking steps are undertaken to ensure the ability of the adapter to interact with the database.
	- Although not a strict requirement, it is recommended to have access to a tool such as [pgAdmin](https://www.pgadmin.org/) to view data that the adapter writes to the database.
- If **SQL Server** is the chosen database provider, access to a MS SQL Server instance on which the adapter database is deployed. While developed using SQL Server 2019 Developer (version 15.0.2000.5), it is likely to work on other SQL Server versions without any issues.
	- Azure SQL Database is not supported due to the adapter database being partitioned. As-is, an **Azure SQL Managed Instance** or **SQL Server on a VM** are the supported options.
	- If the adapter and database will reside on separate servers, it may be necessary to ensure that appropriate security and networking steps are undertaken to ensure the ability of the adapter to interact with the database.
	- Although not a strict requirement, it is recommended to have access to a tool such as [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver15) to view data that the adapter writes to the database.

## Developers

To clone the repository:
```shell
> git clone https://github.com/Geotab/mygeotab-api-adapter.git mygeotab-api-adapter
```

### Pre-requisites for Developers

In addition to the prerequisites for using the MyGeotab API Adapter, developers working with the source code will need to have the [.NET 9.0 SDK](https://dotnet.microsoft.com/download) or higher installed.

## Feedback

Help us prioritize future efforts and better understand how the API Adapter is used! If you would like to provide any feedback about the MyGeotab API Adapter solution, please feel free to complete the 100% voluntary [MyGeotab API Adapter - Usage Survey](https://docs.google.com/forms/d/e/1FAIpQLSeIv-6A4Ugu7aIoyJdXrqVWyOF7sB8nuHOV-FDAYqayaPlkJg/viewform?usp=header).
