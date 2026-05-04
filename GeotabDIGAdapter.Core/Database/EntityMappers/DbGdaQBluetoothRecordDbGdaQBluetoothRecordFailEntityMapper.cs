using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DbGdaQBluetoothRecordFail"/> entities from <see cref="DbGdaQBluetoothRecord"/> entities.
    /// </summary>
    public class DbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper : IDbGdaQBluetoothRecordDbGdaQBluetoothRecordFailEntityMapper
    {
        /// <inheritdoc/>
        public DbGdaQBluetoothRecordFail CreateEntity(DbGdaQBluetoothRecord dbGdaQBluetoothRecord, string failureReason)
        {
            var dbGdaQBluetoothRecordFail = new DbGdaQBluetoothRecordFail
            {
                DatabaseWriteOperationType = Common.DatabaseWriteOperationType.Insert,
                OriginalQueueId = dbGdaQBluetoothRecord.id,
                ThirdPartyId = dbGdaQBluetoothRecord.ThirdPartyId,
                DateTime = dbGdaQBluetoothRecord.DateTime,
                Address = dbGdaQBluetoothRecord.Address,
                Data = dbGdaQBluetoothRecord.Data,
                DataType = dbGdaQBluetoothRecord.DataType,
                OriginalRecordLastChangedUtc = dbGdaQBluetoothRecord.RecordLastChangedUtc,
                FailureReason = failureReason,
                RecordCreationTimeUtc = DateTime.UtcNow
            };

            return dbGdaQBluetoothRecordFail;
        }
    }
}
