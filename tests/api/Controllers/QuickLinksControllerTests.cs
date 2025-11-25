using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Controllers;

public class QuickLinksControllerTests
{
    private readonly QuickLinksController _controller;
    private readonly Mock<IQuickLinkService> _mockService;
    
    public QuickLinksControllerTests()
    {
        _mockService = new Mock<IQuickLinkService>();
        _controller = new QuickLinksController(_mockService.Object);
        var context = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }

    [Fact]
    public async Task GetQuickLinks_ReturnsOk_WithResult()
    {
        _mockService.Setup(s => s.GetJudgeQuickLinks()).ReturnsAsync([]);
        var result = await _controller.GetQuickLinks();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetJudgeQuickLinks(), Times.Once());
    }
}