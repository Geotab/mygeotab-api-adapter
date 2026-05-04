# MyGeotab API Adapter

This repository contains two adapters for integrating with the Geotab platform. Each adapter has its own comprehensive README with architecture overview, configuration reference, operator guide, database schema reference, troubleshooting, and developer guide.

## MyGeotab API Adapter

The [MyGeotab API Adapter](MyGeotabAPIAdapter/README.md) is a free, open source .NET 10.0 (C#) application that downloads data from a MyGeotab database and writes it to a local SQL Server or PostgreSQL database. It uses the [MyGeotab SDK](https://geotab.github.io/sdk/) data feeds to pull the most common data sets incrementally, streaming them into tables that can serve as the foundation for downstream integrations with external systems.

For developers, the solution serves as both an example of proper MyGeotab API integration via data feeds and a ready-to-use starting point for building new integrations with the Geotab platform.

See the **[MyGeotab API Adapter README](MyGeotabAPIAdapter/README.md)** for full documentation.

## Geotab DIG Adapter

The [Geotab DIG Adapter](GeotabDIGAdapter/README.md) is an application that pushes telemetry data *into* MyGeotab via the [Data Intake Gateway (DIG)](https://github.com/Geotab/data-intake-gateway). While the MyGeotab API Adapter pulls data *out of* MyGeotab, the Geotab DIG Adapter enables integrators to stream data from custom (non-Geotab) telematics devices *into* the MyGeotab platform.

The two adapters can be used independently or together. They share the same `geotabadapterdb` database but use separate schemas.

See the **[Geotab DIG Adapter README](GeotabDIGAdapter/README.md)** for full documentation.

## Feedback

Help us prioritize future efforts and better understand how the adapters are used. If you would like to provide any feedback, please feel free to complete the applicable 100% voluntary survey(s):

- [MyGeotab API Adapter - Usage Survey](https://docs.google.com/forms/d/e/1FAIpQLSeIv-6A4Ugu7aIoyJdXrqVWyOF7sB8nuHOV-FDAYqayaPlkJg/viewform?usp=header)
- [Geotab DIG Adapter - Usage Survey](https://docs.google.com/forms/d/e/1FAIpQLSfMMnFxiaTuaw222-3OaA2tOATRDGnQJGA-rrBo48VM51fcRQ/viewform?usp=header)
