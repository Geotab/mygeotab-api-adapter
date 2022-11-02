using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Trip"/> and <see cref="DbTrip"/> entities.
    /// </summary>
    public interface IGeotabTripDbTripObjectMapper : ICreateOnlyGeotabObjectMapper<Trip, DbTrip>
    {
    }
}
