using System;
using System.IO;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Scv.Api.Models;

namespace Scv.Api.Validators;

public class FileUploadRequestValidator : AbstractValidator<FileUploadRequest>
{
    private const long MaxFileSizeBytes = 1 * 1024 * 1024; // 1 MB
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png"];
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

    public FileUploadRequestValidator()
    {
        RuleFor(r => r.File)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("No file uploaded.")
            .Must(file => file is { Length: > 0 }).WithMessage("No file uploaded.")
            .Must(file => file.Length <= MaxFileSizeBytes).WithMessage("File size must not exceed 1 MB.")
            .Must(BeAnAllowedImage).WithMessage("Only JPG, JPEG and PNG image files are allowed.");
    }

    private static bool BeAnAllowedImage(IFormFile file)
    {
        if (string.IsNullOrWhiteSpace(file.FileName) || string.IsNullOrWhiteSpace(file.ContentType))
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant())
            || !AllowedExtensions.Contains(extension))
        {
            return false;
        }

        // Verify the actual file bytes so a renamed non-image cannot pass the extension/content-type checks.
        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[8];
        try
        {
            stream.ReadExactly(header);
        }
        catch (EndOfStreamException)
        {
            return false;
        }

        return IsPng(header) || IsJpeg(header);
    }

    private static bool IsPng(ReadOnlySpan<byte> header) =>
        header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
        header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;

    private static bool IsJpeg(ReadOnlySpan<byte> header) =>
        header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
}
