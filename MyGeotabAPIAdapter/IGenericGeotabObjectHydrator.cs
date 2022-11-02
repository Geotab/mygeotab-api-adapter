#nullable enable
using Geotab.Checkmate.ObjectModel;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a generic class that handles "hydration" of Geotab <see cref="Entity"/>s by replacing a subject entity with a fully-populated equivalent from a cache.
    /// </summary>
    /// <typeparam name="T">The type of Geotab <see cref="Entity"/> to be processed by this <see cref="IGenericGeotabObjectHydrator{T}"/> instance.</typeparam>
    internal interface IGenericGeotabObjectHydrator<T> where T : Entity
    {
        /// <summary>
        /// The cache of fully-populated <typeparamref name="T"/> entities used to hydrate entities.
        /// </summary>
        public IGenericGeotabObjectCacher<T> GenericGeotabObjectCacher { get; }

        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Replaces the supplied <paramref name="entityToHydrate"/> with a fully-populated <see cref="T"/> from the <see cref="GenericGeotabObjectCacher"/>'s <see cref="IGenericGeotabObjectCacher{T}.GeotabObjectCache"/> based on matching <see cref="Id"/>s. Returns the fully-populated <see cref="T"/>. If no match is found, returns <paramref name="noEntitySubstitute"/> instead.
        /// </summary>
        /// <param name="entityToHydrate">The <typeparamref name="T"/> to be replaced with a fully-populated counterpart.</param>
        /// <param name="noEntitySubstitute">The value to return if no match for <paramref name="entityToHydrate"/> is found in the <see cref="GenericGeotabObjectCacher"/>'s <see cref="IGenericGeotabObjectCacher{T}.GeotabObjectCache"/>.</param>
        /// <returns></returns>
        T HydrateEntity(T? entityToHydrate, T noEntitySubstitute);
    }
}
