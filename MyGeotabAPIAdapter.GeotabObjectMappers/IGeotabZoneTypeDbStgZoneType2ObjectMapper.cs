using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="ZoneType"/> and <see cref="DbStgZoneType2"/> entities.
    /// </summary>
    public interface IGeotabZoneTypeDbStgZoneType2ObjectMapper : ICreateOnlyGeotabObjectMapper<ZoneType, DbStgZoneType2>
    {
    }
}
