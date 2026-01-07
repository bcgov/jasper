namespace Scv.Models.Dars
{
    public class TranscriptSearchRequest
    {
        public string? PhysicalFileId { get; set; }
        public string? MdocJustinNo { get; set; }
        public bool ReturnChildRecords { get; set; } = true;
    }
}
