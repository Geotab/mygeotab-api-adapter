using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityPersisters
{
    /// <summary>
    /// Interface for a generic class with methods involving the persistence of <see cref="IDbEntity"/> entities to a corresponding database table.
    /// </summary>
    public interface IGenericEntityPersister<T> : IEntityPersister<T> where T : IDbEntity
    {
    }
}
