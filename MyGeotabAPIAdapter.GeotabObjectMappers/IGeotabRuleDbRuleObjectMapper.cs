using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Rule"/> and <see cref="DbRule"/> entities.
    /// </summary>
    public interface IGeotabRuleDbRuleObjectMapper : IStatusableGeotabObjectMapper<Rule, DbRule>
    {
    }
}
