using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DbDVIRDefectUpdate"/> and <see cref="DbFailedDVIRDefectUpdate"/> entities.
    /// </summary>
    public interface IDbDVIRDefectUpdateDbFailedDVIRDefectUpdateEntityMapper
    {
        /// <summary>
        /// Converts the supplied <see cref="DbDVIRDefectUpdate"/> into a <see cref="DbFailedDVIRDefectUpdate"/>.
        /// </summary>
        /// <param name="dbDVIRDefectUpdate">The <see cref="DbDVIRDefectUpdate"/> to be converted.</param>
        /// <param name="failureMessage">A message indicating the reason why the DVIRDefect update failed.</param>
        /// <returns></returns>
        DbFailedDVIRDefectUpdate CreateEntity(DbDVIRDefectUpdate dbDVIRDefectUpdate, string failureMessage);
    }
}
