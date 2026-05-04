using Dapper.Contrib.Extensions;

namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// A class with properties that map to the columns of the gda.ProvisionedDevices database table.
    /// </summary>
    [Table("gda.ProvisionedDevices")]
    public class DbGdaProvisionedDevice : IDbEntity
    {
        /// <inheritdoc/>
        [Write(false)]
        public string DatabaseTableName => "gda.ProvisionedDevices";

        /// <inheritdoc/>
        [Write(false)]
        public Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        [Key]
        public long id { get; set; }

        public string ThirdPartyId { get; set; }

        public string ErpNo { get; set; }

        public string GeotabSerialNumber { get; set; }

        public bool IsOkayToSendDataToGeotab { get; set; }

        public DateTime? DeviceProvisionedDateTimeUtc { get; set; }

        [ChangeTracker]
        public DateTime RecordLastChangedUtc { get; set; }
    }
}