using Geotab.Checkmate.ObjectModel.Exceptions;
using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <see cref="Condition"/> and <see cref="DbCondition"/> entities.
    /// </summary>
    public interface IGeotabConditionDbConditionObjectMapper : IStatusableGeotabObjectMapper<Condition, DbCondition>
    {
        /// <summary>
        /// Creates and returns a list of <see cref="DbCondition"/> entities representing any and all <see cref="Condition"/>s associated with the <paramref name="rule"/>.
        /// </summary>
        /// <param name="rule">The <see cref="Rule"/> for which to create and return a list of associated <see cref="DbCondition"/> entities.</param>
        /// <param name="entityStatus">The status to apply to the <see cref="DbCondition"/>s in the list.</param>
        /// <returns></returns>
        IList<DbCondition> CreateDbConditionEntitiesForRule(Rule rule, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active);
    }
}
