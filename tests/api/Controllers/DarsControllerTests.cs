using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DarsSearchResults = DARSCommon.Models.DarsSearchResults;
using ApiException = DARSCommon.Clients.LogNotesServices.ApiException;
using DarsClientSearchResult = Scv.Api.Models.Dars.DarsClientSearchResult;
using TranscriptDocument = Scv.Models.Dars.TranscriptDocument;
using TranscriptAppearance = Scv.Models.Dars.TranscriptAppearance;
using TranscriptSearchRequest = Scv.Models.Dars.TranscriptSearchRequest;
using Scv.Models.Document;

namespace tests.api.Controllers
{
    public class DarsControllerTests
    {
        private readonly Faker _faker;
        private readonly Mock<Scv.Api.Services.IDarsService> _mockDarsService;
        private readonly Mock<ILogger<Scv.Api.Controllers.DarsController>> _mockLogger;
        private readonly Mock<IValidator<TranscriptSearchRequest>> _mockValidator;
        private readonly Mock<Scv.Api.Documents.IDocumentMerger> _mockDocumentMerger;
        private readonly Scv.Api.Controllers.DarsController _controller;

        public DarsControllerTests()
        {
            _faker = new Faker();
            _mockDarsService = new Mock<Scv.Api.Services.IDarsService>();
            _mockLogger = new Mock<ILogger<Scv.Api.Controllers.DarsController>>();
            _mockValidator = new Mock<IValidator<TranscriptSearchRequest>>();
            _mockDocumentMerger = new Mock<Scv.Api.Documents.IDocumentMerger>();

            _controller = new Scv.Api.Controllers.DarsController(
                _mockDarsService.Object,
                _mockLogger.Object,
                _mockValidator.Object,
                _mockDocumentMerger.Object);

            var context = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = context
            };
        }

        #region Search Tests

