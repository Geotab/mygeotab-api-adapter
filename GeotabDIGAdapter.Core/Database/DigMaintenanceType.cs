using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Lists possible values for <see cref="DbGdaDigMaintenanceLog.MaintenanceTypeId"/>.
    /// <para>
    /// The first three have the same ids and meanings as the MyGeotab API Adapter's <see cref="DBMaintenanceType"/>, so that one reading of a maintenance log covers both adapters. <see cref="Retention"/> exists only here, because only this adapter performs retention.
    /// </para>
    /// </summary>
    public class DigMaintenanceType : Enumeration
    {
        /// <summary>
        /// Level 1 maintenance: UPDATE STATISTICS on SQL Server, VACUUM and ANALYZE on PostgreSQL.
        /// </summary>
        public static readonly DigMaintenanceType Level1 = new(1, "Level1");

        /// <summary>
        /// Level 2 maintenance: index REORGANIZE and REBUILD on SQL Server, REINDEX on PostgreSQL.
        /// </summary>
        public static readonly DigMaintenanceType Level2 = new(2, "Level2");

        /// <summary>
        /// Partition management: the creation of the period partitions the partitioned tables need.
        /// </summary>
        public static readonly DigMaintenanceType Partition = new(3, "Partition");

        /// <summary>
        /// Retention: partition retirement, or the DIGInvalidRecords sweep. Each writes a row of its own, because they run on either side of the maintenance pause.
        /// </summary>
        public static readonly DigMaintenanceType Retention = new(4, "Retention");

        public DigMaintenanceType(int id, string name) : base(id, name) { }
    }
}
