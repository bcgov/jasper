using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Documents;
using Scv.Api.Models;
using Scv.Api.Services;
using Scv.Core.Helpers.Exceptions;
using Scv.Models;
using Scv.Models.Document;
using TDCommon.Clients.DocumentsServices;
using Xunit;

namespace tests.api.Controllers
{
    public class TransitoryDocumentsControllerTests
    {
        private readonly Faker _faker;
        private readonly Mock<ITransitoryDocumentsService> _mockTransitoryDocumentsService;
        private readonly Mock<IKeycloakTokenService> _mockKeycloakTokenService;
        private readonly Mock<IDocumentMerger> _mockDocumentMerger;
        private readonly Mock<IOptions<TdApiOptions>> _mockTdApiOptions;
        private readonly TdApiOptions _defaultTdApiOptions;
        private readonly TransitoryDocumentsController _controller;

        public TransitoryDocumentsControllerTests()
        {
            _faker = new Faker();
            _mockTransitoryDocumentsService = new Mock<ITransitoryDocumentsService>();
            _mockKeycloakTokenService = new Mock<IKeycloakTokenService>();
            _mockDocumentMerger = new Mock<IDocumentMerger>();
            _mockTdApiOptions = new Mock<IOptions<TdApiOptions>>();

            _defaultTdApiOptions = new TdApiOptions
            {
                MaxFileSize = 10 * 1024 * 1024 // 10 MB
            };

            _mockTdApiOptions.Setup(o => o.Value).Returns(_defaultTdApiOptions);

            _controller = new TransitoryDocumentsController(
                _mockTransitoryDocumentsService.Object,
                _mockKeycloakTokenService.Object,
                _mockDocumentMerger.Object,
                _mockTdApiOptions.Object);

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

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.GetDocuments(locationId, roomCd, date));

            Assert.Equal("locationId is required and must be non-empty.", exception.Message);
            _mockTransitoryDocumentsService.Verify(
                s => s.ListSharedDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
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

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.GetDocuments(locationId, roomCd, date));

