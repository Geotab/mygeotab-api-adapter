using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DebugData"/> and <see cref="DbDebugData"/> entities.
    /// </summary>
    public interface IGeotabDebugDataDbDebugDataObjectMapper : ICreateOnlyGeotabObjectMapper<DebugData, DbDebugData>
    {
    }
}
