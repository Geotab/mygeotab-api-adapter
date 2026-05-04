using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.Q_ProvisionDevices database table.
    /// </summary>
    [Table("gda.Q_ProvisionDevices")]
    public class DbGdaQProvisionDevice : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.Q_ProvisionDevices";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        public string ThirdPartyId { get; set; }

        public string ErpNo { get; set; }

        public int? HardwareId { get; set; }

        public int ProductId { get; set; }

        public string PromoCode { get; set; }

        public string SubPlan { get; set; }

        public DateTime RecordCreationTimeUtc { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }

        public byte ProcessingStatus { get; set; }

        public DateTime? ProcessingStartTimeUtc { get; set; }

        public byte RetryCount { get; set; }
    }
}