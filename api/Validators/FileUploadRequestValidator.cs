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
        return AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant())
            && AllowedExtensions.Contains(extension);
    }
}
