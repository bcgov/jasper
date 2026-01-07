using System.ComponentModel.DataAnnotations;

namespace Scv.Models.TransitoryDocuments
{
    /// <summary>
    /// Request model for searching transitory documents by location, region, room, and date.
    /// </summary>
    public class TransitoryDocumentSearchRequest
    {
        /// <summary>
        /// The region code (e.g., "VAN", "VIC").
        /// </summary>
        [Required]
        public required string RegionCode { get; set; }

        /// <summary>
        /// The region name (e.g., "Vancouver", "Victoria").
        /// </summary>
        [Required]
        public required string RegionName { get; set; }

        /// <summary>
        /// The agency identifier code (e.g., "4801.0001", "4871.0001").
        /// </summary>
        [Required]
        public required string AgencyIdentifierCd { get; set; }

        /// <summary>
        /// The location short name (e.g., "222 Main", "Provincial Court").
        /// </summary>
        [Required]
        public required string LocationShortName { get; set; }

        /// <summary>
        /// The room code (e.g., "101", "R9").
        /// Optional - if not provided, searches all rooms.
        /// </summary>
        public string? RoomCd { get; set; }

        /// <summary>
        /// The date to search for documents (ISO 8601 format: YYYY-MM-DD).
        /// </summary>
        [Required]
        public required DateOnly Date { get; set; }
    }
}
