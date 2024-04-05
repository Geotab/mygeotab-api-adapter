using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DutyStatusLog"/> and <see cref="DbDutyStatusLog"/> entities.
    /// </summary>
    public interface IGeotabDutyStatusLogDbDutyStatusLogObjectMapper : ICreateOnlyGeotabObjectMapper<DutyStatusLog, DbDutyStatusLog>
    {
    }
}
