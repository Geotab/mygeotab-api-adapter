using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.Q_GpsRecords database table.
    /// </summary>
    [Table("gda.Q_GpsRecords")]
    public class DbGdaQGpsRecord : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.Q_GpsRecords";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        public string ThirdPartyId { get; set; }

        public DateTime DateTime { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public float? Speed { get; set; }

        public bool? IsGpsValid { get; set; }

        public bool? IsIgnitionOn { get; set; }

        public bool? IsAuxiliary1On { get; set; }

        public bool? IsAuxiliary2On { get; set; }

        public bool? IsAuxiliary3On { get; set; }

        public bool? IsAuxiliary4On { get; set; }

        public bool? IsAuxiliary5On { get; set; }

        public bool? IsAuxiliary6On { get; set; }

        public bool? IsAuxiliary7On { get; set; }

        public bool? IsAuxiliary8On { get; set; }

        public DateTime RecordCreationTimeUtc { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }

        public byte ProcessingStatus { get; set; }

        public DateTime? ProcessingStartTimeUtc { get; set; }

        public byte RetryCount { get; set; }
    }
}
