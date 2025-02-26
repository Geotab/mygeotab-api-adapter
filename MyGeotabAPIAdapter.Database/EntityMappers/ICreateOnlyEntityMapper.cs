using System.Collections.Generic;

namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <typeparamref name="T1"/> and <typeparamref name="T2"/> entities.
    /// </summary>
    /// <typeparam name="T1">The first type of entity.</typeparam>
    /// <typeparam name="T2">The second type of entity.</typeparam>
    public interface ICreateOnlyEntityMapper<T1, T2>
    {
        /// <summary>
        /// Creates and returns a new list of <typeparamref name="T2"/> entities mapped to the <paramref name="entitiesToMapTo"/>.
        /// </summary>
        /// <param name="entitiesToMapTo">The list of <see cref="T1"/> entities for which return a corresponding list of <see cref="T2"/> entities.</param>
        /// <returns></returns>
        List<T2> CreateEntities(List<T1> entitiesToMapTo);

        /// <summary>
        /// Creates and returns a new <typeparamref name="T2"/> entity using property values of the <paramref name="entityToMapTo"/>.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which property values should be used to populate the new <typeparamref name="T2"/> entity.</param>
        /// <returns></returns>
        T2 CreateEntity(T1 entityToMapTo);
    }
}
