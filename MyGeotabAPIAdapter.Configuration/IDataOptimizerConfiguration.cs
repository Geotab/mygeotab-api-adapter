using System;

namespace MyGeotabAPIAdapter.Configuration
{
    /// <summary>
    /// Interface for a class that reads (from appsettings.json) and stores application configuration settings. Intended for association with the MyGeotabAPIAdapter.DataOptimizer project.
    /// </summary>
    public interface IDataOptimizerConfiguration
    {
        /// <summary>
        /// The connection string used to access the MyGeotabAPIAdapter database.
        /// </summary>
        string AdapterDatabaseConnectionString { get; }

        /// <summary>
        /// A string representation of the <see cref="MyGeotabAPIAdapter.Database.ConnectionInfo.DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instances for the MyGeotabAPIAdapter database.
        /// </summary>
        string AdapterDatabaseProviderType { get; }

        /// <summary>
        /// The number of records to process per batch when loading BinaryDatas from the Adapter database into the Optimizer database.
        /// </summary>
        int BinaryDataProcessorBatchSize { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the BinaryDataProcessor. Only used if <see cref="BinaryDataProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime BinaryDataProcessorDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="BinaryDataProcessorDailyStartTimeUTC"/> in order to calculate the daily stop time for the BinaryDataProcessor. Only used if <see cref="BinaryDataProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int BinaryDataProcessorDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the BinaryDataProcessor and the next.
        /// </summary>
        int BinaryDataProcessorExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the BinaryDataProcessor.
        /// </summary>
        OperationMode BinaryDataProcessorOperationMode { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the DeviceProcessor. Only used if <see cref="DeviceProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime DeviceProcessorDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="DeviceProcessorDailyStartTimeUTC"/> in order to calculate the daily stop time for the DeviceProcessor. Only used if <see cref="DeviceProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int DeviceProcessorDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the DeviceProcessor and the next.
        /// </summary>
        int DeviceProcessorExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the DeviceProcessor.
        /// </summary>
        OperationMode DeviceProcessorOperationMode { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the DiagnosticProcessor. Only used if <see cref="DiagnosticProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime DiagnosticProcessorDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="DiagnosticProcessorDailyStartTimeUTC"/> in order to calculate the daily stop time for the DiagnosticProcessor. Only used if <see cref="DiagnosticProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int DiagnosticProcessorDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the DiagnosticProcessor and the next.
        /// </summary>
        int DiagnosticProcessorExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the DiagnosticProcessor.
        /// </summary>
        OperationMode DiagnosticProcessorOperationMode { get; }

        /// <summary>
        /// Indicates whether machine name validation should be disabled. 
        /// </summary>
        bool DisableMachineNameValidation { get; }

        /// <summary>
        /// The number of records to process per batch when loading DriverChanges from the Adapter database into the Optimizer database.
        /// </summary>
        int DriverChangeProcessorBatchSize { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the DriverChangeProcessor. Only used if <see cref="DriverChangeProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime DriverChangeProcessorDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="DriverChangeProcessorDailyStartTimeUTC"/> in order to calculate the daily stop time for the UserProcessor. Only used if <see cref="DriverChangeProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int DriverChangeProcessorDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the DriverChangeProcessor and the next.
        /// </summary>
        int DriverChangeProcessorExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the DriverChangeProcessor.
        /// </summary>
        OperationMode DriverChangeProcessorOperationMode { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.BinaryDataProcessor"/> should be enabled. 
        /// </summary>
        bool EnableBinaryDataProcessor { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.DeviceProcessor"/> should be enabled. 
        /// </summary>
        bool EnableDeviceProcessor { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.DiagnosticProcessor"/> should be enabled. 
        /// </summary>
        bool EnableDiagnosticProcessor { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.DriverChangeProcessor"/> should be enabled. 
        /// </summary>
        bool EnableDriverChangeProcessor { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.FaultDataOptimizer"/> should be enabled. 
        /// </summary>
        bool EnableFaultDataOptimizer { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.FaultDataProcessor"/> should be enabled. 
        /// </summary>
        bool EnableFaultDataProcessor { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.LogRecordProcessor"/> should be enabled. 
        /// </summary>
        bool EnableLogRecordProcessor { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.StatusDataOptimizer"/> should be enabled. 
        /// </summary>
        bool EnableStatusDataOptimizer { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.StatusDataProcessor"/> should be enabled. 
        /// </summary>
        bool EnableStatusDataProcessor { get; }

        /// <summary>
        /// Indicates whether the <see cref="MyGeotabAPIAdapter.DataOptimizer.UserProcessor"/> should be enabled. 
        /// </summary>
        bool EnableUserProcessor { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the FaultDataOptimizer. Only used if <see cref="FaultDataOptimizerOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime FaultDataOptimizerDailyStartTimeUTC { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily stop time for the FaultDataOptimizer. Only used if <see cref="FaultDataOptimizerOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        /// 

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="FaultDataOptimizerDailyStartTimeUTC"/> in order to calculate the daily stop time for the FaultDataOptimizer. Only used if <see cref="FaultDataOptimizerOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int FaultDataOptimizerDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the FaultDataOptimizer and the next.
        /// </summary>
        int FaultDataOptimizerExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the FaultDataOptimizer.
        /// </summary>
        OperationMode FaultDataOptimizerOperationMode { get; }

        /// <summary>
        /// Indicates the number of directions on the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see> to use when returning compass directions associated with specific bearing values. Determines the <see cref="MyGeotabAPIAdapter.Geospatial.CompassRoseType"/> that will be used. This property is only used if <see cref="FaultDataOptimizerPopulateLongitudeLatitude"/> and <see cref="FaultDataOptimizerPopulateDirection"/> are both set to <c>true</c>.
        /// </summary>
        int FaultDataOptimizerNumberOfCompassDirections { get; }

        /// <summary>
        /// Indicates whether the <c>Bearing</c> column in the <c>FaultDataT</c> table should be populated. This property is only used if <see cref="FaultDataOptimizerPopulateLongitudeLatitude"/> is set to <c>true</c>.
        /// </summary>
        bool FaultDataOptimizerPopulateBearing { get; }

        /// <summary>
        /// Indicates whether the <c>Direction</c> column in the <c>FaultDataT</c> table should be populated. This property is only used if <see cref="FaultDataOptimizerPopulateLongitudeLatitude"/> is set to <c>true</c>.
        /// </summary>
        bool FaultDataOptimizerPopulateDirection { get; }

        /// <summary>
        /// Indicates whether the <c>DriverId</c> column in the <c>FaultDataT</c> table should be populated. 
        /// </summary>
        bool FaultDataOptimizerPopulateDriverId { get; }

        /// <summary>
        /// Indicates whether the <c>Longitude</c> and <c>Latitude</c> columns in the <c>FaultDataT</c> table should be populated. 
        /// </summary>
        bool FaultDataOptimizerPopulateLongitudeLatitude { get; }

        /// <summary>
        /// Indicates whether the <c>Speed</c> column in the <c>FaultDataT</c> table should be populated. This property is only used if <see cref="FaultDataOptimizerPopulateLongitudeLatitude"/> is set to <c>true</c>.
        /// </summary>
        bool FaultDataOptimizerPopulateSpeed { get; }

        /// <summary>
        /// The number of records to process per batch when loading FaultDatas from the Adapter database into the Optimizer database.
        /// </summary>
        int FaultDataProcessorBatchSize { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the FaultDataProcessor. Only used if <see cref="FaultDataProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime FaultDataProcessorDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="FaultDataProcessorDailyStartTimeUTC"/> in order to calculate the daily stop time for the UserProcessor. Only used if <see cref="FaultDataProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int FaultDataProcessorDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the FaultDataProcessor and the next.
        /// </summary>
        int FaultDataProcessorExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the FaultDataProcessor.
        /// </summary>
        OperationMode FaultDataProcessorOperationMode { get; }

        /// <summary>
        /// The number of records to process per batch when loading LogRecords from the Adapter database into the Optimizer database.
        /// </summary>
        int LogRecordProcessorBatchSize { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the LogRecordProcessor. Only used if <see cref="LogRecordProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime LogRecordProcessorDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="LogRecordProcessorDailyStartTimeUTC"/> in order to calculate the daily stop time for the LogRecordProcessor. Only used if <see cref="LogRecordProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int LogRecordProcessorDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the LogRecordProcessor and the next.
        /// </summary>
        int LogRecordProcessorExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the LogRecordProcessor.
        /// </summary>
        OperationMode LogRecordProcessorOperationMode { get; }

        /// <summary>
        /// The connection string used to access the MyGeotabAPIAdapter Data Optimizer database.
        /// </summary>
        string OptimizerDatabaseConnectionString { get; }

        /// <summary>
        /// A string representation of the <see cref="MyGeotabAPIOptimizer.Database.ConnectionInfo.DataAccessProviderType"/> to be used when creating <see cref="System.Data.IDbConnection"/> instancesfor the MyGeotabAPIAdapter Data Optimizer database.
        /// </summary>
        string OptimizerDatabaseProviderType { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the StatusDataOptimizer. Only used if <see cref="StatusDataOptimizerOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime StatusDataOptimizerDailyStartTimeUTC { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily stop time for the StatusDataOptimizer. Only used if <see cref="StatusDataOptimizerOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        /// 

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="StatusDataOptimizerDailyStartTimeUTC"/> in order to calculate the daily stop time for the StatusDataOptimizer. Only used if <see cref="StatusDataOptimizerOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int StatusDataOptimizerDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the StatusDataOptimizer and the next.
        /// </summary>
        int StatusDataOptimizerExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the StatusDataOptimizer.
        /// </summary>
        OperationMode StatusDataOptimizerOperationMode { get; }

        /// <summary>
        /// Indicates the number of directions on the <see href="https://en.wikipedia.org/wiki/Compass_rose">compass rose</see> to use when returning compass directions associated with specific bearing values. Determines the <see cref="MyGeotabAPIAdapter.Geospatial.CompassRoseType"/> that will be used. This property is only used if <see cref="StatusDataOptimizerPopulateLongitudeLatitude"/> and <see cref="StatusDataOptimizerPopulateDirection"/> are both set to <c>true</c>.
        /// </summary>
        int StatusDataOptimizerNumberOfCompassDirections { get; }

        /// <summary>
        /// Indicates whether the <c>Bearing</c> column in the <c>StatusDataT</c> table should be populated. This property is only used if <see cref="StatusDataOptimizerPopulateLongitudeLatitude"/> is set to <c>true</c>.
        /// </summary>
        bool StatusDataOptimizerPopulateBearing { get; }

        /// <summary>
        /// Indicates whether the <c>Direction</c> column in the <c>StatusDataT</c> table should be populated. This property is only used if <see cref="StatusDataOptimizerPopulateLongitudeLatitude"/> is set to <c>true</c>.
        /// </summary>
        bool StatusDataOptimizerPopulateDirection { get; }

        /// <summary>
        /// Indicates whether the <c>DriverId</c> column in the <c>StatusDataT</c> table should be populated. 
        /// </summary>
        bool StatusDataOptimizerPopulateDriverId { get; }

        /// <summary>
        /// Indicates whether the <c>Longitude</c> and <c>Latitude</c> columns in the <c>StatusDataT</c> table should be populated. 
        /// </summary>
        bool StatusDataOptimizerPopulateLongitudeLatitude { get; }

        /// <summary>
        /// Indicates whether the <c>Speed</c> column in the <c>StatusDataT</c> table should be populated. This property is only used if <see cref="StatusDataOptimizerPopulateLongitudeLatitude"/> is set to <c>true</c>.
        /// </summary>
        bool StatusDataOptimizerPopulateSpeed { get; }

        /// <summary>
        /// The number of records to process per batch when loading StatusDatas from the Adapter database into the Optimizer database.
        /// </summary>
        int StatusDataProcessorBatchSize { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the StatusDataProcessor. Only used if <see cref="StatusDataProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime StatusDataProcessorDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="StatusDataProcessorDailyStartTimeUTC"/> in order to calculate the daily stop time for the UserProcessor. Only used if <see cref="StatusDataProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int StatusDataProcessorDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the StatusDataProcessor and the next.
        /// </summary>
        int StatusDataProcessorExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the StatusDataProcessor.
        /// </summary>
        OperationMode StatusDataProcessorOperationMode { get; }

        /// <summary>
        /// The maximum number of seconds that a database <see cref="System.Threading.Tasks.Task"/> or batch thereof can take to be completed before it is deemed that there is a database connectivity issue and a <see cref="Database.DatabaseConnectionException"/> will be thrown.
        /// </summary>
        int TimeoutSecondsForDatabaseTasks { get; }

        /// <summary>
        /// The <see cref="DateTime"/> of which the time of day portion will be used as the daily start time for the UserProcessor. Only used if <see cref="UserProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        DateTime UserProcessorDailyStartTimeUTC { get; }

        /// <summary>
        /// The number of seconds to add to the <see cref="DateTime.TimeOfDay"/> of the <see cref="UserProcessorDailyStartTimeUTC"/> in order to calculate the daily stop time for the UserProcessor. Only used if <see cref="UserProcessorOperationMode"/> is set to <see cref="OperationMode.Scheduled"/>.
        /// </summary>
        int UserProcessorDailyRunTimeSeconds { get; }

        /// <summary>
        /// The minimum number of seconds to wait between the start of one execution iteration of the UserProcessor and the next.
        /// </summary>
        int UserProcessorExecutionIntervalSeconds { get; }

        /// <summary>
        /// The operation mode to be used by the UserProcessor.
        /// </summary>
        OperationMode UserProcessorOperationMode { get; }

        /// <summary>
        /// Validates the configuration object values and uses those values to set properties of the <see cref="IDataOptimizerConfiguration"/>.
        /// </summary>
        void ProcessConfigItems();
    }
}
