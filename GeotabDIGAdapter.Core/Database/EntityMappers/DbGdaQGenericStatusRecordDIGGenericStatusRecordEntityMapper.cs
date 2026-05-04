using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.DIGAPI.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// A class that creates <see cref="DIGGenericStatusRecord"/> entities from <see cref="DbGdaQGenericStatusRecord"/> entities.
    /// </summary>
    public class DbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper : IDbGdaQGenericStatusRecordDIGGenericStatusRecordEntityMapper
    {
        /// <inheritdoc/>
        public DIGGenericStatusRecord CreateEntity(DbGdaQGenericStatusRecord dbGdaQGenericStatusRecord, string serialNo)
        {
            var digGenericStatusRecord = new DIGGenericStatusRecord
            {
                DateTime = DateTime.SpecifyKind(dbGdaQGenericStatusRecord.DateTime, DateTimeKind.Utc),
                SerialNo = serialNo,
                Code = dbGdaQGenericStatusRecord.Code,
                Value = dbGdaQGenericStatusRecord.Value
            };

            return digGenericStatusRecord;
        }
    }
}