            Assert.Equal("roomCd is required and must be non-empty.", exception.Message);
            _mockTransitoryDocumentsService.Verify(
                s => s.ListSharedDocuments(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GetDocuments_ReturnsOk_WithDocumentList()
        {
            // Arrange
            var locationId = _faker.Random.AlphaNumeric(10);
            var roomCd = _faker.Random.AlphaNumeric(5);
            var date = DateOnly.FromDateTime(_faker.Date.Recent());
            var bearerToken = _faker.Random.AlphaNumeric(50);
            var expectedDocuments = new List<FileMetadataDto>
            {
                CreateFileMetadata(),
                CreateFileMetadata()
            };

            _mockKeycloakTokenService
                .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(bearerToken);

            _mockTransitoryDocumentsService
                .Setup(s => s.ListSharedDocuments(bearerToken, locationId, roomCd, date.ToString("yyyy-MM-dd")))
                .ReturnsAsync(expectedDocuments);

            // Act
            var result = await _controller.GetDocuments(locationId, roomCd, date);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualDocuments = Assert.IsAssignableFrom<IEnumerable<FileMetadataDto>>(okResult.Value);
            Assert.Equal(expectedDocuments.Count, actualDocuments.Count());

            _mockKeycloakTokenService.Verify(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockTransitoryDocumentsService.Verify(
                s => s.ListSharedDocuments(bearerToken, locationId, roomCd, date.ToString("yyyy-MM-dd")),
                Times.Once);
        }

        [Fact]
        public async Task GetDocuments_ReturnsOk_WithEmptyList_WhenNoDocumentsFound()
        {
            // Arrange
            var locationId = _faker.Random.AlphaNumeric(10);
            var roomCd = _faker.Random.AlphaNumeric(5);
            var date = DateOnly.FromDateTime(_faker.Date.Recent());
            var bearerToken = _faker.Random.AlphaNumeric(50);

            _mockKeycloakTokenService
                .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(bearerToken);

            _mockTransitoryDocumentsService
                .Setup(s => s.ListSharedDocuments(bearerToken, locationId, roomCd, It.IsAny<string>()))
                .ReturnsAsync(new List<FileMetadataDto>());

            // Act
            var result = await _controller.GetDocuments(locationId, roomCd, date);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualDocuments = Assert.IsAssignableFrom<IEnumerable<FileMetadataDto>>(okResult.Value);
            Assert.Empty(actualDocuments);
        }

        [Fact]
        public async Task GetDocuments_FormatsDateCorrectly()
        {
            // Arrange
            var locationId = _faker.Random.AlphaNumeric(10);
            var roomCd = _faker.Random.AlphaNumeric(5);
            var date = new DateOnly(2025, 10, 31);
            var bearerToken = _faker.Random.AlphaNumeric(50);

            _mockKeycloakTokenService
                .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(bearerToken);

            _mockTransitoryDocumentsService
                .Setup(s => s.ListSharedDocuments(bearerToken, locationId, roomCd, "2025-10-31"))
                .ReturnsAsync(new List<FileMetadataDto>());

            // Act
            await _controller.GetDocuments(locationId, roomCd, date);

            // Assert
            _mockTransitoryDocumentsService.Verify(
                s => s.ListSharedDocuments(bearerToken, locationId, roomCd, "2025-10-31"),
                Times.Once);
        }

        #endregion

        #region DownloadFile Tests

        [Fact]
        public async Task DownloadFile_ThrowsBadRequestException_WhenFileMetadataIsNull()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.DownloadFile(null));

            Assert.Equal("fileMetadata is required.", exception.Message);
            _mockTransitoryDocumentsService.Verify(
                s => s.DownloadFile(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DownloadFile_ThrowsBadRequestException_WhenAbsolutePathIsInvalid(string absolutePath)
        {
            // Arrange
            var fileMetadata = new FileMetadataDto
            {
                AbsolutePath = absolutePath,
                SizeBytes = 1024
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.DownloadFile(fileMetadata));

            Assert.Equal("AbsolutePath is required and must be non-empty.", exception.Message);
            _mockTransitoryDocumentsService.Verify(
                s => s.DownloadFile(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task DownloadFile_ThrowsBadRequestException_WhenSizeBytesIsNegative()
        {
            // Arrange
            var fileMetadata = new FileMetadataDto
            {
                AbsolutePath = _faker.System.FilePath(),
                SizeBytes = -1
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.DownloadFile(fileMetadata));

            Assert.Equal("SizeBytes must be greater than or equal to 0.", exception.Message);
            _mockTransitoryDocumentsService.Verify(
                s => s.DownloadFile(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task DownloadFile_ThrowsBadRequestException_WhenSizeBytesExceedsMaxFileSize()
        {
            // Arrange
            var fileMetadata = new FileMetadataDto
            {
                AbsolutePath = _faker.System.FilePath(),
                SizeBytes = _defaultTdApiOptions.MaxFileSize + 1
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.DownloadFile(fileMetadata));

            var maxSizeMB = _defaultTdApiOptions.MaxFileSize / 1024.0 / 1024.0;
            Assert.Equal($"File size exceeds maximum allowed size of {maxSizeMB:F2} MB.", exception.Message);
            _mockTransitoryDocumentsService.Verify(
                s => s.DownloadFile(It.IsAny<string>(), It.IsAny<string>()),
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
            var bearerToken = _faker.Random.AlphaNumeric(50);

            var fileMetadata = new FileMetadataDto
            {
                AbsolutePath = absolutePath,
                SizeBytes = fileContent.Length
            };

            var fileResponse = new FileStreamResponse(stream, fileName, contentType);

            _mockKeycloakTokenService
                .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(bearerToken);

            _mockTransitoryDocumentsService
                .Setup(s => s.DownloadFile(bearerToken, absolutePath))
                .ReturnsAsync(fileResponse);

            // Act
            var result = await _controller.DownloadFile(fileMetadata);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal(fileName, fileResult.FileDownloadName);
            Assert.Equal(contentType, fileResult.ContentType);
            Assert.True(fileResult.EnableRangeProcessing);
            Assert.Equal(stream, fileResult.FileStream);

            _mockKeycloakTokenService.Verify(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockTransitoryDocumentsService.Verify(s => s.DownloadFile(bearerToken, absolutePath), Times.Once);
        }

        [Fact]
        public async Task DownloadFile_EnablesRangeProcessing()
        {
            // Arrange
            var absolutePath = _faker.System.FilePath();
            var stream = new MemoryStream(_faker.Random.Bytes(1024));
            var bearerToken = _faker.Random.AlphaNumeric(50);

            var fileMetadata = new FileMetadataDto
            {
                AbsolutePath = absolutePath,
                SizeBytes = 1024
            };

            var fileResponse = new FileStreamResponse(stream, _faker.System.FileName(), "application/pdf");

            _mockKeycloakTokenService
                .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(bearerToken);

            _mockTransitoryDocumentsService
                .Setup(s => s.DownloadFile(bearerToken, absolutePath))
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
            var bearerToken = _faker.Random.AlphaNumeric(50);

            var fileMetadata = new FileMetadataDto
            {
                AbsolutePath = absolutePath,
                SizeBytes = fileSize
            };

            var fileResponse = new FileStreamResponse(stream, _faker.System.FileName(), "application/pdf");

            _mockKeycloakTokenService
                .Setup(s => s.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(bearerToken);

            _mockTransitoryDocumentsService
                .Setup(s => s.DownloadFile(bearerToken, absolutePath))
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
            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.MergePdfs(null));

            Assert.Equal("files are required and must contain at least one file.", exception.Message);
            _mockDocumentMerger.Verify(
                m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
                Times.Never);
        }

        [Fact]
        public async Task MergePdfs_ThrowsBadRequestException_WhenFilesArrayIsNull()
        {
            // Arrange
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = null
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.MergePdfs(request));

            Assert.Equal("files are required and must contain at least one file.", exception.Message);
            _mockDocumentMerger.Verify(
                m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
                Times.Never);
        }

        [Fact]
        public async Task MergePdfs_ThrowsBadRequestException_WhenFilesArrayIsEmpty()
        {
            // Arrange
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = Array.Empty<FileMetadataDto>()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.MergePdfs(request));

            Assert.Equal("files are required and must contain at least one file.", exception.Message);
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
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = new[]
                {
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                    new FileMetadataDto { AbsolutePath = invalidPath, SizeBytes = 2048 }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.MergePdfs(request));

            Assert.Equal("All files must have a valid AbsolutePath.", exception.Message);
            _mockDocumentMerger.Verify(
                m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
                Times.Never);
        }

        [Fact]
        public async Task MergePdfs_ThrowsBadRequestException_WhenAnyFileHasNegativeSizeBytes()
        {
            // Arrange
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = new[]
                {
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = -1 }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.MergePdfs(request));

            Assert.Equal("All files must have SizeBytes greater than or equal to 0.", exception.Message);
            _mockDocumentMerger.Verify(
                m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()),
                Times.Never);
        }

        [Fact]
        public async Task MergePdfs_ThrowsBadRequestException_WhenTotalSizeExceedsMaxFileSize()
        {
            // Arrange
            var halfMax = _defaultTdApiOptions.MaxFileSize / 2 + 1;
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = new[]
                {
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = halfMax },
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = halfMax }
                }
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<BadRequestException>(
                async () => await _controller.MergePdfs(request));

            var maxSizeMB = _defaultTdApiOptions.MaxFileSize / 1024.0 / 1024.0;
            exception.Message.Should().Contain($"exceeds maximum allowed size of {maxSizeMB:F2} MB");
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
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = new[]
                {
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 2048 }
                }
            };

            var expectedResponse = new PdfDocumentResponse
            {
                Base64Pdf = mergedContent
            };

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
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = new[]
                {
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 }
                }
            };

            PdfDocumentRequest[] capturedRequests = null;

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
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = new[]
                {
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 2048 },
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 4096 }
                }
            };

            PdfDocumentRequest[] capturedRequests = null;

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
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = new[]
                {
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 }
                }
            };

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
            var request = new TransitoryDocumentsController.MergePdfsRequest
            {
                Files = new[]
                {
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 1024 },
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 2048 },
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 4096 },
                    new FileMetadataDto { AbsolutePath = _faker.System.FilePath(), SizeBytes = 8192 }
                }
            };

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

        private FileMetadataDto CreateFileMetadata()
        {
            var fileName = _faker.System.FileName();
            return new FileMetadataDto
            {
                FileName = fileName,
                Extension = Path.GetExtension(fileName),
                SizeBytes = _faker.Random.Long(1000, 1000000),
                CreatedUtc = _faker.Date.Recent(),
                AbsolutePath = _faker.System.FilePath(),
                MatchedRoomFolder = _faker.Random.Bool() ? _faker.Random.AlphaNumeric(5) : null
            };
        }

        #endregion
    }
}
