using System.ComponentModel.DataAnnotations;

namespace Scv.Models.TransitoryDocuments
{
    public class GetDocumentsRequest
    {
        [Required]
        public string LocationId { get; set; }

        [Required]
        public string RoomCd { get; set; }

        [Required]
        public DateOnly Date { get; set; }
    }
}