using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Scv.Api.Helpers;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Options;
using Xunit;

namespace tests.api.Infrastructure.Authorization;

public class CsoRoleAuthorizationHandlerTests
{
    private const string Audience = "cso-jasper";
    private const string WriteRole = "cso-order-write";
    private const string ServiceAccountUsername = "service-account-cso-jasper-dev";

    private readonly Mock<ILogger<CSoRoleAuthorizationHandler>> _loggerMock = new();

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CSoRoleAuthorizationHandler(_loggerMock.Object, null));
    }

    [Fact]
    public void Constructor_WithMissingAudience_ThrowsArgumentException()
    {
        var options = Options.Create(new KeycloakOptions
        {
            Audience = " ",
            Authority = "https://keycloak",
            ClientId = "client",
            WriteRole = WriteRole
        });

        Assert.Throws<ArgumentException>(() => new CSoRoleAuthorizationHandler(_loggerMock.Object, options));
    }

    [Fact]
    public void Constructor_WithMissingWriteRole_ThrowsArgumentException()
    {
        var options = Options.Create(new KeycloakOptions
        {
            Audience = Audience,
            Authority = "https://keycloak",
            ClientId = "client",
            WriteRole = "  "
        });

        Assert.Throws<ArgumentException>(() => new CSoRoleAuthorizationHandler(_loggerMock.Object, options));
    }

    [Fact]
    public async Task HandleRequirementAsync_UnauthenticatedUser_FailsRequirement()
    {
        var handler = CreateHandler();
        var requirement = new CsoRoleRequirement(CsoRoles.Write);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            CreateUser(isAuthenticated: false, roles: new[] { WriteRole }),
            null);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_NonServiceAccountUser_FailsRequirement()
    {
        var handler = CreateHandler();
        var requirement = new CsoRoleRequirement(CsoRoles.Write);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            CreateUser(isServiceAccount: false, roles: new[] { WriteRole }),
            null);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_NoClientRoles_FailsRequirement()
    {
        var handler = CreateHandler();
        var requirement = new CsoRoleRequirement(CsoRoles.Write);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            CreateUser(roles: null),
            null);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_WriteRolePresent_Succeeds()
    {
        var handler = CreateHandler();
        var requirement = new CsoRoleRequirement(CsoRoles.Write);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            CreateUser(roles: new[] { WriteRole.ToUpperInvariant() }),
            null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
    }

    [Fact]
    public async Task HandleRequirementAsync_WriteRoleMissing_FailsRequirement()
    {
        var handler = CreateHandler();
        var requirement = new CsoRoleRequirement(CsoRoles.Write);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            CreateUser(roles: new[] { "some-other-role" }),
            null);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UnsupportedRequiredRole_FailsRequirement()
    {
        var handler = CreateHandler();
        var requirement = new CsoRoleRequirement("unknown-role");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            CreateUser(roles: new[] { WriteRole }),
            null);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    private CSoRoleAuthorizationHandler CreateHandler()
    {
        var options = Options.Create(new KeycloakOptions
        {
            Audience = Audience,
            Authority = "https://keycloak",
            ClientId = "client",
            WriteRole = WriteRole
        });

        return new CSoRoleAuthorizationHandler(_loggerMock.Object, options);
    }

    private static ClaimsPrincipal CreateUser(
        bool isAuthenticated = true,
        bool isServiceAccount = true,
        IEnumerable<string> roles = null,
        string audience = Audience)
    {
        var identity = new ClaimsIdentity(isAuthenticated ? "Keycloak" : null);
        var preferredUsername = isServiceAccount ? ServiceAccountUsername : "regular-user";
        identity.AddClaim(new Claim(CustomClaimTypes.PreferredUsername, preferredUsername));

        if (roles != null)
        {
            identity.AddClaim(new Claim("resource_access", BuildResourceAccess(audience, roles)));
        }

        return new ClaimsPrincipal(identity);
    }

    private static string BuildResourceAccess(string audience, IEnumerable<string> roles)
    {
        var sanitizedRoles = (roles ?? Array.Empty<string>()).Where(role => !string.IsNullOrWhiteSpace(role));
        var encodedRoles = string.Join(",", sanitizedRoles.Select(role => $"\"{role}\""));
        return $"{{\"{audience}\":{{\"roles\":[{encodedRoles}]}}}}";
    }
}
