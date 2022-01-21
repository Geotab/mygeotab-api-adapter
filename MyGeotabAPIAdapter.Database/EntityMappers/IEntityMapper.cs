namespace MyGeotabAPIAdapter.Database.EntityMappers
{
    /// <summary>
    /// Interface for a class with methods involving mapping between <typeparamref name="T1"/> and <typeparamref name="T2"/> entities.
    /// </summary>
    /// <typeparam name="T1">The first type of entity.</typeparam>
    /// <typeparam name="T2">The second type of entity.</typeparam>
    public interface IEntityMapper<T1,T2>
    {
        /// <summary>
        /// Creates and returns a new <typeparamref name="T2"/> entity using property values of the <paramref name="entityToMapTo"/>.
        /// </summary>
        /// <param name="entityToMapTo">The entity from which property values should be used to populate the new <typeparamref name="T2"/> entity.</param>
        /// <returns></returns>
        T2 CreateEntity(T1 entityToMapTo);

        /// <summary>
        /// Updates properties of the <paramref name="entityToUpdate"/> using property values of the <paramref name="entityToMapTo"/>. 
        /// </summary>
        /// <param name="entityToUpdate">The entity to be updated.</param>
        /// <param name="entityToMapTo">The entity from which to obtain values to be used when updating properties of the <paramref name="entityToUpdate"/>.</param>
        void UpdateEntity(T2 entityToUpdate, T1 entityToMapTo);
    }
}
