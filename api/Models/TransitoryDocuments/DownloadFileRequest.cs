using System.ComponentModel.DataAnnotations;
using FileMetadata = Scv.TdApi.Models.FileMetadataDto;

namespace Scv.Api.Models.TransitoryDocuments;

public class DownloadFileRequest
{
    [Required]
    public FileMetadata FileMetadata { get; set; }
}