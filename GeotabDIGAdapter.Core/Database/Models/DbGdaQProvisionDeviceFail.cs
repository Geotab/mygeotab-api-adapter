using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.Q_ProvisionDevicesFail database table.
    /// </summary>
    [Table("gda.Q_ProvisionDevicesFail")]
    public class DbGdaQProvisionDeviceFail : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.Q_ProvisionDevicesFail";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        public long OriginalQueueId { get; set; }

        public string ThirdPartyId { get; set; }

        public string ErpNo { get; set; }

        public int? HardwareId { get; set; }

        public int ProductId { get; set; }

        public string PromoCode { get; set; }

        public string SubPlan { get; set; }

        public DateTime OriginalRecordLastChangedUtc { get; set; }

        public string FailureReason { get; set; }

        public DateTime RecordCreationTimeUtc { get; set; }
    }
}