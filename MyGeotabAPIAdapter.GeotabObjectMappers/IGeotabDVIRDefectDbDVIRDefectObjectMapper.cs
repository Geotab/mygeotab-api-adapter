using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DVIRDefect"/> and <see cref="DbDVIRDefect"/> entities.
    /// </summary>
    public interface IGeotabDVIRDefectDbDVIRDefectObjectMapper
    {
        /// <summary>
        /// Creates and returns a <see cref="DbDVIRDefect"/> using information from the supplied inputs.
        /// </summary>
        /// <param name="dvirLog">The <see cref="DVIRLog"/> from which to capture information.</param>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> from which to capture information.</param>
        /// <param name="defect">The <see cref="Defect"/> from which to capture information.</param>
        /// <param name="defectListPartDefect">The <see cref="DefectListPartDefect"/> from which to capture information.</param>
        /// <param name="entityStatus">The status to apply to the new entity.</param>
        /// <returns></returns>
        DbDVIRDefect CreateEntity(DVIRLog dvirLog, DVIRDefect dvirDefect, Defect defect, DefectListPartDefect defectListPartDefect, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active);

        /// <summary>
        /// Indicates whether the <see cref="DbDVIRDefect"/> differs from the <see cref="DVIRDefect"/>, thereby requiring the <see cref="DbDVIRDefect"/> to be updated in the database. 
        /// </summary>
        /// <param name="dbDVIRDefect">The <see cref="DbDVIRDefect"/> to be evaluated.</param>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> to compare against.</param>
        /// <returns></returns>
        bool EntityRequiresUpdate(DbDVIRDefect dbDVIRDefect, DVIRDefect dvirDefect);

        /// <summary>
        /// Updates properties of the <paramref name="dbDVIRDefect"/> using property values of the <paramref name="dvirDefect"/> and then returns the updated <paramref name="dbDVIRDefect"/>
        /// </summary>
        /// <param name="dbDVIRDefect">The entity to be updated.</param>
        /// <param name="dvirLog">The <see cref="DVIRLog"/> from which to capture information.</param>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> from which to capture information.</param>
        /// <param name="defect">The <see cref="Defect"/> from which to capture information.</param>
        /// <param name="defectListPartDefect">The <see cref="DefectListPartDefect"/> from which to capture information.</param>
        /// <param name="entityStatus">The status to apply to the <paramref name="dbDVIRDefect"/>.</param>
        /// <returns></returns>
        DbDVIRDefect UpdateEntity(DbDVIRDefect dbDVIRDefect, DVIRLog dvirLog, DVIRDefect dvirDefect, Defect defect, DefectListPartDefect defectListPartDefect, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active);
    }
}
