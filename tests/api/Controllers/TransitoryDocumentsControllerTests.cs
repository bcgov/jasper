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
using Microsoft.Extensions.Options;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Documents;
using Scv.Api.Models.TransitoryDocuments;
using Scv.Api.Services;
using Scv.Core.Helpers.Exceptions;
using Scv.Models;
using Scv.Models.Document;
using Xunit;
using FileMetadata = Scv.TdApi.Models.FileMetadataDto;

namespace tests.api.Controllers;

public class TransitoryDocumentsControllerTests
{
    private readonly Faker _faker;
    private readonly Mock<ITransitoryDocumentsService> _mockTransitoryDocumentsService;
    private readonly Mock<IKeycloakTokenService> _mockKeycloakTokenService;
    private readonly Mock<IDocumentMerger> _mockDocumentMerger;
    private readonly Mock<IValidator<GetDocumentsRequest>> _mockGetDocumentsValidator;
    private readonly Mock<IValidator<DownloadFileRequest>> _mockDownloadFileValidator;
    private readonly Mock<IValidator<MergePdfsRequest>> _mockMergePdfsValidator;
    private readonly TransitoryDocumentsController _controller;

    public TransitoryDocumentsControllerTests()
    {
        _faker = new Faker();
        _mockTransitoryDocumentsService = new Mock<ITransitoryDocumentsService>();
        _mockKeycloakTokenService = new Mock<IKeycloakTokenService>();
        _mockDocumentMerger = new Mock<IDocumentMerger>();
        _mockGetDocumentsValidator = new Mock<IValidator<GetDocumentsRequest>>();
        _mockDownloadFileValidator = new Mock<IValidator<DownloadFileRequest>>();
        _mockMergePdfsValidator = new Mock<IValidator<MergePdfsRequest>>();

        _controller = new TransitoryDocumentsController(
            _mockTransitoryDocumentsService.Object,
            _mockKeycloakTokenService.Object,
            _mockDocumentMerger.Object,
            _mockGetDocumentsValidator.Object,
            _mockDownloadFileValidator.Object,
            _mockMergePdfsValidator.Object);

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
        var result = await _controller.GetDocuments(locationId, roomCd, date);

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
        var result = await _controller.GetDocuments(locationId, roomCd, date);

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
        var expectedDocuments = new List<TDCommon.Clients.DocumentsServices.FileMetadataDto>
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
        var result = await _controller.GetDocuments(locationId, roomCd, date);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualDocuments = Assert.IsAssignableFrom<IEnumerable<TDCommon.Clients.DocumentsServices.FileMetadataDto>>(okResult.Value);
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
            .ReturnsAsync(new List<TDCommon.Clients.DocumentsServices.FileMetadataDto>());

        // Act
        var result = await _controller.GetDocuments(locationId, roomCd, date);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualDocuments = Assert.IsAssignableFrom<IEnumerable<TDCommon.Clients.DocumentsServices.FileMetadataDto>>(okResult.Value);
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
            .ReturnsAsync(new List<TDCommon.Clients.DocumentsServices.FileMetadataDto>());

        // Act
        await _controller.GetDocuments(locationId, roomCd, date);

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
        var result = await _controller.DownloadFile(null);

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
    public async Task DownloadFile_ThrowsBadRequestException_WhenAbsolutePathIsInvalid(string absolutePath)
    {
        // Arrange
        var fileMetadata = new FileMetadata
        {
            AbsolutePath = absolutePath,
            SizeBytes = 1024
        };

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("FileMetadata.AbsolutePath", "AbsolutePath is required and must be non-empty.") }));

        // Act
        var result = await _controller.DownloadFile(fileMetadata);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("AbsolutePath is required and must be non-empty.", errors);

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
            AbsolutePath = _faker.System.FilePath(),
            SizeBytes = -1
        };

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("FileMetadata.SizeBytes", "SizeBytes must be greater than or equal to 0.") }));

        // Act
        var result = await _controller.DownloadFile(fileMetadata);

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
            AbsolutePath = _faker.System.FilePath(),
            SizeBytes = maxFileSize + 1
        };

        var maxSizeMB = maxFileSize / 1024.0 / 1024.0;
        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("FileMetadata.SizeBytes", $"File size exceeds maximum allowed size of {maxSizeMB:F2} MB.") }));

        // Act
        var result = await _controller.DownloadFile(fileMetadata);

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
        var absolutePath = _faker.System.FilePath();
        var fileName = _faker.System.FileName();
        var contentType = "application/pdf";
        var fileContent = _faker.Random.Bytes(1024);
        var stream = new MemoryStream(fileContent);

        var fileMetadata = new FileMetadata
        {
            AbsolutePath = absolutePath,
            SizeBytes = fileContent.Length
        };

        var fileResponse = new FileStreamResponse(stream, fileName, contentType);

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTransitoryDocumentsService
            .Setup(s => s.DownloadFile(absolutePath))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await _controller.DownloadFile(fileMetadata);

        // Assert
        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal(fileName, fileResult.FileDownloadName);
        Assert.Equal(contentType, fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal(stream, fileResult.FileStream);

        _mockTransitoryDocumentsService.Verify(s => s.DownloadFile(absolutePath), Times.Once);
    }

    [Fact]
    public async Task DownloadFile_EnablesRangeProcessing()
    {
        // Arrange
        var absolutePath = _faker.System.FilePath();
        var stream = new MemoryStream(_faker.Random.Bytes(1024));

        var fileMetadata = new FileMetadata
        {
            AbsolutePath = absolutePath,
            SizeBytes = 1024
        };

        var fileResponse = new FileStreamResponse(stream, _faker.System.FileName(), "application/pdf");

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTransitoryDocumentsService
            .Setup(s => s.DownloadFile(absolutePath))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await _controller.DownloadFile(fileMetadata);

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
        var absolutePath = _faker.System.FilePath();
        var stream = new MemoryStream(new byte[fileSize]);

        var fileMetadata = new FileMetadata
        {
            AbsolutePath = absolutePath,
            SizeBytes = fileSize
        };

        var fileResponse = new FileStreamResponse(stream, _faker.System.FileName(), "application/pdf");

        _mockDownloadFileValidator
            .Setup(v => v.ValidateAsync(It.IsAny<DownloadFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockTransitoryDocumentsService
            .Setup(s => s.DownloadFile(absolutePath))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await _controller.DownloadFile(fileMetadata);

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
        var result = await _controller.MergePdfs(null);

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
        var result = await _controller.MergePdfs(request);

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
        var result = await _controller.MergePdfs(request);

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
    public async Task MergePdfs_ThrowsBadRequestException_WhenAnyFileHasInvalidAbsolutePath(string invalidPath)
    {
        // Arrange
        var request = new MergePdfsRequest
        {
            Files = new[]
            {
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                new FileMetadata { AbsolutePath = invalidPath, SizeBytes = 2048 }
            }
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Files", "All files must have a valid AbsolutePath.") }));

        // Act
        var result = await _controller.MergePdfs(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
        Assert.Contains("All files must have a valid AbsolutePath.", errors);

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
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = -1 }
            }
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(
                new[] { new FluentValidation.Results.ValidationFailure("Files", "All files must have SizeBytes greater than or equal to 0.") }));

        // Act
        var result = await _controller.MergePdfs(request);

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
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = halfMax },
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = halfMax }
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
        var result = await _controller.MergePdfs(request);

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
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 2048 }
            }
        };

        var expectedResponse = new PdfDocumentResponse
        {
            Base64Pdf = mergedContent
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockKeycloakTokenService
            .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bearerToken);

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.MergePdfs(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var actualResponse = Assert.IsType<PdfDocumentResponse>(okResult.Value);
        Assert.Equal(mergedContent, actualResponse.Base64Pdf);

        _mockKeycloakTokenService.Verify(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
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
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 }
            }
        };

        PdfDocumentRequest[] capturedRequests = null;

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockKeycloakTokenService
            .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bearerToken);

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .Callback<PdfDocumentRequest[]>(reqs => capturedRequests = reqs)
            .ReturnsAsync(new PdfDocumentResponse { Base64Pdf = "test" });

        // Act
        await _controller.MergePdfs(request);

        // Assert
        Assert.NotNull(capturedRequests);
        Assert.Single(capturedRequests);
        Assert.Equal(DocumentType.TransitoryDocument, capturedRequests[0].Type);
        Assert.Equal(request.Files[0].AbsolutePath, capturedRequests[0].Data.Path);
        Assert.Equal(bearerToken, capturedRequests[0].Data.BearerToken);
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
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 2048 },
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 4096 }
            }
        };

        PdfDocumentRequest[] capturedRequests = null;

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockKeycloakTokenService
            .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bearerToken);

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .Callback<PdfDocumentRequest[]>(reqs => capturedRequests = reqs)
            .ReturnsAsync(new PdfDocumentResponse { Base64Pdf = "test" });

        // Act
        await _controller.MergePdfs(request);

        // Assert
        Assert.NotNull(capturedRequests);
        Assert.Equal(3, capturedRequests.Length);
        Assert.All(capturedRequests, req =>
        {
            Assert.Equal(bearerToken, req.Data.BearerToken);
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
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 }
            }
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockKeycloakTokenService
            .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bearerToken);

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .ReturnsAsync(new PdfDocumentResponse { Base64Pdf = "test" });

        // Act
        var result = await _controller.MergePdfs(request);

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
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 2048 },
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 4096 },
                new FileMetadata { AbsolutePath = _faker.System.FilePath(), SizeBytes = 8192 }
            }
        };

        _mockMergePdfsValidator
            .Setup(v => v.ValidateAsync(It.IsAny<MergePdfsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _mockKeycloakTokenService
            .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bearerToken);

        _mockDocumentMerger
            .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .ReturnsAsync(new PdfDocumentResponse { Base64Pdf = "test" });

        // Act
        var result = await _controller.MergePdfs(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region Helper Methods

    private TDCommon.Clients.DocumentsServices.FileMetadataDto CreateFileMetadata()
    {
        var fileName = _faker.System.FileName();
        return new TDCommon.Clients.DocumentsServices.FileMetadataDto
        {
            FileName = fileName,
            Extension = Path.GetExtension(fileName),
            SizeBytes = _faker.Random.Long(1000, 1000000),
            CreatedUtc = DateTimeOffset.FromFileTime(_faker.Date.Recent().Ticks),
            AbsolutePath = _faker.System.FilePath(),
            MatchedRoomFolder = _faker.Random.Bool() ? _faker.Random.AlphaNumeric(5) : null
        };
    }

    #endregion
}
