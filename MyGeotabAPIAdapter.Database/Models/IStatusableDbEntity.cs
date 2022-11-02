namespace MyGeotabAPIAdapter.Database.Models
{
    public interface IStatusableDbEntity
    {
        /// <summary>
        /// Indicates whether the subject corresponding object is active (<see cref="Common.DatabaseRecordStatus.Active"/>) or deleted (<see cref="Common.DatabaseRecordStatus.Deleted"/>) in the MyGeotab database.
        /// </summary>
        int EntityStatus { get; set; }
    }
}
