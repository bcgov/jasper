using System.Collections.Generic;

namespace Scv.Db.Models;

/// <summary>
/// Binder document model containing fields common to both Criminal and Civil documents.
/// </summary>
public class BinderDocument
{
    // Common fields
    public string DocumentId { get; set; }
    public string Category { get; set; }
    public string FileName { get; set; }
    /// <summary>
    /// Zero-based integer to determine how BinderDocuments are sorted
    /// </summary>
    public int Order { get; set; }
    /// <summary>
    /// Document type identifier. See <see cref="Scv.Api.Documents.DocumentType"/> for valid values.
    /// </summary>
    public int DocumentType { get; set; }

    // Criminal-specific fields
    public int? DocumentPageCount { get; set; }

    // Civil-specific fields
    public int? FileSeqNo { get; set; }
    public string SwornByNm { get; set; }
    public string FiledDt { get; set; }
    public string DateGranted { get; set; }
    public List<Issue> Issues { get; set; } = [];
    public List<FiledBy> FiledBy { get; set; } = [];
    public List<DocumentSupport> DocumentSupport { get; set; } = [];
    /// <summary>
    /// Order ID for transcript documents
    /// </summary>
    public string OrderId { get; set; }
}


// Civil documents child objects
public class DocumentSupport
{
    public string ActCd { get; set; }
    public string ActDsc { get; set; }
}

public class Issue
{
    public string IssueResultDsc { get; set; }
    public string IssueDsc { get; set; }
}

public class FiledBy
{
    public string FiledByName { get; set; }
    public string RoleTypeCode { get; set; }
}