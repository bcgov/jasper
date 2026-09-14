using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Services;
using Scv.Core.Infrastructure;
using Scv.Models.CourtLocation;
using Xunit;

namespace tests.api.Controllers;

public class CourtLocationsControllerTests
{
    private readonly CourtLocationsController _controller;
    private readonly Mock<ICourtLocationService> _mockService;

    public CourtLocationsControllerTests()
    {
        _mockService = new Mock<ICourtLocationService>();
        _controller = new CourtLocationsController(_mockService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCourtLocationByCode_ReturnsBadRequest_WhenCodeIsMissing(string code)
    {
        var result = await _controller.GetCourtLocationByCode(code);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Please provide a location code.", badRequest.Value);
        _mockService.Verify(
            s => s.GetCourtLocationByCodeAsync(It.IsAny<string>()),
            Times.Never());
    }

    [Fact]
    public async Task GetCourtLocationByCode_ReturnsBadRequest_WhenServiceFails()
    {
        _mockService
            .Setup(s => s.GetCourtLocationByCodeAsync("4801"))
            .ReturnsAsync(OperationResult<CourtLocationDto>.Failure("boom"));

        var result = await _controller.GetCourtLocationByCode("4801");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to retrieve court location: boom", badRequest.Value);
        _mockService.Verify(s => s.GetCourtLocationByCodeAsync("4801"), Times.Once());
    }

    [Fact]
    public async Task GetCourtLocationByCode_ReturnsNotFound_WhenPayloadIsNull()
    {
        _mockService
            .Setup(s => s.GetCourtLocationByCodeAsync("4801"))
            .ReturnsAsync(OperationResult<CourtLocationDto>.Success(null));

        var result = await _controller.GetCourtLocationByCode("4801");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Court location with code '4801' not found.", notFound.Value);
        _mockService.Verify(s => s.GetCourtLocationByCodeAsync("4801"), Times.Once());
    }

    [Fact]
    public async Task GetCourtLocationByCode_ReturnsOk_WithPayload()
    {
        var dto = new CourtLocationDto { Code = "4801", Name = "Vancouver" };
        _mockService
            .Setup(s => s.GetCourtLocationByCodeAsync("4801"))
            .ReturnsAsync(OperationResult<CourtLocationDto>.Success(dto));

        var result = await _controller.GetCourtLocationByCode("4801");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(dto, okResult.Value);
        _mockService.Verify(s => s.GetCourtLocationByCodeAsync("4801"), Times.Once());
    }
}
