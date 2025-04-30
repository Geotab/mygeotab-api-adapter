using Geotab.Checkmate.ObjectModel.Charging;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="ChargeEvent"/> and <see cref="DbStgChargeEvent2"/> entities.
    /// </summary>
    public interface IGeotabChargeEventDbStgChargeEvent2ObjectMapper : ICreateOnlyGeotabObjectMapper<ChargeEvent, DbStgChargeEvent2>
    {
    }
}
