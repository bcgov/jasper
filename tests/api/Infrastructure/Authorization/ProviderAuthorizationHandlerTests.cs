using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Scv.Api.Controllers;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Authentication;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Models.AccessControlManagement;
using Xunit;

namespace tests.api.Infrastructure.Authorization;

public class ProviderAuthorizationHandlerTests
{
    private const string UserId = "user-id";
    private const string EncodedGuid = "AQAAAAAAAAAAAAAAAAAAAA==";

    [Theory]
    [InlineData(typeof(UsersController), nameof(UsersController.RequestAccess), true)]
    [InlineData(typeof(UsersController), nameof(UsersController.GetMyUser), false)]
    [InlineData(typeof(AuthController), nameof(AuthController.UserInfo), false)]
    public async Task HandleAsync_OnlyLetsRequestAccessContinueWithStaleClaims(Type controller, string actionName, bool expectedSuccess)
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetByIdWithPermissionsAsync(UserId))
            .ReturnsAsync(new UserDto { Id = UserId, IsActive = true, Roles = ["Judge"] });

        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(CustomClaimTypes.UserId, UserId),
            new Claim(CustomClaimTypes.ProvjudUserGuid, EncodedGuid)
        ], "TestAuthType"));
        var httpContext = CreateEndpointContext(controller, actionName, principal);
        var context = new AuthorizationHandlerContext([new ProviderAuthorizationHandler.ProviderRequirement()], principal, httpContext);

        await new ProviderAuthorizationHandler(userService.Object).HandleAsync(context);

        Assert.Equal(expectedSuccess, context.HasSucceeded);
        if (!expectedSuccess)
        {
            Assert.Equal("User claims changed since last authentication.", Assert.Single(context.FailureReasons).Message);
        }
    }

    [Theory]
    [InlineData(typeof(AuthController), nameof(AuthController.UserInfo), true, false, false, true)]
    [InlineData(typeof(UsersController), nameof(UsersController.GetMyUser), true, false, false, true)]
    [InlineData(typeof(ApplicationController), nameof(ApplicationController.GetApplicationInfo), true, false, false, false)]
    [InlineData(typeof(ApplicationController), nameof(ApplicationController.GetApplicationInfo), true, true, false, false)]
    [InlineData(typeof(ApplicationController), nameof(ApplicationController.GetApplicationInfo), true, false, true, true)]
    [InlineData(typeof(AuthController), nameof(AuthController.UserInfo), false, false, false, false)]
    [InlineData(typeof(UsersController), nameof(UsersController.GetMyUser), false, false, false, false)]
    public async Task ProviderPolicy_OnlyAllowsSelfInformationWithoutPcssAccess(Type controller, string actionName,
        bool authenticated, bool hasIdentityProviderGroup, bool hasJasperAccess, bool expectedSuccess)
    {
        var userService = new Mock<IUserService>();
        userService.Setup(s => s.GetByIdWithPermissionsAsync(UserId)).ReturnsAsync(new UserDto
        {
            Id = UserId,
            IsActive = hasJasperAccess,
            GroupIds = hasJasperAccess ? ["judiciary"] : [],
            Roles = hasJasperAccess ? ["Judge"] : []
        });
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.UserId, UserId),
            new(CustomClaimTypes.ProvjudUserGuid, EncodedGuid),
            new(CustomClaimTypes.IsActive, hasJasperAccess.ToString())
        };
        if (hasJasperAccess)
        {
            claims.Add(new Claim(CustomClaimTypes.Role, "Judge"));
        }
        if (hasIdentityProviderGroup)
        {
            claims.Add(new Claim(CustomClaimTypes.Groups, "court-viewer-provincial"));
        }
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? TokenValidationParameters.DefaultAuthenticationType : null));
        var httpContext = CreateEndpointContext(controller, actionName, principal);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(userService.Object);
        services.AddSingleton(Options.Create(new KeycloakOptions { Authority = "https://identity.example.com" }));
        services.AddScvAuthorization();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var result = await authorization.AuthorizeAsync(principal, httpContext, nameof(ProviderAuthorizationHandler));

        Assert.Equal(expectedSuccess, result.Succeeded);
    }

    [Fact]
    public async Task HandleAsync_RetainsSiteMinderAuthorization()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([], SiteMinderAuthenticationHandler.SiteMinder));
        var httpContext = CreateEndpointContext(typeof(ApplicationController), nameof(ApplicationController.GetApplicationInfo), principal);
        var authContext = new AuthorizationHandlerContext([new ProviderAuthorizationHandler.ProviderRequirement()], principal, httpContext);

        await new ProviderAuthorizationHandler(Mock.Of<IUserService>()).HandleAsync(authContext);

        Assert.True(authContext.HasSucceeded);
    }

    private static DefaultHttpContext CreateEndpointContext(Type controller, string actionName, ClaimsPrincipal principal)
    {
        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new ControllerActionDescriptor
        {
            ControllerTypeInfo = controller.GetTypeInfo(),
            ActionName = actionName
        }), actionName));
        return httpContext;
    }
}
