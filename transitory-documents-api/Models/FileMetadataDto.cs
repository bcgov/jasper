using System;
using System.ComponentModel.DataAnnotations;

namespace Scv.TdApi.Models
{
    public sealed record FileMetadataDto(
        string FileName,
        string Extension,
        [Range(0, long.MaxValue, ErrorMessage = "SizeBytes must be greater than or equal to 0")]
        long SizeBytes,
        DateTime CreatedUtc,
        [Required(ErrorMessage = "AbsolutePath is required")]
        string AbsolutePath,
        string? MatchedRoomFolder // e.g., "CTR001" when file found under that folder; null for top-level day folder
    );
}