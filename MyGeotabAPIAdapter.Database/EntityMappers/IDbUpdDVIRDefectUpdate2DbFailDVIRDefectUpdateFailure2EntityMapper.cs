using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbUpdDVIRDefectUpdate2"/> and <see cref="DbFailDVIRDefectUpdateFailure2"/> entities.
    /// </summary>
    public interface IDbUpdDVIRDefectUpdate2DbFailDVIRDefectUpdateFailure2EntityMapper
    {
        DbFailDVIRDefectUpdateFailure2 CreateEntity(DbUpdDVIRDefectUpdate2 dbUpdDVIRDefectUpdate2, string failureMessage);
    }
}