        [Fact]
        public async Task Search_ReturnsBadRequest_WhenAgencyIdentifierCdIsEmpty()
        {
            // Arrange
            var date = _faker.Date.Recent();
            var agencyIdentifierCd = "";
            var courtRoomCd = _faker.Random.AlphaNumeric(5);

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("agencyIdentifierCd must be non-empty.", badRequestResult.Value);
            _mockDarsService.Verify(
                s => s.DarsApiSearch(It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Search_ReturnsOk_WithResults_WhenRecordingsFound()
        {
            // Arrange
            var date = _faker.Date.Recent();
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString(); ;
            var courtRoomCd = _faker.Random.AlphaNumeric(5);
            var expectedResults = new List<DarsSearchResults>
            {
                CreateDarsSearchResult(date, Int32.Parse(agencyIdentifierCd), courtRoomCd),
                CreateDarsSearchResult(date, Int32.Parse(agencyIdentifierCd), courtRoomCd)
            };

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ReturnsAsync(new DarsClientSearchResult { Results = expectedResults, Cookies = [] });

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualResults = Assert.IsAssignableFrom<IEnumerable<DarsSearchResults>>(okResult.Value);
            Assert.Equal(expectedResults.Count, actualResults.Count());
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        [Fact]
        public async Task Search_ReturnsNotFound_WhenNoRecordingsFound()
        {
            // Arrange
            var date = _faker.Date.Recent();
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString();
            var courtRoomCd = _faker.Random.AlphaNumeric(5);

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ReturnsAsync(new DarsClientSearchResult { Results = [], Cookies = [] });

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        [Fact]
        public async Task Search_ReturnsNotFound_WhenServiceReturnsNull()
        {
            // Arrange
            var date = _faker.Date.Recent();
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString();
            var courtRoomCd = _faker.Random.AlphaNumeric(5);

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ReturnsAsync(new DarsClientSearchResult { Results = null, Cookies = [] });

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        [Fact]
        public async Task Search_ReturnsNotFound_WhenApiExceptionWithStatus404()
        {
            // Arrange
            var date = _faker.Date.Recent();
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString();
            var courtRoomCd = _faker.Random.AlphaNumeric(5);
            var apiException = new ApiException("Not found", 404, "response", null, null);

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ThrowsAsync(apiException);

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        [Fact]
        public async Task Search_ReturnsInternalServerError_WhenApiExceptionWithNon404Status()
        {
            // Arrange
            var date = _faker.Date.Recent();
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString();
            var courtRoomCd = _faker.Random.AlphaNumeric(5);
            var apiException = new ApiException("Internal server error", 500, "response", null, null);

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ThrowsAsync(apiException);

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while searching for audio recordings.", statusCodeResult.Value);
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(403)]
        [InlineData(500)]
        [InlineData(503)]
        public async Task Search_ReturnsInternalServerError_ForApiExceptionStatusCodes(int statusCode)
        {
            // Arrange
            var date = _faker.Date.Recent();
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString();
            var courtRoomCd = _faker.Random.AlphaNumeric(5);
            var apiException = new ApiException($"Error {statusCode}", statusCode, "response", null, null);

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ThrowsAsync(apiException);

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        [Fact]
        public async Task Search_PassesCorrectParameters_ToService()
        {
            // Arrange
            var date = new DateTime(2025, 11, 3, 10, 30, 0, DateTimeKind.Unspecified);
            var agencyIdentifierCd = "42";
            var courtRoomCd = "CTR001";
            var expectedResults = new List<DarsSearchResults> { CreateDarsSearchResult(date, Int32.Parse(agencyIdentifierCd), courtRoomCd) };

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ReturnsAsync(new DarsClientSearchResult { Results = expectedResults, Cookies = [] });

            // Act
            await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        [Fact]
        public async Task Search_HandlesEmptyCourtRoomCd()
        {
            // Arrange
            var date = _faker.Date.Recent();
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString();
            var courtRoomCd = string.Empty;
            var expectedResults = new List<DarsSearchResults> { CreateDarsSearchResult(date, Int32.Parse(agencyIdentifierCd), courtRoomCd) };

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ReturnsAsync(new DarsClientSearchResult { Results = expectedResults, Cookies = [] });

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        [Fact]
        public async Task Search_HandlesFutureDate()
        {
            // Arrange
            var date = DateTime.Now.AddDays(7);
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString();
            var courtRoomCd = _faker.Random.AlphaNumeric(5);

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ReturnsAsync(new DarsClientSearchResult { Results = [], Cookies = [] });

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        [Fact]
        public async Task Search_HandlesPastDate()
        {
            // Arrange
            var date = DateTime.Now.AddYears(-5);
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString();
            var courtRoomCd = _faker.Random.AlphaNumeric(5);
            var expectedResults = new List<DarsSearchResults> { CreateDarsSearchResult(date, Int32.Parse(agencyIdentifierCd), courtRoomCd) };

            _mockDarsService
                .Setup(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd))
                .ReturnsAsync(new DarsClientSearchResult { Results = expectedResults, Cookies = [] });

            // Act
            var result = await _controller.Search(date, agencyIdentifierCd, courtRoomCd);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockDarsService.Verify(s => s.DarsApiSearch(date, agencyIdentifierCd, courtRoomCd), Times.Once);
        }

        #endregion

        #region GetTranscripts Tests

        [Fact]
        public async Task GetTranscripts_ReturnsBadRequest_WhenValidationFails()
        {
            // Arrange
            var physicalFileId = _faker.Random.AlphaNumeric(10);
            var validationResult = new FluentValidation.Results.ValidationResult(new List<ValidationFailure>
            {
                new ValidationFailure("PhysicalFileId", "Invalid format")
            });

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId,
                ReturnChildRecords = true
            });

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var errors = Assert.IsAssignableFrom<IEnumerable<string>>(badRequestResult.Value);
            Assert.Contains("Invalid format", errors);

            _mockValidator.Verify(v => v.ValidateAsync(
                It.Is<TranscriptSearchRequest>(r =>
                    r.PhysicalFileId == physicalFileId &&
                    r.MdocJustinNo == null &&
                    r.ReturnChildRecords == true),
                default), Times.Once);
        }

        [Fact]
        public async Task GetTranscripts_ReturnsOk_WhenTranscriptsFound()
        {
            // Arrange
            var physicalFileId = _faker.Random.AlphaNumeric(10);
            var transcripts = new List<TranscriptDocument>
            {
                new TranscriptDocument
                {
                    Id = _faker.Random.Int(1, 1000),
                    OrderId = _faker.Random.Int(1, 1000),
                    Description = _faker.Lorem.Sentence(),
                    FileName = _faker.System.FileName("pdf"),
                    PagesComplete = _faker.Random.Int(1, 1000),
                    StatusCodeId = 1,
                    Appearances = new List<TranscriptAppearance>()
                },
                new TranscriptDocument
                {
                    Id = _faker.Random.Int(1, 1000),
                    OrderId = _faker.Random.Int(1, 1000),
                    Description = _faker.Lorem.Sentence(),
                    FileName = _faker.System.FileName("pdf"),
                    PagesComplete = _faker.Random.Int(1, 100),
                    StatusCodeId = 1,
                    Appearances = new List<TranscriptAppearance>()
                }
            };

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(physicalFileId, null, true))
                .Returns(Task.FromResult<IEnumerable<TranscriptDocument>>(transcripts));

            // Act
            var result = await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId,
                ReturnChildRecords = true
            });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedTranscripts = Assert.IsAssignableFrom<IEnumerable<TranscriptDocument>>(okResult.Value);
            Assert.Equal(2, returnedTranscripts.Count());
        }

        [Fact]
        public async Task GetTranscripts_ReturnsNotFound_WhenNoTranscriptsFound()
        {
            // Arrange
            var physicalFileId = _faker.Random.AlphaNumeric(10);

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(physicalFileId, null, true))
                .Returns(Task.FromResult<IEnumerable<TranscriptDocument>>(new List<TranscriptDocument>()));

            // Act
            var result = await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId,
                ReturnChildRecords = true
            });

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetTranscripts_ReturnsNotFound_WhenResultIsNull()
        {
            // Arrange
            var physicalFileId = _faker.Random.AlphaNumeric(10);

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(physicalFileId, null, true))
                .Returns(Task.FromResult<IEnumerable<TranscriptDocument>>(null));

            // Act
            var result = await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId,
                ReturnChildRecords = true
            });

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetTranscripts_ReturnsNotFound_WhenApiException404()
        {
            // Arrange
            var physicalFileId = _faker.Random.AlphaNumeric(10);

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(physicalFileId, null, true))
                .ThrowsAsync(new DARSCommon.Clients.TranscriptsServices.ApiException("Not found", 404, "response", null, null));

            // Act
            var result = await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId,
                ReturnChildRecords = true
            });

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetTranscripts_ReturnsInternalServerError_WhenApiException()
        {
            // Arrange
            var physicalFileId = _faker.Random.AlphaNumeric(10);

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(physicalFileId, null, true))
                .ThrowsAsync(new DARSCommon.Clients.TranscriptsServices.ApiException("Server error", 500, "response", null, null));

            // Act
            var result = await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId,
                ReturnChildRecords = true
            });

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while searching for transcripts.", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetTranscripts_PassesCorrectParameters_WithPhysicalFileId()
        {
            // Arrange
            var physicalFileId = "12345";

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(physicalFileId, null, true))
                .Returns(Task.FromResult<IEnumerable<TranscriptDocument>>(new List<TranscriptDocument>
                {
                    new TranscriptDocument { Id = 1, OrderId = 1, Description = "", FileName = "", PagesComplete = 0, StatusCodeId = 1, Appearances = new List<TranscriptAppearance>() }
                }));

            // Act
            await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId,
                ReturnChildRecords = true
            });

            // Assert
            _mockDarsService.Verify(s => s.GetCompletedDocuments(physicalFileId, null, true), Times.Once);
            _mockValidator.Verify(v => v.ValidateAsync(
                It.Is<TranscriptSearchRequest>(r =>
                    r.PhysicalFileId == physicalFileId &&
                    r.MdocJustinNo == null &&
                    r.ReturnChildRecords == true),
                default), Times.Once);
        }

        [Fact]
        public async Task GetTranscripts_PassesCorrectParameters_WithMdocJustinNo()
        {
            // Arrange
            var mdocJustinNo = "54321";

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(null, mdocJustinNo, true))
                .Returns(Task.FromResult<IEnumerable<TranscriptDocument>>(new List<TranscriptDocument>
                {
                    new TranscriptDocument { Id = 1, OrderId = 1, Description = "", FileName = "", PagesComplete = 0, StatusCodeId = 1, Appearances = new List<TranscriptAppearance>() }
                }));

            // Act
            await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                MdocJustinNo = mdocJustinNo,
                ReturnChildRecords = true
            });

            // Assert
            _mockDarsService.Verify(s => s.GetCompletedDocuments(null, mdocJustinNo, true), Times.Once);
            _mockValidator.Verify(v => v.ValidateAsync(
                It.Is<TranscriptSearchRequest>(r =>
                    r.PhysicalFileId == null &&
                    r.MdocJustinNo == mdocJustinNo &&
                    r.ReturnChildRecords == true),
                default), Times.Once);
        }

        [Fact]
        public async Task GetTranscripts_PassesCorrectParameters_WithBothParameters()
        {
            // Arrange
            var physicalFileId = "12345";
            var mdocJustinNo = "54321";
            var returnChildRecords = false;

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(physicalFileId, mdocJustinNo, returnChildRecords))
                .Returns(Task.FromResult<IEnumerable<TranscriptDocument>>(new List<TranscriptDocument>
                {
                    new TranscriptDocument { Id = 1, OrderId = 1, Description = "", FileName = "", PagesComplete = 0, StatusCodeId = 1, Appearances = new List<TranscriptAppearance>() }
                }));

            // Act
            await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId,
                MdocJustinNo = mdocJustinNo,
                ReturnChildRecords = returnChildRecords
            });

            // Assert
            _mockDarsService.Verify(s => s.GetCompletedDocuments(physicalFileId, mdocJustinNo, returnChildRecords), Times.Once);
            _mockValidator.Verify(v => v.ValidateAsync(
                It.Is<TranscriptSearchRequest>(r =>
                    r.PhysicalFileId == physicalFileId &&
                    r.MdocJustinNo == mdocJustinNo &&
                    r.ReturnChildRecords == returnChildRecords),
                default), Times.Once);
        }

        [Fact]
        public async Task GetTranscripts_TrimsWhitespace_FromParameters()
        {
            // Arrange
            var physicalFileId = "  12345  ";
            var mdocJustinNo = "  54321  ";
            var expectedPhysicalFileId = "12345";
            var expectedMdocJustinNo = "54321";

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(expectedPhysicalFileId, expectedMdocJustinNo, true))
                .Returns(Task.FromResult<IEnumerable<TranscriptDocument>>(new List<TranscriptDocument>
                {
                    new TranscriptDocument { Id = 1, OrderId = 1, Description = "", FileName = "", PagesComplete = 0, StatusCodeId = 1, Appearances = new List<TranscriptAppearance>() }
                }));

            // Act
            await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId,
                MdocJustinNo = mdocJustinNo,
                ReturnChildRecords = true
            });

            // Assert - Service should receive trimmed values
            _mockDarsService.Verify(s => s.GetCompletedDocuments(expectedPhysicalFileId, expectedMdocJustinNo, true), Times.Once);

            // Assert - Validator should receive the original request with untrimmed values
            _mockValidator.Verify(v => v.ValidateAsync(
                It.Is<TranscriptSearchRequest>(r =>
                    r.PhysicalFileId == physicalFileId &&
                    r.MdocJustinNo == mdocJustinNo &&
                    r.ReturnChildRecords == true),
                default), Times.Once);
        }

        [Fact]
        public async Task GetTranscripts_WithDefaultReturnChildRecords_UsesTrue()
        {
            // Arrange
            var physicalFileId = "12345";

            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<TranscriptSearchRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _mockDarsService
                .Setup(s => s.GetCompletedDocuments(physicalFileId, null, true))
                .Returns(Task.FromResult<IEnumerable<TranscriptDocument>>(new List<TranscriptDocument>
                {
                    new TranscriptDocument { Id = 1, OrderId = 1, Description = "", FileName = "", PagesComplete = 0, StatusCodeId = 1, Appearances = new List<TranscriptAppearance>() }
                }));

            // Act - Using default value from TranscriptSearchRequest
            await _controller.GetTranscripts(new TranscriptSearchRequest
            {
                PhysicalFileId = physicalFileId
                // ReturnChildRecords will use default value of true from the model
            });

            // Assert
            _mockDarsService.Verify(s => s.GetCompletedDocuments(physicalFileId, null, true), Times.Once);
        }

        #endregion

        #region GetTranscriptDocument Tests

        [Fact]
        public async Task GetTranscriptDocument_ReturnsBadRequest_WhenOrderIdIsEmpty()
        {
            // Arrange
            var orderId = "";
            var documentId = _faker.Random.Int(1, 1000).ToString();

            // Act
            var result = await _controller.GetTranscriptDocument(orderId, documentId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Order ID and Document ID are required.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetTranscriptDocument_ReturnsBadRequest_WhenDocumentIdIsEmpty()
        {
            // Arrange
            var orderId = _faker.Random.Int(1, 1000).ToString();
            var documentId = "";

            // Act
            var result = await _controller.GetTranscriptDocument(orderId, documentId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Order ID and Document ID are required.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetTranscriptDocument_ReturnsOk_WithBase64Pdf()
        {
            // Arrange
            var orderId = _faker.Random.Int(1, 1000).ToString();
            var documentId = _faker.Random.Int(1, 1000).ToString();
            var expectedBase64 = Convert.ToBase64String(_faker.Random.Bytes(100));

            _mockDocumentMerger
                .Setup(m => m.MergeDocuments(It.IsAny<Scv.Models.Document.PdfDocumentRequest[]>()))
                .ReturnsAsync(new Scv.Models.Document.PdfDocumentResponse { Base64Pdf = expectedBase64 });

            // Act
            var result = await _controller.GetTranscriptDocument(orderId, documentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var base64Property = response.GetType().GetProperty("base64Pdf");
            Assert.NotNull(base64Property);
            Assert.Equal(expectedBase64, base64Property.GetValue(response));
        }

        [Fact]
        public async Task GetTranscriptDocument_CallsMergeDocuments_WithCorrectParameters()
        {
            // Arrange
            var orderId = "123";
            var documentId = "456";

            _mockDocumentMerger
                .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
                .ReturnsAsync(new PdfDocumentResponse { Base64Pdf = "test" });

            // Act
            await _controller.GetTranscriptDocument(orderId, documentId);

            // Assert
            _mockDocumentMerger.Verify(m => m.MergeDocuments(
                It.Is<PdfDocumentRequest[]>(reqs =>
                    reqs.Length == 1 &&
                    reqs[0].Type == Scv.Models.DocumentType.Transcript &&
                    reqs[0].Data.OrderId == orderId &&
                    reqs[0].Data.DocumentId == documentId)),
                Times.Once);
        }

        [Fact]
        public async Task GetTranscriptDocument_ReturnsInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            var orderId = _faker.Random.Int(1, 1000).ToString();
            var documentId = _faker.Random.Int(1, 1000).ToString();

            _mockDocumentMerger
                .Setup(m => m.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetTranscriptDocument(orderId, documentId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving the transcript document.", statusCodeResult.Value);
        }

        #endregion

        #region Helper Methods

        private DarsSearchResults CreateDarsSearchResult(DateTime date, int agencyIdentifierCd, string courtRoomCd)
        {
            return new DarsSearchResults
            {
                Date = date.ToString("yyyy-MM-dd"),
                LocationId = agencyIdentifierCd,
                CourtRoomCd = courtRoomCd,
                Url = _faker.Internet.Url(),
                FileName = _faker.System.FileName("json"),
                LocationNm = _faker.Address.City()
            };
        }

        #endregion
    }
}