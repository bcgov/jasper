using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;

namespace Scv.Api.Services.AWS;

public interface IS3Service
{
    Task<Stream> DownloadFileAsync(string bucketName, string key);
}

public class S3Service(IAmazonS3 s3Client) : IS3Service
{
    private readonly IAmazonS3 _s3Client = s3Client;

    public async Task<Stream> DownloadFileAsync(string bucketName, string key)
    {
        try
        {
            using var response = await _s3Client.GetObjectAsync(bucketName, key);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            throw new IOException("Unexpected error downloading file from S3: {ex.Message}", ex);
        }
    }
}
