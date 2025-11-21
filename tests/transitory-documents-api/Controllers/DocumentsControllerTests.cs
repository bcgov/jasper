using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Models;
using Scv.Models.TransitoryDocuments;
using Scv.TdApi.Controllers;
using Scv.TdApi.Models;
using Scv.TdApi.Services;
using Xunit;

namespace tests.tdApi.Tests.Controllers
{
    public class DocumentsControllerTests
    {
        private readonly Faker _faker;
        private readonly Mock<ISharedDriveFileService> _mockService;
        private readonly Mock<ILogger<DocumentsController>> _mockLogger;
        private readonly DocumentsController _controller;

        public DocumentsControllerTests()
        {
            _faker = new Faker();
            _mockService = new Mock<ISharedDriveFileService>();
            _mockLogger = new Mock<ILogger<DocumentsController>>();
            _controller = new DocumentsController(_mockService.Object, _mockLogger.Object);

            var context = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };
        }

        #region Search Tests

        [Fact]
        public async Task Search_ReturnsBadRequest_WhenRequestIsNull()
        {
            // Act
            var result = await _controller.Search(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Request body is required.", badRequestResult.Value);
            _mockService.Verify(s => s.FindFilesAsync(It.IsAny<TransitoryDocumentSearchRequest>()), Times.Never);
        }

        [Fact]
        public async Task Search_ReturnsOk_WithFilesList_WhenFilesFound()
        {
            // Arrange
            var request = CreateValidSearchRequest();
            var expectedFiles = new List<FileMetadataDto>
            {
                CreateFileMetadata(),
                CreateFileMetadata()
            };

            _mockService.Setup(s => s.FindFilesAsync(request))
                       .ReturnsAsync(expectedFiles);

            // Act
            var result = await _controller.Search(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualFiles = Assert.IsAssignableFrom<IReadOnlyList<FileMetadataDto>>(okResult.Value);
            Assert.Equal(expectedFiles.Count, actualFiles.Count);
            Assert.Equal(expectedFiles, actualFiles);

            _mockService.Verify(s => s.FindFilesAsync(request), Times.Once);
        }

        [Fact]
        public async Task Search_ReturnsOk_WithEmptyList_WhenNoFilesFound()
        {
            // Arrange
            var request = CreateValidSearchRequest();
            var expectedFiles = new List<FileMetadataDto>();

            _mockService.Setup(s => s.FindFilesAsync(request))
                       .ReturnsAsync(expectedFiles);

            // Act
            var result = await _controller.Search(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualFiles = Assert.IsAssignableFrom<IReadOnlyList<FileMetadataDto>>(okResult.Value);
            Assert.Empty(actualFiles);

            _mockService.Verify(s => s.FindFilesAsync(request), Times.Once);
        }

        [Fact]
        public async Task Search_HandlesRequestWithNullRoomCd()
        {
            // Arrange
            var request = new TransitoryDocumentSearchRequest
            {
                RegionCode = _faker.Address.StateAbbr(),
                RegionName = _faker.Address.State(),
                AgencyIdentifierCd = _faker.Random.AlphaNumeric(10),
                LocationShortName = _faker.Address.City(),
                RoomCd = null,
                Date = DateOnly.FromDateTime(_faker.Date.Recent())
            };

            _mockService.Setup(s => s.FindFilesAsync(request))
                       .ReturnsAsync(new List<FileMetadataDto>());

            // Act
            var result = await _controller.Search(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockService.Verify(s => s.FindFilesAsync(request), Times.Once);
        }

        #endregion

        #region GetContent Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetContent_ReturnsBadRequest_WhenPathIsInvalid(string path)
        {
            // Act
            var result = await _controller.GetContent(path);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("path is required and must be an relative path.", badRequestResult.Value);
            _mockService.Verify(s => s.OpenFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetContent_ReturnsFile_WhenFileExists()
        {
            // Arrange
            var path = _faker.System.FilePath();
            var fileName = _faker.System.FileName();
            var contentType = "application/pdf";
            var fileContent = _faker.Random.Bytes(1024);
            var stream = new MemoryStream(fileContent);

            var fileResponse = new FileStreamResponse(stream, fileName, contentType);

            _mockService.Setup(s => s.OpenFileAsync(path))
                       .ReturnsAsync(fileResponse);

            // Act
            var result = await _controller.GetContent(path);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal(fileName, fileResult.FileDownloadName);
            Assert.Equal(contentType, fileResult.ContentType);
            Assert.True(fileResult.EnableRangeProcessing);
            Assert.Equal(stream, fileResult.FileStream);

            _mockService.Verify(s => s.OpenFileAsync(path), Times.Once);
        }

        [Fact]
        public async Task GetContent_ReturnsFileWithPdfContentType()
        {
            // Arrange
            var path = _faker.System.FilePath();
            var stream = new MemoryStream(_faker.Random.Bytes(256));
            var fileResponse = new FileStreamResponse(stream, "document.pdf", "application/pdf");

            _mockService.Setup(s => s.OpenFileAsync(path))
                       .ReturnsAsync(fileResponse);

            // Act
            var result = await _controller.GetContent(path);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
        }

        [Fact]
        public async Task GetContent_ThrowsFileNotFoundException_WhenFileDoesNotExist()
        {
            // Arrange
            var path = _faker.System.FilePath();
            _mockService.Setup(s => s.OpenFileAsync(path))
                       .ThrowsAsync(new FileNotFoundException("File not found"));

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                async () => await _controller.GetContent(path));

            _mockService.Verify(s => s.OpenFileAsync(path), Times.Once);
        }

        [Fact]
        public async Task GetContent_EnablesRangeProcessing()
        {
            // Arrange
            var path = _faker.System.FilePath();
            var stream = new MemoryStream(_faker.Random.Bytes(1024));
            var fileResponse = new FileStreamResponse(stream, _faker.System.FileName(), "application/pdf");

            _mockService.Setup(s => s.OpenFileAsync(path))
                       .ReturnsAsync(fileResponse);

            // Act
            var result = await _controller.GetContent(path);

            // Assert
            var fileResult = Assert.IsType<FileStreamResult>(result);
            Assert.True(fileResult.EnableRangeProcessing, "Range processing should be enabled for large file downloads");
        }

        #endregion

        #region Helper Methods

        private TransitoryDocumentSearchRequest CreateValidSearchRequest()
        {
            return new TransitoryDocumentSearchRequest
            {
                RegionCode = _faker.Address.StateAbbr(),
                RegionName = _faker.Address.State(),
                AgencyIdentifierCd = _faker.Random.AlphaNumeric(10),
                LocationShortName = _faker.Address.City(),
                RoomCd = _faker.Random.AlphaNumeric(5),
                Date = DateOnly.FromDateTime(_faker.Date.Recent())
            };
        }

        private FileMetadataDto CreateFileMetadata()
        {
            var fileName = _faker.System.FileName();
            return new FileMetadataDto()
            {
                FileName = fileName,
                Extension = Path.GetExtension(fileName),
                SizeBytes = _faker.Random.Long(1000, 1000000),
                CreatedUtc = _faker.Date.Recent().ToUniversalTime(),
                RelativePath = _faker.System.FilePath(),
                MatchedRoomFolder = _faker.Random.Bool() ? _faker.Random.AlphaNumeric(5) : null
            };
        }

        #endregion
    }
}
