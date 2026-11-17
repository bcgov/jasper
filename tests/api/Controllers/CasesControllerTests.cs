using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Core.Infrastructure;
using Scv.Core.Infrastructure.Authorization;
using Scv.Models;
using Scv.Db.Models;
using Scv.Models.AccessControlManagement;
using Xunit;

namespace tests.api.Controllers;

public class CasesControllerTests
{
    private readonly CasesController _controller;
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly Mock<IUserService> _mockUserService = new();

    private readonly int judgeId = 1;

    public CasesControllerTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _controller = new CasesController(_mockCaseService.Object, _mockUserService.Object);

        var claims = new List<Claim>
        {
            new(CustomClaimTypes.JudgeId, judgeId.ToString()),
            new(CustomClaimTypes.IsActive, "true")
        };
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

        Assert.IsType<OkObjectResult>(result);
        _mockCaseService.Verify(s => s.GetAssignedCasesAsync(judgeId), Times.Once());
    }

    [Theory]
    [InlineData(Role.JUDGE)]
    [InlineData(Role.ACJ_CHIEF_JUDGE)]
    [InlineData(Role.RAJ)]
    [InlineData(Role.DEVELOPER)]
    [InlineData(Role.ADMIN)] // A role-managed grant works without a judiciary role name.
    [InlineData("")]
    public async Task Override_UsesRealSignedInJudgeAndForwardsClear(string role)
    {
        var identity = (ClaimsIdentity)_controller.User.Identity;
        identity.AddClaim(new Claim(CustomClaimTypes.Role, role));
        identity.AddClaim(new Claim(CustomClaimTypes.Permission, Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE));
        identity.AddClaim(new Claim(CustomClaimTypes.ExternalJudgeId, judgeId.ToString()));
        identity.AddClaim(new Claim(CustomClaimTypes.Groups, "jasper-view-others-schedule"));
        var request = new CaseDueDateOverrideRequest { UpdatedDate = System.DateTime.UtcNow };
        _mockCaseService.Setup(s => s.SetDueDateOverrideAsync("case", judgeId, request))
            .ReturnsAsync((200, OperationResult<List<CaseDto>>.Success([])));
        var response = Assert.IsType<ObjectResult>(await _controller.SetDueDateOverride("case", request));
        Assert.Equal(200, response.StatusCode);
        _mockCaseService.Verify(s => s.SetDueDateOverrideAsync("case", judgeId, request), Times.Once);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Override_RejectsMissingPermissionAndConfiguredViewingFallback(bool permission, bool externalAssociation)
    {
        var identity = (ClaimsIdentity)_controller.User.Identity;
        identity.AddClaim(new Claim(CustomClaimTypes.Role, Role.JUDGE));
        if (permission) identity.AddClaim(new Claim(CustomClaimTypes.Permission, Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE));
        if (externalAssociation) identity.AddClaim(new Claim(CustomClaimTypes.ExternalJudgeId, judgeId.ToString()));
        Assert.IsType<ForbidResult>(await _controller.SetDueDateOverride("case", new CaseDueDateOverrideRequest()));
        _mockCaseService.Verify(s => s.SetDueDateOverrideAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<CaseDueDateOverrideRequest>()), Times.Never);
    }

    [Fact]
    public async Task Override_UsesMappedJudgeAssociation()
    {
        var identity = (ClaimsIdentity)_controller.User.Identity;
        identity.AddClaim(new Claim(CustomClaimTypes.Role, Role.JUDGE));
        identity.AddClaim(new Claim(CustomClaimTypes.Permission, Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE));
        identity.AddClaim(new Claim(CustomClaimTypes.UserId, "user"));
        _mockUserService.Setup(s => s.GetByIdWithPermissionsAsync("user"))
            .ReturnsAsync(new UserDto { JudgeId = judgeId });
        _mockCaseService.Setup(s => s.SetDueDateOverrideAsync("case", judgeId, It.IsAny<CaseDueDateOverrideRequest>()))
            .ReturnsAsync((403, OperationResult<List<CaseDto>>.Failure("Another judge's record")));
        var response = Assert.IsType<ObjectResult>(await _controller.SetDueDateOverride("case", new CaseDueDateOverrideRequest()));
        Assert.Equal(403, response.StatusCode);
    }

    [Fact]
    public void Override_UsesTheExistingPermissionAuthorizationFilter()
    {
        var method = typeof(CasesController).GetMethod(nameof(CasesController.SetDueDateOverride));
        var attribute = Assert.IsType<RequiresPermissionAttribute>(Assert.Single(
            method.GetCustomAttributes(typeof(RequiresPermissionAttribute), true)));
        var requirement = Assert.IsType<PermissionRequirement>(Assert.Single(attribute.Arguments));
        Assert.Equal(Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE, Assert.Single(requirement.RequiredPermissions));
    }

    [Fact]
    public async Task Override_RejectsInactiveUsers()
    {
        var identity = (ClaimsIdentity)_controller.User.Identity;
        identity.RemoveClaim(identity.FindFirst(CustomClaimTypes.IsActive));
        identity.AddClaim(new Claim(CustomClaimTypes.Permission, Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE));
        identity.AddClaim(new Claim(CustomClaimTypes.ExternalJudgeId, "1"));
        Assert.IsType<ForbidResult>(await _controller.SetDueDateOverride("case", new CaseDueDateOverrideRequest()));
        _mockCaseService.Verify(s => s.SetDueDateOverrideAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<CaseDueDateOverrideRequest>()), Times.Never);
    }

    [Fact]
    public async Task Override_RejectsIdentityMismatchBeforeCallingService()
    {
        var identity = (ClaimsIdentity)_controller.User.Identity;
        identity.AddClaim(new Claim(CustomClaimTypes.Permission, Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE));
        identity.AddClaim(new Claim(CustomClaimTypes.ExternalJudgeId, "2"));
        identity.AddClaim(new Claim(CustomClaimTypes.Groups, "jasper-view-others-schedule"));
        Assert.IsType<ForbidResult>(await _controller.SetDueDateOverride("case", new CaseDueDateOverrideRequest()));
        _mockCaseService.Verify(s => s.SetDueDateOverrideAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<CaseDueDateOverrideRequest>()), Times.Never);
    }

    [Fact]
    public async Task Override_RejectsUnauthenticatedClaims()
    {
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(CustomClaimTypes.Permission, Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE),
            new Claim(CustomClaimTypes.JudgeId, "1"),
            new Claim(CustomClaimTypes.ExternalJudgeId, "1")
        ]));
        Assert.IsType<ForbidResult>(await _controller.SetDueDateOverride("case", new CaseDueDateOverrideRequest()));
        _mockCaseService.Verify(s => s.SetDueDateOverrideAsync(It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<CaseDueDateOverrideRequest>()), Times.Never);
    }

    [Theory]
    [InlineData(false, true, null)]
    [InlineData(true, false, null)]
    [InlineData(true, true, 1)]
    public async Task GetCases_ExposesOnlyVerifiedEditingIdentityWhileViewingAnotherJudge(
        bool permission, bool association, int? expected)
    {
        var identity = (ClaimsIdentity)_controller.User.Identity;
        identity.AddClaim(new Claim(CustomClaimTypes.Groups, "jasper-view-others-schedule"));
        if (permission) identity.AddClaim(new Claim(CustomClaimTypes.Permission, Permission.EDIT_RESERVED_JUDGMENTS_DUE_DATE));
        if (association) identity.AddClaim(new Claim(CustomClaimTypes.ExternalJudgeId, "1"));
        _mockCaseService.Setup(s => s.GetAssignedCasesAsync(2))
            .ReturnsAsync(OperationResult<CaseResponse>.Success(new CaseResponse()));
        var response = Assert.IsType<OkObjectResult>(await _controller.GetAssignedCases(2));
        var payload = Assert.IsType<OperationResult<CaseResponse>>(response.Value).Payload;
        Assert.Equal(expected, payload.DueDateEditingJudgeId);
    }
}
