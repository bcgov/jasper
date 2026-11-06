namespace Scv.Models.AppDocument;

public class AppDocumentDto : BaseDto
{
    public string Key { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
    public int Size { get; set; }
    public byte[] Data { get; set; }
}
