namespace Scv.Models.TransitoryDocuments
{
    public class TransitoryDocumentFileMetadata
    {
        public required string FileName { get; set; }
        public required string Extension { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public required string RelativePath { get; set; }
        public string? MatchedRoomFolder { get; set; }
    }
}
