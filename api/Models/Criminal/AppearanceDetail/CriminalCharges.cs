namespace Scv.Api.Models.Criminal.AppearanceDetail
{
    /// <summary>
    /// Adds extra fields to criminalAppearanceCount.
    /// </summary>
    public class CriminalCharges : JCCommon.Clients.FileServices.ClAppearanceCount
    {
        public string AppearanceReasonDsc { get; set; }
        public string AppearanceResultDesc { get; set; }
        public string FindingDsc { get; set; }
    }
}