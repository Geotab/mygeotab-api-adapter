using MyGeotabAPIAdapter.Database.Models;


namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbLogRecord2"/> and <see cref="DbEntityMetadata2"/> entities.
    /// </summary>
    public interface IDbLogRecord2DbEntityMetadata2EntityMapper : ICreateOnlyEntityMapper<DbLogRecord2, DbEntityMetadata2>
    {
    }
}
