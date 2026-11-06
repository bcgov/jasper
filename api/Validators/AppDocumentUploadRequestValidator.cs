using System;
using System.IO;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Scv.Api.Models;

namespace Scv.Api.Validators;

public class AppDocumentUploadRequestValidator : AbstractValidator<AppDocumentUploadRequest>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxKeyLength = 100;
    private const string PdfContentType = "application/pdf";
    private const string PdfExtension = ".pdf";
    private static readonly byte[] PdfHeader = [0x25, 0x50, 0x44, 0x46, 0x2D]; // %PDF-

    public AppDocumentUploadRequestValidator()
    {
        RuleFor(r => r.Key)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Key is required.")
            .MaximumLength(MaxKeyLength).WithMessage($"Key must not exceed {MaxKeyLength} characters.")
            .Matches("^[A-Za-z0-9][A-Za-z0-9._-]*$")
                .WithMessage("Key may only contain letters, numbers, '.', '_' and '-', and must start with a letter or number.");

        RuleFor(r => r.File)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("No file uploaded.")
            .Must(file => file is { Length: > 0 }).WithMessage("No file uploaded.")
            .Must(file => file.Length <= MaxFileSizeBytes).WithMessage("File size must not exceed 10 MB.")
            .Must(BeAPdf).WithMessage("Only PDF files are allowed.");
    }

    private static bool BeAPdf(IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(file.FileName) || string.IsNullOrWhiteSpace(file.ContentType))
        {
            return false;
        }

        if (!string.Equals(file.ContentType, PdfContentType, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetExtension(file.FileName), PdfExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Verify the actual file bytes so a renamed non-PDF cannot pass the extension/content-type checks.
        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[5];
        try
        {
            stream.ReadExactly(header);
        }
        catch (EndOfStreamException)
        {
            return false;
        }

        return header.SequenceEqual(PdfHeader);
    }
}
