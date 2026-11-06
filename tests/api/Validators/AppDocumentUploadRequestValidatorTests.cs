using System.IO;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using Scv.Api.Models;
using Scv.Api.Validators;
using Xunit;

namespace tests.api.Validators;

public class AppDocumentUploadRequestValidatorTests
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private static readonly byte[] PdfHeader = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37];

    private readonly AppDocumentUploadRequestValidator _validator = new();

    private static IFormFile CreateFile(string fileName, string contentType, byte[] content, long? length = null)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length ?? content.Length);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        return file.Object;
    }

    private static AppDocumentUploadRequest CreateRequest(string key = "user-guide", IFormFile file = null) => new()
    {
        Key = key,
        File = file ?? CreateFile("guide.pdf", "application/pdf", PdfHeader)
    };

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldPass()
    {
        var result = _validator.TestValidate(CreateRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenKeyIsMissing_ShouldFail(string key)
    {
        var result = _validator.TestValidate(CreateRequest(key));

        result.ShouldHaveValidationErrorFor(r => r.Key)
            .WithErrorMessage("Key is required.");
    }

    [Theory]
    [InlineData("user guide")]
    [InlineData("-guide")]
    [InlineData("guide/1")]
    public void Validate_WhenKeyHasInvalidCharacters_ShouldFail(string key)
    {
        var result = _validator.TestValidate(CreateRequest(key));

        result.ShouldHaveValidationErrorFor(r => r.Key);
    }

    [Fact]
    public void Validate_WhenKeyIsTooLong_ShouldFail()
    {
        var result = _validator.TestValidate(CreateRequest(new string('a', 101)));

        result.ShouldHaveValidationErrorFor(r => r.Key)
            .WithErrorMessage("Key must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WhenFileIsNull_ShouldFail()
    {
        var request = CreateRequest();
        request.File = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("No file uploaded.");
    }

    [Fact]
    public void Validate_WhenFileExceedsMaxSize_ShouldFail()
    {
        var file = CreateFile("guide.pdf", "application/pdf", PdfHeader, MaxFileSizeBytes + 1);

        var result = _validator.TestValidate(CreateRequest(file: file));

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("File size must not exceed 10 MB.");
    }

    [Theory]
    [InlineData("guide.png", "application/pdf")]
    [InlineData("guide.pdf", "image/png")]
    public void Validate_WhenExtensionOrContentTypeIsNotPdf_ShouldFail(string fileName, string contentType)
    {
        var file = CreateFile(fileName, contentType, PdfHeader);

        var result = _validator.TestValidate(CreateRequest(file: file));

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("Only PDF files are allowed.");
    }

    [Fact]
    public void Validate_WhenFileContentIsNotPdf_ShouldFail()
    {
        var file = CreateFile("guide.pdf", "application/pdf", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        var result = _validator.TestValidate(CreateRequest(file: file));

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("Only PDF files are allowed.");
    }

    [Fact]
    public void Validate_WhenFileIsShorterThanHeader_ShouldFail()
    {
        var file = CreateFile("guide.pdf", "application/pdf", [0x25, 0x50]);

        var result = _validator.TestValidate(CreateRequest(file: file));

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("Only PDF files are allowed.");
    }
}
