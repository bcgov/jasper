using System.IO;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using Scv.Api.Models;
using Scv.Api.Validators;
using Xunit;

namespace tests.api.Validators;

public class FileUploadRequestValidatorTests
{
    private const long MaxFileSizeBytes = 1 * 1024 * 1024; // 1 MB

    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];

    private readonly FileUploadRequestValidator _validator = new();

    private static IFormFile CreateFile(string fileName, string contentType, byte[] content, long? length = null)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length ?? content.Length);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        return file.Object;
    }

    [Fact]
    public void Validate_WhenFileIsNull_ShouldFail()
    {
        var request = new FileUploadRequest { File = null };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("No file uploaded.");
    }

    [Fact]
    public void Validate_WhenFileIsEmpty_ShouldFail()
    {
        var request = new FileUploadRequest { File = CreateFile("signature.png", "image/png", [], 0) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("No file uploaded.");
    }

    [Fact]
    public void Validate_WhenFileExceedsMaxSize_ShouldFail()
    {
        var request = new FileUploadRequest { File = CreateFile("signature.png", "image/png", PngHeader, MaxFileSizeBytes + 1) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("File size must not exceed 1 MB.");
    }

    [Fact]
    public void Validate_WhenFileIsExactlyMaxSize_ShouldPass()
    {
        var request = new FileUploadRequest { File = CreateFile("signature.png", "image/png", PngHeader, MaxFileSizeBytes) };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.File);
    }

    [Fact]
    public void Validate_WhenFileIsNotAnAllowedImage_ShouldFail()
    {
        var request = new FileUploadRequest { File = CreateFile("malware.exe", "application/octet-stream", [0x4D, 0x5A, 0x90, 0x00], 1024) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("Only JPG, JPEG and PNG image files are allowed.");
    }

    [Fact]
    public void Validate_WhenExtensionAndContentTypeAreAllowedButBytesAreNotAnImage_ShouldFail()
    {
        // A non-image renamed with an allowed extension and spoofed content type must still be rejected.
        var request = new FileUploadRequest { File = CreateFile("malware.png", "image/png", [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00]) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("Only JPG, JPEG and PNG image files are allowed.");
    }

    [Fact]
    public void Validate_WhenContentTypeIsSpoofedButExtensionIsNotAllowed_ShouldFail()
    {
        var request = new FileUploadRequest { File = CreateFile("malware.exe", "image/png", PngHeader) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("Only JPG, JPEG and PNG image files are allowed.");
    }

    [Fact]
    public void Validate_WhenFileIsSmallerThanTheHeader_ShouldFail()
    {
        var request = new FileUploadRequest { File = CreateFile("signature.png", "image/png", [0x89, 0x50, 0x4E]) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("Only JPG, JPEG and PNG image files are allowed.");
    }

    [Fact]
    public void Validate_WhenExtensionAndContentTypeAreUppercase_ShouldPass()
    {
        var request = new FileUploadRequest { File = CreateFile("SIGNATURE.PNG", "IMAGE/PNG", PngHeader) };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.File);
    }

    [Theory]
    [InlineData("signature.png", "image/png")]
    [InlineData("signature.jpg", "image/jpeg")]
    [InlineData("signature.jpeg", "image/jpeg")]
    public void Validate_WhenFileIsAllowedImageWithinSizeLimit_ShouldPass(string fileName, string contentType)
    {
        var header = fileName.EndsWith(".png") ? PngHeader : JpegHeader;
        var request = new FileUploadRequest { File = CreateFile(fileName, contentType, header) };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.File);
    }
}
