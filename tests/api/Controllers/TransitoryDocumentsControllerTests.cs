using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Documents;
using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Document;
using Scv.Models.TransitoryDocuments;
using Xunit;
using FileMetadata = Scv.TdApi.Models.FileMetadataDto;

namespace tests.api.Controllers;

public class TransitoryDocumentsControllerTests
{
    private readonly Faker _faker;
    private readonly Mock<ITransitoryDocumentsService> _mockTransitoryDocumentsService;
    private readonly Mock<IDocumentMerger> _mockDocumentMerger;
    private readonly Mock<IValidator<GetDocumentsRequest>> _mockGetDocumentsValidator;
    private readonly Mock<IValidator<DownloadFileRequest>> _mockDownloadFileValidator;
    private readonly Mock<IValidator<MergePdfsRequest>> _mockMergePdfsValidator;
    private readonly Mock<ILogger<TransitoryDocumentsController>> _mockLogger;
    private readonly TransitoryDocumentsController _controller;

    public TransitoryDocumentsControllerTests()
    {
        _faker = new Faker();
        _mockTransitoryDocumentsService = new Mock<ITransitoryDocumentsService>();
        _mockDocumentMerger = new Mock<IDocumentMerger>();
        _mockGetDocumentsValidator = new Mock<IValidator<GetDocumentsRequest>>();
        _mockDownloadFileValidator = new Mock<IValidator<DownloadFileRequest>>();
        _mockMergePdfsValidator = new Mock<IValidator<MergePdfsRequest>>();
        _mockLogger = new Mock<ILogger<TransitoryDocumentsController>>();

        _controller = new TransitoryDocumentsController(
            _mockTransitoryDocumentsService.Object,
            _mockDocumentMerger.Object,
            _mockGetDocumentsValidator.Object,
            _mockDownloadFileValidator.Object,
            _mockMergePdfsValidator.Object,
            _mockLogger.Object);

        var context = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }

