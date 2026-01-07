using JCCommon.Clients.FileServices;
using Scv.Db.Models;

namespace Scv.Api.Models.Criminal.Detail
{
    /// <summary>
    /// Wrapper for CfcDocument, adding in additional fields
    /// </summary>
    public class CriminalDocument : CfcDocument
    {
        private string _category;

        public string PartId { get; set; }

        public string Category
        {
            get => _category;
            set => _category = DocumentCategory.Format(value);
        }

        public string DocumentTypeDescription { get; set; }
        public bool? HasFutureAppearance { get; set; }
    }
}