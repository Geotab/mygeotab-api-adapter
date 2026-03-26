using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Audit"/> and <see cref="DbAuditLog2"/> entities.
    /// </summary>
    public interface IGeotabAuditDbAuditLog2ObjectMapper : ICreateOnlyGeotabObjectMapper<Audit, DbAuditLog2>
    {
    }
}
