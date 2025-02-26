using MyGeotabAPIAdapter.Helpers;

namespace MyGeotabAPIAdapter.MyGeotabAPI
{
    /// <summary>
    /// Lists Geotab <see cref="Id"/> types. 
    /// </summary>
    public class GeotabIdType : Enumeration
    {
        public static readonly GeotabIdType GuidId = new(1, "GuidId");
        public static readonly GeotabIdType LongId = new(2, "LongId");
        public static readonly GeotabIdType NamedGuidId = new(3, "NamedGuidId");
        public static readonly GeotabIdType ShimId = new(4, "ShimId");

        public GeotabIdType(int id, string name) : base(id, name) { }
    }
}
