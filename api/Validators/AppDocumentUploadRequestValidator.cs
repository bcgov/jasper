using System;
using System.Linq;
using FluentValidation;
using Scv.Api.Models;
using Scv.Db.Models;

namespace Scv.Api.Validators;

public class AppDocumentUploadRequestValidator : AbstractValidator<AppDocumentUploadRequest>
{
    public AppDocumentUploadRequestValidator()
    {
        RuleFor(r => r.Key)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Key is required.")
            .Must(key => AppDocument.ALL_APP_DOCUMENTS.Any(ad => ad.Key == key))
                .WithMessage($"Key must be one of: {string.Join(", ", AppDocument.ALL_APP_DOCUMENTS.Select(ad => ad.Key))}.");

        RuleFor(r => r.File)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("No file uploaded.")
            .Must(file => file is { Length: > 0 }).WithMessage("No file uploaded.");

        // Validation rules against the default AppDocument list
        When(r => r.File is { Length: > 0 } && AppDocument.ALL_APP_DOCUMENTS.Any(ad => ad.Key == r.Key), () =>
        {
            RuleFor(r => r.File)
                .Cascade(CascadeMode.Stop)
                .Must((r, file) => file.Length <= GetAppDocument(r.Key).TargetSize)
                    .WithMessage(r => $"File size must not exceed {GetAppDocument(r.Key).TargetSize / 1024} KB.")
                .Must((r, file) => string.Equals(file.ContentType, GetAppDocument(r.Key).ContentType, StringComparison.OrdinalIgnoreCase))
                    .WithMessage(r => $"File content type must be {GetAppDocument(r.Key).ContentType}.");
        });
    }

    private static AppDocument GetAppDocument(string key) =>
        AppDocument.ALL_APP_DOCUMENTS.First(ad => ad.Key == key);
}
