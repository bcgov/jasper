using Microsoft.AspNetCore.Http;

namespace Scv.Api.Models;

public class FileUploadRequest
{
    public IFormFile File { get; set; }
}
