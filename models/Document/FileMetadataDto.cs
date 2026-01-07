namespace Scv.TdApi.Models
{
    public class FileMetadataDto
    {
        public required string FileName { get; set; }
        public required string Extension { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public required string RelativePath { get; set; }
        public string? MatchedRoomFolder { get; set; } // e.g., "CTR001" when file found under that folder; null for top-level day folder
    }
}