namespace Scv.Models
{
    public class BinderDocumentDto
    {
        public string DocumentId { get; set; }
        /// <summary>
        /// Zero-based integer to determine sorting order
        /// </summary>
        public int Order { get; set; }
        public DocumentType DocumentType { get; set; }
        public string FileName { get; set; }
        public string Category { get; set; }
        /// <summary>
        /// Order ID for transcript documents
        /// </summary>
        public string OrderId { get; set; }
    }
}
