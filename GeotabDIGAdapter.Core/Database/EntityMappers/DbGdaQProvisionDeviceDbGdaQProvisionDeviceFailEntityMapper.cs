using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQProvisionDeviceFail"/> entities from <see cref="DbGdaQProvisionDevice"/> entities.
    /// </summary>
    public class DbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper : IDbGdaQProvisionDeviceDbGdaQProvisionDeviceFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQProvisionDeviceFail CreateEntity(DbGdaQProvisionDevice dbGdaQProvisionDevice, string failureReason)
        {
            var dbGdaQProvisionDeviceFail = new DbGdaQProvisionDeviceFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                ErpNo = dbGdaQProvisionDevice.ErpNo,
                FailureReason = failureReason,
                HardwareId = dbGdaQProvisionDevice.HardwareId,
                OriginalQueueId = dbGdaQProvisionDevice.id,
                OriginalRecordLastChangedUtc = dbGdaQProvisionDevice.RecordLastChangedUtc,
                ProductId = dbGdaQProvisionDevice.ProductId,
                PromoCode = dbGdaQProvisionDevice.PromoCode,
                RecordCreationTimeUtc = DateTime.UtcNow,
                SubPlan = dbGdaQProvisionDevice.SubPlan,
                ThirdPartyId = dbGdaQProvisionDevice.ThirdPartyId
            };

            return dbGdaQProvisionDeviceFail;
        }
    }
}