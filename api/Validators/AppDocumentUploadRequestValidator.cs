using System.Linq;
using FluentValidation;
using Scv.Api.Constants;
using Scv.Api.Models;

namespace Scv.Api.Validators;

public class AppDocumentUploadRequestValidator : AbstractValidator<AppDocumentUploadRequest>
{
    private const long MaxFileSizeBytes = 500 * 1024; // Limit to 500 KB for now

    public AppDocumentUploadRequestValidator()
    {
        RuleFor(r => r.Key)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Key is required.")
            .Must(key => AppDocumentKeys.All.Contains(key))
                .WithMessage($"Key must be one of: {string.Join(", ", AppDocumentKeys.All)}.");

        RuleFor(r => r.File)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("No file uploaded.")
            .Must(file => file is { Length: > 0 }).WithMessage("No file uploaded.")
            .Must(file => file.Length <= MaxFileSizeBytes).WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024} KB.");
    }
}
