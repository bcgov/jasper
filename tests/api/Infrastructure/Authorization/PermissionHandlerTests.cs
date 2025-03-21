using System.Security.Claims;
using System.Threading.Tasks;
using Bogus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using Scv.Db.Models;
using Xunit;

namespace tests.api.Infrastructure.Authorization;
public class PermissionHandlerTests
{
    private readonly Mock<ILogger<PermissionHandler>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IUserService> _mockUserService;
    private readonly PermissionHandler _handler;
    private readonly Faker _faker = new Faker();

    public PermissionHandlerTests()
    {
        _mockLogger = new Mock<ILogger<PermissionHandler>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockUserService = new Mock<IUserService>();
        _handler = new PermissionHandler(
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockUserService.Object);
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
    public async Task NullUser_ShouldBeUnauthorized()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _mockUserService
            .Setup(u => u.GetWithPermissionsAsync(It.IsAny<string>()))
            .ReturnsAsync((UserDto)null);

        var identity = new ClaimsIdentity([new(CustomClaimTypes.Email, _faker.Internet.Email())], "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            [],
            user,
            null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task UserWithoutPermissions_ShouldBeUnauthorized()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _mockUserService
            .Setup(u => u.GetWithPermissionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserDto
            {
                Permissions = [Permission.VIEW_CHILDREN]
            });

        var requirement = new PermissionRequirement(permissions: [Permission.LOCK_UNLOCK_USERS]);

        var identity = new ClaimsIdentity([new(CustomClaimTypes.Email, _faker.Internet.Email())], "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            null);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task UserWithPermissions_ShouldBeAuthorized()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _mockUserService
            .Setup(u => u.GetWithPermissionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserDto
            {
                Permissions = [Permission.LOCK_UNLOCK_USERS]
            });

        var requirement = new PermissionRequirement(permissions: [Permission.LOCK_UNLOCK_USERS]);

        var identity = new ClaimsIdentity([new(CustomClaimTypes.Email, _faker.Internet.Email())], "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            null);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task UserWithPermissions_WhenOrConditioniShouldBeAuthorized()
    {
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        _mockUserService
            .Setup(u => u.GetWithPermissionsAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserDto
            {
                Permissions = [
                    Permission.LOCK_UNLOCK_USERS,
                    Permission.VIEW_OWN_SCHEDULE,
                    Permission.VIEW_MULTIPLE_DOCUMENTS
                ]
            });

        var requirement = new PermissionRequirement(true, [Permission.VIEW_OWN_SCHEDULE, Permission.VIEW_QUICK_LINKS]);

        var identity = new ClaimsIdentity([new(CustomClaimTypes.Email, _faker.Internet.Email())], "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(
            [requirement],
            user,
            null);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }
}
