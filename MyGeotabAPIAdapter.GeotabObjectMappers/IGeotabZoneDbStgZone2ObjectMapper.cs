using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Zone"/> and <see cref="DbStgZone2"/> entities.
    /// </summary>
    public interface IGeotabZoneDbStgZone2ObjectMapper : ICreateOnlyGeotabObjectMapper<Zone, DbStgZone2>
    {
        /// <summary>
        /// Builds a JSON array containing the Ids of the <paramref name="zoneGroups"/>.
        /// </summary>
        /// <param name="zoneGroups">The list of <see cref="Group"/> objects whose Ids are to be included in the output JSON array.</param>
        /// <returns></returns>
        string GetZoneGroupsJSON(IList<Group> zoneGroups);

        /// <summary>
        /// Builds a JSON array containing the Ids of the <paramref name="zoneTypes"/>.
        /// </summary>
        /// <param name="zoneTypes">The list of <see cref="ZoneType"/> objects whose Ids are to be included in the output JSON array.</param>
        /// <returns></returns>
        string GetZoneTypeIdsJSON(IList<ZoneType> zoneTypes);
    }
}
