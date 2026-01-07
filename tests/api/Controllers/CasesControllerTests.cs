using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Core.Infrastructure;
using Scv.Models;
using Xunit;

namespace tests.api.Controllers;

public class CasesControllerTests
{
    private readonly CasesController _controller;
    private readonly Mock<ICaseService> _mockCaseService;

    private int judgeId = 1;

    public CasesControllerTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _controller = new CasesController(_mockCaseService.Object);

        var claims = new List<Claim> { new(CustomClaimTypes.JudgeId, judgeId.ToString()), };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetCases_ReturnsOk_WithResult()
    {
        _mockCaseService.Setup(s => s.GetAssignedCasesAsync(judgeId)).ReturnsAsync(OperationResult<CaseResponse>.Success(new CaseResponse
        {
            ReservedJudgments = [],
            ScheduledContinuations = []
        }));

        var result = await _controller.GetAssignedCases();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<OkObjectResult>(result);
        _mockCaseService.Verify(s => s.GetAssignedCasesAsync(judgeId), Times.Once());
    }
}
