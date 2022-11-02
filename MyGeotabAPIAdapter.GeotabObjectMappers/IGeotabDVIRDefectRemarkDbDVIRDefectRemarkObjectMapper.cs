using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DefectRemark"/> and <see cref="DbDVIRDefectRemark"/> entities.
    /// </summary>
    public interface IGeotabDVIRDefectRemarkDbDVIRDefectRemarkObjectMapper
    {
        /// <summary>
        /// Creates and returns a <see cref="DbDVIRDefectRemark"/> using information from the supplied inputs.
        /// </summary>
        /// <param name="defectRemark">The <see cref="DefectRemark"/> to be converted.</param>
        /// <param name="entityStatus">The status to apply to the new entity.</param>
        /// <returns></returns>
        DbDVIRDefectRemark CreateEntity(DefectRemark defectRemark, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active);
    }
}
