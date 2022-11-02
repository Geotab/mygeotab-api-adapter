namespace MyGeotabAPIAdapter.Database.DataAccess
{
    /// <summary>
    /// A composite of <see cref="IUnitOfWorkContext"/> and <see cref="IConnectionContext"/>.
    /// </summary>
    public interface IDatabaseUnitOfWorkContext : IUnitOfWorkContext, IConnectionContext, ISqlConnectionContext
    {
    }
}
