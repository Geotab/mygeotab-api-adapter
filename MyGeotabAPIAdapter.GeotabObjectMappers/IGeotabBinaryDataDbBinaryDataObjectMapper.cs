using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="BinaryData"/> and <see cref="DbBinaryData"/> entities.
    /// </summary>
    public interface IGeotabBinaryDataDbBinaryDataObjectMapper : ICreateOnlyGeotabObjectMapper<Geotab.Checkmate.ObjectModel.BinaryData, DbBinaryData>
    {
    }
}
