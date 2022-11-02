using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DVIRLog"/> and <see cref="DbDVIRLog"/> entities.
    /// </summary>
    public interface IGeotabDVIRLogDbDVIRLogObjectMapper
    {
        /// <summary>
        /// Creates and returns a <see cref="DbDVIRLog"/> using information from the supplied inputs.
        /// </summary>
        /// <param name="dvirLog">The <see cref="DVIRLog"/> to be converted.</param>
        /// <param name="entityStatus">The status to apply to the new entity.</param>
        /// <returns></returns>
        DbDVIRLog CreateEntity(DVIRLog dvirLog, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active);
    }
}
