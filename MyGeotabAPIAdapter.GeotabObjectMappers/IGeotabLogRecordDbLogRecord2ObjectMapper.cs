using Geotab.Checkmate.ObjectModel;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="LogRecord"/> and <see cref="DbLogRecord2"/> entities.
    /// </summary>
    public interface IGeotabLogRecordDbLogRecord2ObjectMapper : ICreateOnlyGeotabObjectMapper<LogRecord, DbLogRecord2>
    {
    }
}
