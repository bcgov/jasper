using System.ComponentModel.DataAnnotations;
using FileMetadata = Scv.TdApi.Models.FileMetadataDto;

namespace Scv.Api.Models.TransitoryDocuments;

public class MergePdfsRequest
{
    [Required]
    public FileMetadata[] Files { get; set; }
}