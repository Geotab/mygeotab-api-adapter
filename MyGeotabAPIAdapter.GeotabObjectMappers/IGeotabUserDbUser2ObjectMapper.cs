using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="User"/> and <see cref="DbUser2"/> entities.
    /// </summary>
    public interface IGeotabUserDbUser2ObjectMapper : IStatusableGeotabObjectMapper<User, DbUser2>
    {
    }
}
