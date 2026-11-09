namespace Scv.Models.AppDocument;

public class AppDocumentDto : BaseDto
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public long TargetSize { get; set; }
    public byte[] Data { get; set; }
}
