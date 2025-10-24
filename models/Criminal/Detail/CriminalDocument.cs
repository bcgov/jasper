using JCCommon.Clients.FileServices;

namespace Scv.Models.Criminal.Detail
{
    /// <summary>
    /// Wrapper for CfcDocument, adding in additional fields
    /// </summary>
    public class CriminalDocument : CfcDocument
    {
        public string PartId { get; set; }
        public string Category { get; set; }
        public string DocumentTypeDescription { get; set; }
        public bool? HasFutureAppearance { get; set; }
    }
}