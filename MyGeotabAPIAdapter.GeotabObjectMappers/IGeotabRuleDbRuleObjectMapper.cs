using Geotab.Checkmate.ObjectModel;
using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Rule"/> and <see cref="DbRule"/> entities.
    /// </summary>
    public interface IGeotabRuleDbRuleObjectMapper : IStatusableGeotabObjectMapper<Rule, DbRule>
    {
        /// <summary>
        /// Builds a JSON array containing the Ids of the <paramref name="ruleGroups"/>.
        /// </summary>
        /// <param name="ruleGroups">The list of <see cref="Group"/> objects whose Ids are to be included in the output JSON array.</param>
        /// <returns></returns>
        string GetRuleGroupsJSON(IList<Group> ruleGroups);
    }
}
