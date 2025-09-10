namespace Scv.Db.Models;
public class BinderDocument
{
    public string DocumentId { get; set; }
    /// <summary>
    /// Zero-based integer to determine how BinderDocuments are sorted
    /// </summary>
    public int Order { get; set; }
    public int DocumentType { get; set; }
    public string FileName { get; set; }
}
