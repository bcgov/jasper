using Scv.Api.Documents;

namespace Scv.Api.Models;

public class BinderDocumentDto
{
    public string DocumentId { get; set; }
    /// <summary>
    /// Zero-based integer to determine sorting order
    /// </summary>
    public int Order { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; }
}
