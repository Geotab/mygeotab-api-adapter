using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="User"/> and <see cref="DbUser"/> entities.
    /// </summary>
    public interface IGeotabUserDbUserObjectMapper : IStatusableGeotabObjectMapper<User, DbUser>
    {
    }
}
