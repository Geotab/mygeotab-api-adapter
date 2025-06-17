using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DefectRemark"/> and <see cref="DbStgDVIRDefectRemark2"/> entities.
    /// </summary>
    public interface IGeotabDVIRDefectRemarkDbStgDVIRDefectRemark2ObjectMapper
    {
        /// <summary>
        /// Creates and returns a new <see cref="DbStgDVIRDefectRemark2"/> entity using information from the supplied inputs.
        /// </summary>
        /// <param name="dbStgDVIRDefect2">The <see cref="DbStgDVIRDefect2"/> from which to capture information.</param>
        /// <param name="defectRemark">The <see cref="DefectRemark"/> from which to capture information.</param>
        /// <returns></returns>
        DbStgDVIRDefectRemark2 CreateEntity(DbStgDVIRDefect2 dbStgDVIRDefect2, DefectRemark defectRemark);
    }
}
