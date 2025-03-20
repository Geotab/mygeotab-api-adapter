using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Group"/> and <see cref="DbStgGroup2"/> entities.
    /// </summary>
    public interface IGeotabGroupDbStgGroup2ObjectMapper : ICreateOnlyGeotabObjectMapper<Group, DbStgGroup2>
    {
        /// <summary>
        /// Builds a JSON array containing the Ids of the <paramref name="groups"/>.
        /// </summary>
        /// <param name="groupChildren">The list of <see cref="Group"/> objects whose Ids are to be included in the output JSON array.</param>
        /// <returns></returns>
        string GetGroupChildrenIdsJSON(IList<Group> groupChildren);
    }
}
