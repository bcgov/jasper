using FluentValidation;
using Microsoft.Extensions.Options;
using Scv.Models;
using Scv.Models.TransitoryDocuments;

namespace Scv.Api.Validators.TransitoryDocuments;

public class DownloadFileRequestValidator : AbstractValidator<DownloadFileRequest>
{
    public DownloadFileRequestValidator(IOptions<TdApiOptions> tdApiOptions)
    {
        var maxFileSize = tdApiOptions.Value.MaxFileSize;
        var maxSizeMB = maxFileSize / 1024.0 / 1024.0;

        RuleFor(x => x.FileMetadata)
            .NotNull().WithMessage("fileMetadata is required.");

        RuleFor(x => x.FileMetadata.RelativePath)
            .NotEmpty().WithMessage("AbsolutePath is required and must be non-empty.")
            .When(x => x.FileMetadata != null);

        RuleFor(x => x.FileMetadata.SizeBytes)
            .GreaterThanOrEqualTo(0).WithMessage("SizeBytes must be greater than or equal to 0.")
            .When(x => x.FileMetadata != null);

        RuleFor(x => x.FileMetadata.SizeBytes)
            .LessThanOrEqualTo(maxFileSize)
            .WithMessage($"File size exceeds maximum allowed size of {maxSizeMB:F2} MB.")
            .When(x => x.FileMetadata != null);
    }
}