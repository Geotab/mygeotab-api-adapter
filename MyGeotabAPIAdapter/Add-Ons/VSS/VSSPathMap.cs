namespace MyGeotabAPIAdapter.Add_Ons.VSS
{
    /// <summary>
    /// Maps Geotab objects to corresponding VSS paths.
    /// </summary>
    public class VSSPathMap
    {
        public string GeotabObjectType { get; set; }
        public string GeotabObjectPropertyName { get; set; }
        public string GeotabDiagnosticId { get; set; }
        public string VSSPath { get; set; }
        public VSSDataType VSSDataType { get; set; }
        public double UnitConversionMultiplier { get; set; }
    }
}
