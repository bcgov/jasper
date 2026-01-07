using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Authorization;
using Scv.Models.AccessControlManagement;
using Scv.Api.Services;
using Scv.Db.Models;
using Xunit;
using Scv.Core.Infrastructure.Authorization;
using Scv.Core.Helpers;

namespace tests.api.Infrastructure.Authorization;
public class PermissionHandlerTests
{
    private readonly Mock<ILogger<PermissionHandler>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly PermissionHandler _handler;

    public PermissionHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PermissionHandler>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _handler = new PermissionHandler(_mockLogger.Object, _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task UnauthenticatedUser_ShouldBeUnauthorized()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        var identity = new ClaimsIdentity();  // No authentication type = unauthenticated
        var user = new ClaimsPrincipal(identity);
        var context = new AuthorizationHandlerContext(
            [],
            user,
            null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task UserWithNoPermissionClaims_ShouldBeUnauthorized()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

        var identity = new ClaimsIdentity([], "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            [],
            user,
            null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task UserWithExactPermissionClaims_ShouldBeAuthorized()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        var requirement = new PermissionRequirement(false, Permission.LOCK_UNLOCK_USERS, Permission.ACCESS_DARS);

        var claims = new List<Claim>{
            new(CustomClaimTypes.Permission, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.Permission, Permission.ACCESS_DARS),
            new(CustomClaimTypes.IsActive, "true")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            null);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task UserWithAnyPermission_ShouldBeAuthorized()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        var requirement = new PermissionRequirement(
            true,
            Permission.VIEW_CASE_DETAILS,
            Permission.VIEW_ADJUDICATOR_RESTRICTIONS);

        var claims = new List<Claim>{
            new(CustomClaimTypes.Permission, Permission.LOCK_UNLOCK_USERS),
            new(CustomClaimTypes.Permission, Permission.ACCESS_DARS),
            new(CustomClaimTypes.Permission, Permission.VIEW_CASE_DETAILS),
            new(CustomClaimTypes.IsActive, "true")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            null);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task UserDisabled_ShouldNotBeAuthorized()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());

        var claims = new List<Claim>{
            new(CustomClaimTypes.IsActive, "false")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            [],
            user,
            null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
