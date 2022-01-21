using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbLogRecord"/> entities.
    /// </summary>
    public class DbLogRecordRepository2 : BaseRepository2<DbLogRecord>
    {
        public DbLogRecordRepository2(IConnectionContext context) : base(context) { }
    }
}
