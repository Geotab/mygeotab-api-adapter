using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Interface for a class that lists names of objects (e.g. tables, views, etc.) in the Adapter database.
    /// </summary>
    public interface IAdapterDatabaseObjectNames
    {
        /// <summary>
        /// The nickname of the Adapter database. For use in logging.
        /// </summary>
        string AdapterDatabaseNickname { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbBinaryData"/> entities.
        /// </summary>
        string DbBinaryDataTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbConfigFeedVersion"/> entities.
        /// </summary>
        string DbConfigFeedVersionsTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDevice"/> entities.
        /// </summary>
        string DbDeviceTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDiagnostic"/> entities.
        /// </summary>
        string DbDiagnosticTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDriverChange"/> entities.
        /// </summary>
        string DbDriverChangeTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDutyStatusAvailability"/> entities.
        /// </summary>
        string DbDutyStatusAvailabilityTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRDefect"/> entities.
        /// </summary>
        string DbDVIRDefectTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRDefectRemark"/> entities.
        /// </summary>
        string DbDVIRDefectRemarkTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRDefectUpdate"/> entities.
        /// </summary>
        string DbDVIRDefectUpdatesTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDVIRLog"/> entities.
        /// </summary>
        string DbDVIRLogTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbExceptionEvent"/> entities.
        /// </summary>
        string DbExceptionEventTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFailedDVIRDefectUpdate"/> entities.
        /// </summary>
        string DbFailedDVIRDefectUpdatesTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultData"/> entities.
        /// </summary>
        string DbFaultDataTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbLogRecord"/> entities.
        /// </summary>
        string DbLogRecordTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbRule"/> entities.
        /// </summary>
        string DbRuleTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusData"/> entities.
        /// </summary>
        string DbStatusDataTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbTrip"/> entities.
        /// </summary>
        string DbTripTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbUser"/> entities.
        /// </summary>
        string DbUserTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbZone"/> entities.
        /// </summary>
        string DbZoneTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbZoneType"/> entities.
        /// </summary>
        string DbZoneTypeTableName { get; }
    }
}
