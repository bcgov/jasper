using System.ComponentModel.DataAnnotations;

namespace Scv.Models.TransitoryDocuments
{
    public class DownloadFileRequest
    {
        [Required]
        public TransitoryDocumentFileMetadata FileMetadata { get; set; }
    }
}