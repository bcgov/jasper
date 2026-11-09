using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using Scv.Api.Models;
using Scv.Api.Validators;
using Scv.Db.Models;
using Xunit;

namespace tests.api.Validators;

public class AppDocumentUploadRequestValidatorTests
{
    private static readonly byte[] PdfHeader = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37];

    private readonly AppDocumentUploadRequestValidator _validator = new();

    public static IEnumerable<object[]> AppDocuments =>
        AppDocument.ALL_APP_DOCUMENTS.Select(ad => new object[] { ad.Key });

    private static AppDocument GetAppDocument(string key) =>
        AppDocument.ALL_APP_DOCUMENTS.First(ad => ad.Key == key);

    private static IFormFile CreateFile(string fileName, string contentType, byte[] content, long? length = null)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length ?? content.Length);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        return file.Object;
    }

    private static AppDocumentUploadRequest CreateRequest(string key, IFormFile file = null) => new()
    {
        Key = key,
        File = file ?? CreateFile("file", GetAppDocument(key).ContentType, PdfHeader)
    };

    [Theory]
    [MemberData(nameof(AppDocuments))]
    public void Validate_WhenRequestIsValid_ShouldPass(string key)
    {
        var result = _validator.TestValidate(CreateRequest(key));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenKeyIsMissing_ShouldFail(string key)
    {
        var request = new AppDocumentUploadRequest
        {
            Key = key,
            File = CreateFile("file.pdf", "application/pdf", PdfHeader)
        };

        var result = _validator.TestValidate(request);

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
        var request = new AppDocumentUploadRequest
        {
            Key = key,
            File = CreateFile("file.pdf", "application/pdf", PdfHeader)
        };

        var result = _validator.TestValidate(request);

        var allowedKeys = string.Join(", ", AppDocument.ALL_APP_DOCUMENTS.Select(ad => ad.Key));
        result.ShouldHaveValidationErrorFor(r => r.Key)
            .WithErrorMessage($"Key must be one of: {allowedKeys}.");
        result.ShouldNotHaveValidationErrorFor(r => r.File);
    }

    [Theory]
    [MemberData(nameof(AppDocuments))]
    public void Validate_WhenFileIsNull_ShouldFail(string key)
    {
        var request = CreateRequest(key);
        request.File = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("No file uploaded.");
    }

    [Theory]
    [MemberData(nameof(AppDocuments))]
    public void Validate_WhenFileIsEmpty_ShouldFail(string key)
    {
        var file = CreateFile("empty", GetAppDocument(key).ContentType, []);

        var result = _validator.TestValidate(CreateRequest(key, file));

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage("No file uploaded.");
    }

    [Theory]
    [MemberData(nameof(AppDocuments))]
    public void Validate_WhenFileExceedsTargetSize_ShouldFail(string key)
    {
        var appDocument = GetAppDocument(key);
        var file = CreateFile("file", appDocument.ContentType, PdfHeader, appDocument.TargetSize + 1);

        var result = _validator.TestValidate(CreateRequest(key, file));

        result.ShouldHaveValidationErrorFor(r => r.File)
            .WithErrorMessage($"File size must not exceed {appDocument.TargetSize / 1024} KB.");
    }

    [Theory]
    [MemberData(nameof(AppDocuments))]
    public void Validate_WhenFileIsExactlyTargetSize_ShouldPass(string key)
    {
        var appDocument = GetAppDocument(key);
        var file = CreateFile("file", appDocument.ContentType, PdfHeader, appDocument.TargetSize);

        var result = _validator.TestValidate(CreateRequest(key, file));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("image/png")]
    [InlineData("")]
    public void Validate_WhenContentTypeDoesNotMatch_ShouldFail(string contentType)
    {
        foreach (var appDocument in AppDocument.ALL_APP_DOCUMENTS.Where(ad => ad.ContentType != contentType))
        {
            var file = CreateFile("file", contentType, [0x01, 0x02, 0x03]);

            var result = _validator.TestValidate(CreateRequest(appDocument.Key, file));

            result.ShouldHaveValidationErrorFor(r => r.File)
                .WithErrorMessage($"File content type must be {appDocument.ContentType}.");
        }
    }

    [Theory]
    [MemberData(nameof(AppDocuments))]
    public void Validate_WhenContentTypeDiffersOnlyByCase_ShouldPass(string key)
    {
        var contentType = GetAppDocument(key).ContentType.ToUpperInvariant();
        var file = CreateFile("file", contentType, PdfHeader);

        var result = _validator.TestValidate(CreateRequest(key, file));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
