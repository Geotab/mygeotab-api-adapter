using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbUser"/> and <see cref="DbUserT"/> entities.
    /// </summary>
    public interface IDbUserDbUserTEntityMapper : IEntityMapper<DbUser,DbUserT>
    {
    }
}
