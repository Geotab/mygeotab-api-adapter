using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="User"/> and <see cref="DbUser"/> entities.
    /// </summary>
    public interface IGeotabUserDbUserObjectMapper : IStatusableGeotabObjectMapper<User, DbUser>
    {
        /// <summary>
        /// Builds a JSON array containing the Ids of the <paramref name="userGroups"/>.
        /// </summary>
        /// <param name="userGroups">The list of <see cref="Group"/> objects whose Ids are to be included in the output JSON array.</param>
        /// <returns></returns>
        string GetUserGroupsJSON(IList<Group> userGroups);
    }
}
