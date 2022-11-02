using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DriverChange"/> and <see cref="DbDriverChange"/> entities.
    /// </summary>
    public interface IGeotabDriverChangeDbDriverChangeObjectMapper : ICreateOnlyGeotabObjectMapper<DriverChange, DbDriverChange>
    {
    }
}
