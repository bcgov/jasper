using System.ComponentModel.DataAnnotations;

namespace Scv.Models.TransitoryDocuments
{
    public class MergePdfsRequest
    {
        [Required]
        public string LocationId { get; set; }

        [Required]
        public string RoomCd { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public TransitoryDocumentFileMetadata[] Files { get; set; }
    }
}