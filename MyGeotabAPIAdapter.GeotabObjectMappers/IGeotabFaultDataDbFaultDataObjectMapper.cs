using Geotab.Checkmate.ObjectModel.Engine;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="FaultData"/> and <see cref="DbFaultData"/> entities.
    /// </summary>
    public interface IGeotabFaultDataDbFaultDataObjectMapper : ICreateOnlyGeotabObjectMapper<FaultData, DbFaultData>
    {
    }
}
