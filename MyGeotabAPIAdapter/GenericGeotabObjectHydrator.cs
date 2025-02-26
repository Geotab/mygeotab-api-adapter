#nullable enable
using Geotab.Checkmate.ObjectModel;
using NLog;
using System;
using System.Reflection;

namespace MyGeotabAPIAdapter
{
    /// <summary>
    /// Interface for a generic class that handles "hydration" of Geotab <see cref="Entity"/>s by replacing a subject entity with a fully-populated equivalent from a cache.
    /// </summary>
    /// <typeparam name="T">The type of Geotab <see cref="Entity"/> to be processed by this <see cref="IGenericGeotabObjectHydrator{T}"/> instance.</typeparam>
    internal class GenericGeotabObjectHydrator<T> : IGenericGeotabObjectHydrator<T> where T : Entity
    {
        // Obtain the type parameter type (for logging purposes).
        readonly Type typeParameterType = typeof(T);

        readonly IGenericGeotabObjectCacher<T> genericGeotabObjectCacher;

        readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <inheritdoc/>
        public string Id { get; private set; }

        /// <inheritdoc/>
        public IGenericGeotabObjectCacher<T> GenericGeotabObjectCacher { get => genericGeotabObjectCacher; }

        /// <summary>
        /// Creates a new instance of the <see cref="GenericGeotabObjectHydrator{T}"/> class.
        /// </summary>
        /// <param name="genericGeotabObjectCacher"></param>
        public GenericGeotabObjectHydrator(IGenericGeotabObjectCacher<T> genericGeotabObjectCacher)
        {
            this.genericGeotabObjectCacher = genericGeotabObjectCacher;
            Id = Guid.NewGuid().ToString();
            logger.Debug($"{nameof(GenericGeotabObjectHydrator<T>)}<{typeParameterType}> [Id: {Id}] created.");
        }

        /// <inheritdoc/>
        public T HydrateEntity(T? entityToHydrate, T noEntitySubstitute)
        {
            if (entityToHydrate == null)
            {
                return noEntitySubstitute;
            }

            if (genericGeotabObjectCacher.GeotabObjectCache.TryGetValue(entityToHydrate.Id, out T hydratedEntity))
            {
                return hydratedEntity;
            }
            else
            {
                return noEntitySubstitute;
            }
        }
    }
}
