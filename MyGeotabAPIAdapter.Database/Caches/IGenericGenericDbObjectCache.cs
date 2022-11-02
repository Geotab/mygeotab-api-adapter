using MyGeotabAPIAdapter.Database.Models;

namespace MyGeotabAPIAdapter.Database.Caches
{
    /// <summary>
    /// Interface for a generic provider of the <see cref="IGenericDbObjectCache{T}"/> interface. Used to wrap the "true provider" so that multiple implementations of the same interface can be distinguised and correctly retrieved when used with dependency injection. 
    /// </summary>
    public interface IGenericGenericDbObjectCache<T1, T2> : IGenericDbObjectCache<T1> where T1 : class, IIdCacheableDbEntity where T2 : IGenericDbObjectCache<T1>
    {
        /// <summary>
        /// A unique identifier assigned during instantiation. Intended for debugging purposes.
        /// </summary>
        string Id { get; }
    }
}
