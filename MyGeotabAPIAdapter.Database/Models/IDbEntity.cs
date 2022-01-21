namespace MyGeotabAPIAdapter.Database.Models
{
    /// <summary>
    /// Interface for database entity models.
    /// </summary>
    public interface IDbEntity
    {
        /// <summary>
        /// The <see cref="Common.DatabaseWriteOperationType"/> to be applied to the subject entity.
        /// </summary>
        Common.DatabaseWriteOperationType DatabaseWriteOperationType { get; set; }

        /// <summary>
        /// The name of the database table with which the subject entity is associated.
        /// </summary>
        string DatabaseTableName { get; }
    }
}
