using System.ComponentModel.DataAnnotations;

namespace Scv.TdApi.Models
{
    public class FileMetadataDto
    {
        public string FileName { get; set; }
        public string Extension { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string AbsolutePath { get; set; }
        public string? MatchedRoomFolder { get; set; } // e.g., "CTR001" when file found under that folder; null for top-level day folder
    }
}