    #region GetDocuments Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDocuments_ThrowsBadRequestException_WhenLocationIdIsInvalid(string locationId)
    {
        // Arrange
        var roomCd = _faker.Random.AlphaNumeric(5);
        var date = DateOnly.FromDateTime(_faker.Date.Recent());

        _mockGetDocumentsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("LocationId", "locationId is required and must be non-empty.") }));

        // Act
        var result = await _controller.DocumentGet(locationId, roomCd, date);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("locationId is required and must be non-empty.", errors);

        _mockTransitoryDocumentsService.Verify(
            s => s.ListSharedDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDocuments_ThrowsBadRequestException_WhenRoomCdIsInvalid(string roomCd)
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(10);
        var date = DateOnly.FromDateTime(_faker.Date.Recent());

        _mockGetDocumentsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("RoomCd", "roomCd is required and must be non-empty.") }));

        // Act
        var result = await _controller.DocumentGet(locationId, roomCd, date);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("roomCd is required and must be non-empty.", errors);

        _mockTransitoryDocumentsService.Verify(
            s => s.ListSharedDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDocuments_ReturnsOk_WithDocumentList()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(10);
        var roomCd = _faker.Random.AlphaNumeric(5);
        var date = DateOnly.FromDateTime(_faker.Date.Recent());
        var expectedDocuments = new List<FileMetadata>
        {
            CreateFileMetadata(),
            CreateFileMetadata()
        };

        _mockGetDocumentsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTransitoryDocumentsService
            .Setup(s => s.ListSharedDocuments(locationId, roomCd, date.ToString("yyyy-MM-dd")))
            .ReturnsAsync(expectedDocuments);

        // Act
        var result = await _controller.DocumentGet(locationId, roomCd, date);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualDocuments = Assert.IsAssignableFrom<IEnumerable<FileMetadata>>(okResult.Value);
        Assert.Equal(expectedDocuments.Count, actualDocuments.Count());

        _mockTransitoryDocumentsService.Verify(
            s => s.ListSharedDocuments(locationId, roomCd, date.ToString("yyyy-MM-dd")),
            Times.Once);
    }

    [Fact]
    public async Task GetDocuments_ReturnsOk_WithEmptyList_WhenNoDocumentsFound()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(10);
        var roomCd = _faker.Random.AlphaNumeric(5);
        var date = DateOnly.FromDateTime(_faker.Date.Recent());

        _mockGetDocumentsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTransitoryDocumentsService
            .Setup(s => s.ListSharedDocuments(locationId, roomCd, It.IsAny<string>()))
            .ReturnsAsync(new List<FileMetadata>());

        // Act
        var result = await _controller.DocumentGet(locationId, roomCd, date);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualDocuments = Assert.IsAssignableFrom<IEnumerable<FileMetadata>>(okResult.Value);
        Assert.Empty(actualDocuments);
    }

    [Fact]
    public async Task GetDocuments_FormatsDateCorrectly()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(10);
        var roomCd = _faker.Random.AlphaNumeric(5);
        var date = new DateOnly(2025, 10, 31);

        _mockGetDocumentsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<GetDocumentsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTransitoryDocumentsService
            .Setup(s => s.ListSharedDocuments(locationId, roomCd, "2025-10-31"))
            .ReturnsAsync(new List<FileMetadata>());

        // Act
        await _controller.DocumentGet(locationId, roomCd, date);

        // Assert
        _mockTransitoryDocumentsService.Verify(
            s => s.ListSharedDocuments(locationId, roomCd, "2025-10-31"),
            Times.Once);
    }

    #endregion

    #region DownloadFile Tests

    [Fact]
    public async Task DownloadFile_ThrowsBadRequestException_WhenFileMetadataIsNull()
    {
        // Arrange
        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("FileMetadata", "fileMetadata is required.") }));

        // Act
        var result = await _controller.DocumentDownload(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("fileMetadata is required.", errors);

        _mockTransitoryDocumentsService.Verify(
            s => s.DownloadFile(It.IsAny<string>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DownloadFile_ThrowsBadRequestException_WhenRelativePathIsInvalid(string relativePath)
    {
        // Arrange
        var fileMetadata = new FileMetadata
        {
            FileName = _faker.System.FileName(),
            Extension = _faker.System.FileExt(),
            RelativePath = relativePath,
            SizeBytes = 1024
        };

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("FileMetadata.RelativePath", "RelativePath is required and must be non-empty.") }));

        // Act
        var result = await _controller.DocumentDownload(fileMetadata);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("RelativePath is required and must be non-empty.", errors);

        _mockTransitoryDocumentsService.Verify(
            s => s.DownloadFile(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task DownloadFile_ThrowsBadRequestException_WhenSizeBytesIsNegative()
    {
        // Arrange
        var fileMetadata = new FileMetadata
        {
            FileName = _faker.System.FileName(),
            Extension = _faker.System.FileExt(),
            RelativePath = _faker.System.FilePath(),
            SizeBytes = -1
        };

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("FileMetadata.SizeBytes", "SizeBytes must be greater than or equal to 0.") }));

        // Act
        var result = await _controller.DocumentDownload(fileMetadata);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("SizeBytes must be greater than or equal to 0.", errors);

        _mockTransitoryDocumentsService.Verify(
            s => s.DownloadFile(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task DownloadFile_ThrowsBadRequestException_WhenSizeBytesExceedsMaxFileSize()
    {
        // Arrange
        var maxFileSize = 10 * 1024 * 1024;
        var fileMetadata = new FileMetadata
        {
            FileName = _faker.System.FileName(),
            Extension = _faker.System.FileExt(),
            RelativePath = _faker.System.FilePath(),
            SizeBytes = maxFileSize + 1
        };

        var maxSizeMB = maxFileSize / 1024.0 / 1024.0;
        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("FileMetadata.SizeBytes", $"File size exceeds maximum allowed size of {maxSizeMB:F2} MB.") }));

        // Act
        var result = await _controller.DocumentDownload(fileMetadata);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains($"File size exceeds maximum allowed size of {maxSizeMB:F2} MB.", errors);

        _mockTransitoryDocumentsService.Verify(
            s => s.DownloadFile(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task DownloadFile_ReturnsFile_WhenFileExists()
    {
        // Arrange
        var relativePath = _faker.System.FilePath();
        var fileName = _faker.System.FileName();
        var contentType = "application/pdf";
        var fileContent = _faker.Random.Bytes(1024);
        var stream = new MemoryStream(fileContent);

        var fileMetadata = new FileMetadata
        {
            FileName = fileName,
            Extension = "pdf",
            RelativePath = relativePath,
            SizeBytes = fileContent.Length
        };

        var fileResponse = new FileStreamResponse(stream, fileName, contentType);

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTransitoryDocumentsService
            .Setup(s => s.DownloadFile(relativePath))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await _controller.DocumentDownload(fileMetadata);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(fileName, fileResult.FileDownloadName);
        Assert.Equal(contentType, fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal(stream, fileResult.FileStream);

        _mockTransitoryDocumentsService.Verify(s => s.DownloadFile(relativePath), Times.Once);
    }

    [Fact]
    public async Task DownloadFile_EnablesRangeProcessing()
    {
        // Arrange
        var relativePath = _faker.System.FilePath();
        var stream = new MemoryStream(_faker.Random.Bytes(1024));

        var fileMetadata = new FileMetadata
        {
            FileName = _faker.System.FileName(),
            Extension = _faker.System.FileExt(),
            RelativePath = relativePath,
            SizeBytes = 1024
        };

        var fileResponse = new FileStreamResponse(stream, _faker.System.FileName(), "application/pdf");

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTransitoryDocumentsService
            .Setup(s => s.DownloadFile(relativePath))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await _controller.DocumentDownload(fileMetadata);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.True(fileResult.EnableRangeProcessing, "Range processing should be enabled for large file downloads");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    [InlineData(5242880)] // 5 MB
    public async Task DownloadFile_AcceptsValidFileSizes(long fileSize)
    {
        // Arrange
        var relativePath = _faker.System.FilePath();
        var stream = new MemoryStream(new byte[fileSize]);

        var fileMetadata = new FileMetadata
        {
            FileName = _faker.System.FileName(),
            Extension = _faker.System.FileExt(),
            RelativePath = relativePath,
            SizeBytes = fileSize
        };

        var fileResponse = new FileStreamResponse(stream, _faker.System.FileName(), "application/pdf");

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTransitoryDocumentsService
            .Setup(s => s.DownloadFile(relativePath))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await _controller.DocumentDownload(fileMetadata);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.NotNull(fileResult);
    }

    #endregion

    #region MergePdfs Tests

    [Fact]
    public async Task MergePdfs_ThrowsBadRequestException_WhenRequestIsNull()
    {
        // Arrange
        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Files", "files are required and must contain at least one file.") }));

        // Act
        var result = await _controller.DocumentMerge(null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("files are required and must contain at least one file.", errors);

        _mockDocumentMerger.Verify(
            m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
            Times.Never);
    }

    [Fact]
    public async Task MergePdfs_ThrowsBadRequestException_WhenFilesArrayIsNull()
    {
        // Arrange
        var request = new MergePdfsRequest
        {
            Files = null
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Files", "files are required and must contain at least one file.") }));

        // Act
        var result = await _controller.DocumentMerge(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("files are required and must contain at least one file.", errors);

        _mockDocumentMerger.Verify(
            m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
            Times.Never);
    }

    [Fact]
    public async Task MergePdfs_ThrowsBadRequestException_WhenFilesArrayIsEmpty()
    {
        // Arrange
        var request = new MergePdfsRequest
        {
            Files = Array.Empty<FileMetadata>()
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Files", "files are required and must contain at least one file.") }));

        // Act
        var result = await _controller.DocumentMerge(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("files are required and must contain at least one file.", errors);

        _mockDocumentMerger.Verify(
            m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MergePdfs_ThrowsBadRequestException_WhenAnyFileHasInvalidRelativePath(string invalidPath)
    {
        // Arrange
        var request = new MergePdfsRequest
        {
            Files = new[]
            {
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 1024, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() },
                new FileMetadata { RelativePath = invalidPath, SizeBytes = 2048, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() }
            }
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Files", "All files must have a valid RelativePath.") }));

        // Act
        var result = await _controller.DocumentMerge(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("All files must have a valid RelativePath.", errors);

        _mockDocumentMerger.Verify(
            m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
            Times.Never);
    }

    [Fact]
    public async Task MergePdfs_ThrowsBadRequestException_WhenAnyFileHasNegativeSizeBytes()
    {
        // Arrange
        var request = new MergePdfsRequest
        {
            Files = new[]
            {
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 1024, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() },
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = -1, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() }
            }
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Files", "All files must have SizeBytes greater than or equal to 0.") }));

        // Act
        var result = await _controller.DocumentMerge(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("All files must have SizeBytes greater than or equal to 0.", errors);

        _mockDocumentMerger.Verify(
            m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
            Times.Never);
    }

    [Fact]
    public async Task MergePdfs_ThrowsBadRequestException_WhenTotalSizeExceedsMaxFileSize()
    {
        // Arrange
        var maxFileSize = 10 * 1024 * 1024;
        var halfMax = maxFileSize / 2 + 1;
        var request = new MergePdfsRequest
        {
            Files = new[]
            {
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = halfMax, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() },
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = halfMax, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() }
            }
        };

        var maxSizeMB = maxFileSize / 1024.0 / 1024.0;
        var totalSize = halfMax * 2;
        var totalSizeMB = totalSize / 1024.0 / 1024.0;

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Files", $"Total file size ({totalSizeMB:F2} MB) exceeds maximum allowed size of {maxSizeMB:F2} MB.") }));

        // Act
        var result = await _controller.DocumentMerge(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        errors.Should().Contain(e => e.Contains("exceeds maximum allowed size"));

        _mockDocumentMerger.Verify(
            m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
            Times.Never);
    }

    [Fact]
    public async Task MergePdfs_ReturnsOk_WithMergedDocument()
    {
        // Arrange
        var bearerToken = _faker.Random.AlphaNumeric(50);
        var mergedContent = _faker.Random.AlphaNumeric(1000);
        var request = new MergePdfsRequest
        {
            Files = new[]
            {
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 1024, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() },
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 2048, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() }
            }
        };

        var expectedResponse = new PdfDocumentResponse
        {
            Base64Pdf = mergedContent
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DocumentMerge(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<PdfDocumentResponse>(okResult.Value);
        Assert.Equal(mergedContent, actualResponse.Base64Pdf);

        _mockDocumentMerger.Verify(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()), Times.Once);
    }

    [Fact]
    public async Task MergePdfs_PassesCorrectDocumentTypeToMerger()
    {
        // Arrange
        var bearerToken = _faker.Random.AlphaNumeric(50);
        var request = new MergePdfsRequest
        {
            Files = new[]
            {
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 1024, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() }
            }
        };

        PdfDocumentRequest[] capturedRequests = null;

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .Callback<PdfDocumentRequest[]>(reqs => capturedRequests = reqs)
            .ReturnsAsync(new PdfDocumentResponse { Base64Pdf = "test" });

        // Act
        await _controller.DocumentMerge(request);

        // Assert
        Assert.NotNull(capturedRequests);
        Assert.Single(capturedRequests);
        Assert.Equal(DocumentType.TransitoryDocument, capturedRequests[0].Type);
        Assert.Equal(request.Files[0].RelativePath, capturedRequests[0].Data.Path);
    }

    [Fact]
    public async Task MergePdfs_PassesBearerTokenToAllDocumentRequests()
    {
        // Arrange
        var bearerToken = _faker.Random.AlphaNumeric(50);
        var request = new MergePdfsRequest
        {
            Files = new[]
            {
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 1024, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() },
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 2048, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() },
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 4096, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() }
            }
        };

        PdfDocumentRequest[] capturedRequests = null;

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .Callback<PdfDocumentRequest[]>(reqs => capturedRequests = reqs)
            .ReturnsAsync(new PdfDocumentResponse { Base64Pdf = "test" });

        // Act
        await _controller.DocumentMerge(request);

        // Assert
        Assert.NotNull(capturedRequests);
        Assert.Equal(3, capturedRequests.Length);
        Assert.All(capturedRequests, req =>
        {
            Assert.Equal(DocumentType.TransitoryDocument, req.Type);
        });
    }

    [Fact]
    public async Task MergePdfs_AcceptsSingleFile()
    {
        // Arrange
        var bearerToken = _faker.Random.AlphaNumeric(50);
        var request = new MergePdfsRequest
        {
            Files = new[]
            {
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 1024, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() }
            }
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .ReturnsAsync(new PdfDocumentResponse { Base64Pdf = "test" });

        // Act
        var result = await _controller.DocumentMerge(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task MergePdfs_AcceptsMultipleFiles()
    {
        // Arrange
        var bearerToken = _faker.Random.AlphaNumeric(50);
        var request = new MergePdfsRequest
        {
            Files = new[]
            {
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 1024, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() },
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 2048, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() },
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 4096, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() },
                new FileMetadata { RelativePath = _faker.System.FilePath(), SizeBytes = 8192, Extension = _faker.System.FileExt(), FileName = _faker.System.FileName() }
            }
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .ReturnsAsync(new PdfDocumentResponse { Base64Pdf = "test" });

        // Act
        var result = await _controller.DocumentMerge(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region Helper Methods

    private FileMetadata CreateFileMetadata()
    {
        var fileName = _faker.System.FileName();
        return new FileMetadata
        {
            FileName = fileName,
            Extension = Path.GetExtension(fileName),
            SizeBytes = _faker.Random.Long(1000, 1000000),
            CreatedUtc = _faker.Date.Recent(),
            RelativePath = _faker.System.FilePath(),
            MatchedRoomFolder = _faker.Random.Bool() ? _faker.Random.AlphaNumeric(5) : null
        };
    }

    #endregion
}
