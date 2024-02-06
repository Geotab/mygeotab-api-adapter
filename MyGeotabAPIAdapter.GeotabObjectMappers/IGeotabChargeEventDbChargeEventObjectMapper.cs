using Geotab.Checkmate.ObjectModel.Charging;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="ChargeEvent"/> and <see cref="DbChargeEvent"/> entities.
    /// </summary>
    public interface IGeotabChargeEventDbChargeEventObjectMapper : ICreateOnlyGeotabObjectMapper<Geotab.Checkmate.ObjectModel.Charging.ChargeEvent, DbChargeEvent>
    {
    }
}
