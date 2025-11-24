using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using DARSCommon.Clients.LogNotesServices;
using DARSCommon.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Models.Dars;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Controllers
{
    public class DarsControllerTests
    {
        private readonly Faker _faker;
        private readonly Mock<IDarsService> _mockDarsService;
        private readonly Mock<ILogger<DarsController>> _mockLogger;
        private readonly DarsController _controller;

        public DarsControllerTests()
        {
            _faker = new Faker();
            _mockDarsService = new Mock<IDarsService>();
            _mockLogger = new Mock<ILogger<DarsController>>();
            _controller = new DarsController(_mockDarsService.Object, _mockLogger.Object);

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
            var agencyIdentifierCd = _faker.Random.Int(1, 1000).ToString();;
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