using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Models;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Controllers;

public class ReservedJudgementsControllerTests
{
    private readonly ReservedJudgementsController _controller;
    private readonly Mock<ICrudService<ReservedJudgementDto>> _mockJudgementService;

    public ReservedJudgementsControllerTests()
    {
        _mockJudgementService = new Mock<ICrudService<ReservedJudgementDto>>();
        _controller = new ReservedJudgementsController(_mockJudgementService.Object);

        var context = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }

    [Fact]
    public async Task GetReservedJudgements_ReturnsOk_WithResult()
    {
        var results = new List<ReservedJudgementDto>();
        _mockJudgementService.Setup(s => s.GetAllAsync()).ReturnsAsync(results);

        var result = await _controller.GetReservedJudgements();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(results, okResult.Value);
        _mockJudgementService.Verify(s => s.GetAllAsync(), Times.Once());
    }
}
