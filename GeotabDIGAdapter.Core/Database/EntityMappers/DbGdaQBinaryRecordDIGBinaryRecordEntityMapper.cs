using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGBinaryRecord"/> entities from <see cref="DbGdaQBinaryRecord"/> entities.
    /// </summary>
    public class DbGdaQBinaryRecordDIGBinaryRecordEntityMapper : IDbGdaQBinaryRecordDIGBinaryRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGBinaryRecord CreateEntity(DbGdaQBinaryRecord dbGdaQBinaryRecord, string serialNo)
        {
            var digBinaryRecord = new DIGBinaryRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQBinaryRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                Data = dbGdaQBinaryRecord.Data
            };

            return digBinaryRecord;
        }
    }
}
