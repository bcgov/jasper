using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Scv.Api.Models;
using Scv.Api.Services;
using Scv.Core.Infrastructure;

namespace Scv.Api.Processors;

public record UploadedFile(byte[] Content, string ContentType, string FileName);

public interface IFileUploadProcessor
{
    Task<OperationResult<UploadedFile>> ValidateAndScanAsync<TRequest>(TRequest request, IValidator<TRequest> validator)
        where TRequest : FileUploadRequest;
}

public class FileUploadProcessor(
    IAntiVirusService antiVirusService,
    ILogger<FileUploadProcessor> logger) : IFileUploadProcessor
{
    private readonly IAntiVirusService _antiVirusService = antiVirusService;
    private readonly ILogger<FileUploadProcessor> _logger = logger;

    public async Task<OperationResult<UploadedFile>> ValidateAndScanAsync<TRequest>(TRequest request, IValidator<TRequest> validator)
        where TRequest : FileUploadRequest
    {
        if (request == null)
        {
            return OperationResult<UploadedFile>.Failure("No file was uploaded.");
        }

        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return OperationResult<UploadedFile>.Failure(validationResult.Errors.Select(e => e.ErrorMessage).FirstOrDefault());
        }

        var file = request.File;
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        memoryStream.Position = 0;
        var (isClean, message) = await _antiVirusService.ScanAsync(memoryStream);
        if (!isClean)
        {
            _logger.LogWarning("The uploaded file failed the antivirus scan: {Message}", message);
            return OperationResult<UploadedFile>.Failure("The uploaded file failed the antivirus scan.");
        }

        return OperationResult<UploadedFile>.Success(new UploadedFile(memoryStream.ToArray(), file.ContentType, file.FileName));
    }
}
