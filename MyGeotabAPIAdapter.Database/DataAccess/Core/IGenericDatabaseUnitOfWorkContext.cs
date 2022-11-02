namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// Interface for a generic provider of the <see cref="IDatabaseUnitOfWorkContext"/> interface. Used to wrap the "true provider" so that multiple implementations of the same interface can be distinguised and correctly retrieved when used with dependency injection. 
    /// </summary>
    public interface IGenericDatabaseUnitOfWorkContext<T> : IDatabaseUnitOfWorkContext where T : IDatabaseUnitOfWorkContext
    {
    }
}
