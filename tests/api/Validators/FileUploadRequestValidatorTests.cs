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

    private readonly FileUploadRequestValidator _validator = new();

    private static IFormFile CreateFile(string fileName, string contentType, long length)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);
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
        var request = new FileUploadRequest { File = CreateFile("signature.png", "image/png", 0) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("No file uploaded.");
    }

    [Fact]
    public void Validate_WhenFileExceedsMaxSize_ShouldFail()
    {
        var request = new FileUploadRequest { File = CreateFile("signature.png", "image/png", MaxFileSizeBytes + 1) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("File size must not exceed 1 MB.");
    }

    [Fact]
    public void Validate_WhenFileIsExactlyMaxSize_ShouldPass()
    {
        var request = new FileUploadRequest { File = CreateFile("signature.png", "image/png", MaxFileSizeBytes) };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.File);
    }

    [Fact]
    public void Validate_WhenFileIsNotAnAllowedImage_ShouldFail()
    {
        var request = new FileUploadRequest { File = CreateFile("malware.exe", "application/octet-stream", 1024) };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("Only JPG, JPEG and PNG image files are allowed.");
    }

    [Theory]
    [InlineData("signature.png", "image/png")]
    [InlineData("signature.jpg", "image/jpeg")]
    [InlineData("signature.jpeg", "image/jpeg")]
    public void Validate_WhenFileIsAllowedImageWithinSizeLimit_ShouldPass(string fileName, string contentType)
    {
        var request = new FileUploadRequest { File = CreateFile(fileName, contentType, 1024) };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.File);
    }
}
