using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="DVIRDefect"/> and <see cref="DbStgDVIRDefect2"/> entities.
    /// </summary>
    public interface IGeotabDVIRDefectDbStgDVIRDefect2ObjectMapper
    {
        /// <summary>
        /// Creates and returns a new <see cref="DbStgDVIRDefect2"/> entity using information from the supplied inputs.
        /// </summary>
        /// <param name="dbStgDVIRLog2">The <see cref="DbStgDVIRLog2"/> with which the <paramref name="dvirDefect"/> is to be associated.</param>
        /// <param name="dvirDefect">The <see cref="DVIRDefect"/> from which to capture information.</param>
        /// <param name="defect">The <see cref="Defect"/> from which to capture information.</param>
        /// <param name="defectListPartDefect2">The <see cref="DefectListPartDefect2"/> from which to capture information.</param>
        /// <returns></returns>
        DbStgDVIRDefect2 CreateEntity(DbStgDVIRLog2 dbStgDVIRLog2, DVIRDefect dvirDefect, Defect defect, DefectListPartDefect2 defectListPartDefect2);
    }
}
