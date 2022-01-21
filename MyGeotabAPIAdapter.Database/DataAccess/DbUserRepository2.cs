using MyGeotabAPIAdapter.Database.Models;


namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A repository class that handles database CRUD operations for <see cref="DbUser"/> entities.
    /// </summary>
    public class DbUserRepository2 : BaseRepository2<DbUser>
    {
        public DbUserRepository2(IConnectionContext context) : base(context) { }
    }
}
