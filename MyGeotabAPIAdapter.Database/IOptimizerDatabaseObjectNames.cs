using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Interface for a class that lists names of objects (e.g. tables, views, etc.) in the Optimizer database.
    /// </summary>
    public interface IOptimizerDatabaseObjectNames
    {
        /// <summary>
        /// The name of the database table associated with <see cref="DbBinaryDataT"/> entities.
        /// </summary>
        string DbBinaryDataTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbBinaryTypeT"/> entities.
        /// </summary>
        string DbBinaryTypeTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbControllerT"/> entities.
        /// </summary>
        string DbControllerTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDeviceT"/> entities.
        /// </summary>
        string DbDeviceTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDiagnosticIdT"/> entities.
        /// </summary>
        string DbDiagnosticIdTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDiagnosticT"/> entities.
        /// </summary>
        string DbDiagnosticTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDriverChangeT"/> entities.
        /// </summary>
        string DbDriverChangeTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbDriverChangeTypeT"/> entities.
        /// </summary>
        string DbDriverChangeTypeTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultDataTDriverIdUpdate"/> entities.
        /// </summary>
        string DbFaultDataTDriverIdUpdateTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultDataT"/> entities.
        /// </summary>
        string DbFaultDataTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultDataTLongLatUpdate"/> entities.
        /// </summary>
        string DbFaultDataTLongLatUpdateTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbFaultDataTWithLagLeadLongLat"/> entities.
        /// </summary>
        string DbFaultDataTWithLagLeadLongLatTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbLogRecordT"/> entities.
        /// </summary>
        string DbLogRecordTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbOProcessorTracking"/> entities.
        /// </summary>
        string DbOProcessorTrackingTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusDataTDriverIdUpdate"/> entities.
        /// </summary>
        string DbStatusDataTDriverIdUpdateTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusDataT"/> entities.
        /// </summary>
        string DbStatusDataTTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusDataTLongLatUpdate"/> entities.
        /// </summary>
        string DbStatusDataTLongLatUpdateTableName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbStatusDataTWithLagLeadLongLat"/> entities.
        /// </summary>
        string DbStatusDataTWithLagLeadLongLatStoredProcedureName { get; }

        /// <summary>
        /// The name of the database table associated with <see cref="DbUserT"/> entities.
        /// </summary>
        string DbUserTTableName { get; }

        /// <summary>
        /// The name of a stored procedure that retrieves <see cref="DbFaultDataT"/> entities.
        /// </summary>
        string GetFaultDataTBatchStoredProcedureName { get; }

        /// <summary>
        /// The name of a stored procedure that retrieves <see cref="DbLogRecordT"/> entities.
        /// </summary>
        string GetLogRecordTBatchStoredProcedureName { get; }

        /// <summary>
        /// The name of a stored procedure that retrieves <see cref="DbStatusDataT"/> entities.
        /// </summary>
        string GetStatusDataTBatchStoredProcedureName { get; }

        /// <summary>
        /// The nickname of the Optimizer database. For use in logging.
        /// </summary>
        string OptimizerDatabaseNickname { get; }
    }
}
