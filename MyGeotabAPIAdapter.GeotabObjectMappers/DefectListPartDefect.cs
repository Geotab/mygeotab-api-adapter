using Geotab.Checkmate.ObjectModel;

namespace MyGeotabAPIAdapter.GeotabObjectMappers
{
    /// <summary>
    /// Represents a <see cref="Defect"/> along with its associated Part within a <see cref="DVIRLog.DefectList"/>. See <see href="https://docs.google.com/document/d/18sb2MOphxqKPiwCQTCPnryqRkIe5PhBvdZgXB5qFmqA/edit#heading=h.pzi26ffj92up">How to - DVIR APIs [PUBLIC]</see> for an explanation of the interrelationships between DefectList and Defect objects in DVIRLogs.
    /// </summary>
    public class DefectListPartDefect
    {
        public string DefectListAssetType { get; set; }
        public string DefectListID { get; set; }
        public string DefectListName { get; set; }
        public string PartID { get; set; }
        public string PartName { get; set; }
        public string DefectID { get; set; }
        public string DefectName { get; set; }
        public string DefectSeverity { get; set; }
    }
}
