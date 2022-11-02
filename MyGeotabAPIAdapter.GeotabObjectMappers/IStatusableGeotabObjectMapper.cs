using MyGeotabAPIAdapter.Database;
using MyGeotabAPIAdapter.Database.Models;
using Geotab.Checkmate.ObjectModel;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <typeparamref name="T1"/> objects and <typeparamref name="T2"/> entities.
    /// </summary>
    /// <typeparam name="T1">The first type of entity.</typeparam>
    /// <typeparam name="T2">The second type of entity.</typeparam>
    public interface IStatusableGeotabObjectMapper<T1, T2> where T1 : IEntity where T2 : IStatusableDbEntity
    {
        /// <summary>
        /// Creates and returns a new <typeparamref name="T2"/> entity using property values of the <paramref name="entityToMapTo"/>.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which property values should be used to populate the new <typeparamref name="T2"/> entity.</param>
        /// <param name="entityStatus">The status to apply to the new entity.</param>
        /// <returns></returns>
        T2 CreateEntity(T1 entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active);

        /// <summary>
        /// Compares properties of the <paramref name="entityToEvaluate"/> with those of the <paramref name="entityToMapTo"/> and determines whether the <paramref name="entityToEvaluate"/> needs to be updated based on changes to property values.
        /// </summary>
        /// <param name="entityToEvaluate">The entity to be evaluated.</param>
        /// <param name="entityToMapTo">The entity to compare properties against in order to determine whether the <paramref name="entityToEvaluate"/> needs to be updated.</param>
        /// <returns></returns>
        bool EntityRequiresUpdate(T2 entityToEvaluate, T1 entityToMapTo);

        /// <summary>
        /// Updates properties of the <paramref name="entityToUpdate"/> using property values of the <paramref name="entityToMapTo"/> and then returns the updated <paramref name="entityToUpdate"/>. 
        /// </summary>
        /// <param name="entityToUpdate">The entity to be updated.</param>
        /// <param name="entityToMapTo">The entity from which to obtain values to be used when updating properties of the <paramref name="entityToUpdate"/>.</param>
        /// <param name="entityStatus">The status to apply to the <paramref name="entityToUpdate"/>.</param>
        T2 UpdateEntity(T2 entityToUpdate, T1 entityToMapTo, Common.DatabaseRecordStatus entityStatus = Common.DatabaseRecordStatus.Active);
    }
}
