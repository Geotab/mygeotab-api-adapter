using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="User"/> and <see cref="DbStgUser2"/> entities.
    /// </summary>
    public interface IGeotabUserDbStgUser2ObjectMapper : ICreateOnlyGeotabObjectMapper<User, DbStgUser2>
    {
        /// <summary>
        /// Builds a JSON array containing the Ids of the <paramref name="userGroups"/>.
        /// </summary>
        /// <param name="userGroups">The list of <see cref="Group"/> objects whose Ids are to be included in the output JSON array.</param>
        /// <returns></returns>
        string GetUserGroupsJSON(IList<Group> userGroups);
    }
}
