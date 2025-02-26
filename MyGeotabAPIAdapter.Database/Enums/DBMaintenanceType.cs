using MyGeotabAPIAdapter.Database.Models;
using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Lists possible values for <see cref="DbDBMaintenanceLog2.MaintenanceType"/>. 
    /// </summary>
    public class DBMaintenanceType : Enumeration
    {
        public static readonly DBMaintenanceType Level1 = new(1, "Level1");
        public static readonly DBMaintenanceType Level2 = new(2, "Level2");
        public static readonly DBMaintenanceType Partition = new(3, "Partition");

        public DBMaintenanceType(int id, string name) : base(id, name) { }
    }
}
