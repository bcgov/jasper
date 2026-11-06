using System.IO;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using Scv.Api.Constants;
using Scv.Api.Models;
using Scv.Api.Validators;
using Xunit;

namespace tests.api.Validators;

public class AppDocumentUploadRequestValidatorTests
{
    private const long MaxFileSizeBytes = 500 * 1024; // 500 KB

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

    private static AppDocumentUploadRequest CreateRequest(string key = AppDocumentKeys.SmallClaimsAddtlPageTemplate, IFormFile file = null) => new()
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
    [InlineData("user-guide")]
    [InlineData("scl_addtl_page_template")]
    [InlineData("SCL_ADDTL_PAGE_TEMPLATE ")]
    [InlineData("OTHER")]
    public void Validate_WhenKeyIsNotAllowed_ShouldFail(string key)
    {
        var result = _validator.TestValidate(CreateRequest(key));

        result.ShouldHaveValidationErrorFor(r => r.Key)
            .WithErrorMessage($"Key must be one of: {AppDocumentKeys.SmallClaimsAddtlPageTemplate}.");
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
            .WithErrorMessage("File size must not exceed 500 KB.");
    }

    [Fact]
    public void Validate_WhenFileIsExactlyMaxSize_ShouldPass()
    {
        var file = CreateFile("guide.pdf", "application/pdf", PdfHeader, MaxFileSizeBytes);

        var result = _validator.TestValidate(CreateRequest(file: file));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("notes.txt", "text/plain")]
    [InlineData("image.png", "image/png")]
    public void Validate_WhenFileIsNotPdf_ShouldPass(string fileName, string contentType)
    {
        var file = CreateFile(fileName, contentType, [0x01, 0x02, 0x03]);

        var result = _validator.TestValidate(CreateRequest(file: file));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenFileIsEmpty_ShouldFail()
    {
        var file = CreateFile("empty.txt", "text/plain", []);

        var result = _validator.TestValidate(CreateRequest(file: file));

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("No file uploaded.");
    }
}
