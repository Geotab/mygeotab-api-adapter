using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="ExceptionEvent"/> and <see cref="DbExceptionEvent"/> entities.
    /// </summary>
    public interface IGeotabExceptionEventDbExceptionEventObjectMapper : ICreateOnlyGeotabObjectMapper<ExceptionEvent, DbExceptionEvent>
    {
    }
}
