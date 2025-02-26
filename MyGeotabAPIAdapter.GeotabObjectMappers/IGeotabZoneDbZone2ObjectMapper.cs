using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;
using System.Text;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Zone"/> and <see cref="DbZone2"/> entities.
    /// </summary>
    public interface IGeotabZoneDbZone2ObjectMapper : IStatusableGeotabObjectMapper<Zone, DbZone2>
    {
        /// <summary>
        /// Builds a JSON array containing the Ids of the <paramref name="zoneTypes"/>.
        /// </summary>
        /// <param name="zoneTypes">The list of <see cref="ZoneType"/> objects whose Ids are to be included in the output JSON array.</param>
        /// <returns></returns>
        string GetZoneTypeIdsJSON(IList<ZoneType> zoneTypes);
    }
}
