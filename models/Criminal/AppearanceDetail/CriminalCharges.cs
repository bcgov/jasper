namespace Scv.Models.Criminal.AppearanceDetail
{
    /// <summary>
    /// Adds extra fields to criminalAppearanceCount.
    /// </summary>
    public class CriminalCharges : JCCommon.Clients.FileServices.CriminalAppearanceCount
    {
        public string AppearanceReasonDsc { get; set; }
        public string AppearanceResultDesc { get; set; }
        public string FindingDsc { get; set; }
        public string PleaCode { get; set; }
        public DateTime? PleaDate { get; set; }
    }
}