using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="StatusData"/> and <see cref="DbStatusData"/> entities.
    /// </summary>
    public interface IGeotabStatusDataDbStatusDataObjectMapper : ICreateOnlyGeotabObjectMapper<StatusData, DbStatusData>
    {
    }
}
