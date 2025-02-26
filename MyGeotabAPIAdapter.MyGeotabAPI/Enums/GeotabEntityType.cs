using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// Lists Geotab <see cref="Entity"/> types. 
    /// </summary>
    public class GeotabEntityType : Enumeration
    {
        public static readonly GeotabEntityType LogRecord = new(1, "LogRecord");
        public static readonly GeotabEntityType StatusData = new(2, "StatusData");
        public static readonly GeotabEntityType FaultData = new(3, "FaultData");

        public GeotabEntityType(int id, string name) : base(id, name) { }
    }
}
