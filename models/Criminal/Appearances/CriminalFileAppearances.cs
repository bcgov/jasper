namespace Scv.Models.Criminal.Appearances
{
    public class CriminalFileAppearances : JCCommon.Clients.FileServices.CriminalFileAppearancesResponse
    {
        /// <summary>
        /// Extended object.
        /// </summary>
        public new System.Collections.Generic.ICollection<CriminalAppearanceDetail> ApprDetail { get; set; }
    }
}