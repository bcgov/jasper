using System.Collections.Generic;
using Scv.Api.Documents;

namespace Scv.Api.Models;

public class BinderDocumentDto
{
    // Common fields
    public string DocumentId { get; set; }
    public string Category { get; set; }
    public string FileName { get; set; }
    public string ImageId { get; set; }
    /// <summary>
    /// Zero-based integer to determine how BinderDocuments are sorted
    /// </summary>
    public int Order { get; set; }
    public DocumentType DocumentType { get; set; }

    // Criminal-specific fields
    public int? DocumentPageCount { get; set; }

    // Civil-specific fields
    public string FileSeqNo { get; set; }
    public string SwornByNm { get; set; }
    public string FiledDt { get; set; }
    public string DateGranted { get; set; }
    public List<IssueDto> Issues { get; set; } = [];
    public List<FiledByDto> FiledBy { get; set; } = [];
    public List<DocumentSupportDto> DocumentSupport { get; set; } = [];
    /// <summary>
    /// Order ID for transcript documents
    /// </summary>
    public string OrderId { get; set; }
}

public class DocumentSupportDto
{
    public string ActCd { get; set; }
    public string ActDsc { get; set; }
}

public class IssueDto
{
    public string IssueNumber { get; set; }
    public string IssueDsc { get; set; }
}

public class FiledByDto
{
    public string FiledByName { get; set; }
    public string RoleTypeCode { get; set; }
}