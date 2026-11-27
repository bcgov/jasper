using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Moq.Protected;
using Scv.Api.Infrastructure.Authentication;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AccessControlManagement;
using Xunit;
using PcssUser = PCSSCommon.Clients.AuthorizationServices.UserItem;

namespace tests.api.Infrastructure.Authentication;

public class AuthenticationServiceCollectionExtensionTests
{
    private const string UserId = "user-id";
    private const string Email = "user@example.com";
    private const string NativeGuid = "01000000000000000000000000000000";
    private const string EncodedGuid = "AQAAAAAAAAAAAAAAAAAAAA==";
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IPcssSyncService> _pcssSyncService = new();

    [Theory]
    [InlineData(PcssSyncOutcome.Synchronized, true)]
    [InlineData(PcssSyncOutcome.Synchronized, false)]
    [InlineData(PcssSyncOutcome.NoAccess, true)]
    public async Task TokenValidated_UsesPersistedAuthorizationForExistingUser(PcssSyncOutcome outcome, bool hasChanges)
    {
        var user = new UserDto { Id = UserId, NativeGuid = NativeGuid, IsActive = true, RoleIds = ["old-role"], GroupIds = ["old-group"] };
        var persisted = outcome == PcssSyncOutcome.NoAccess
            ? new UserDto { Id = UserId, NativeGuid = NativeGuid, IsPendingRegistration = false }
            : new UserDto { Id = UserId, NativeGuid = NativeGuid, IsActive = true, Roles = ["Verified role"], GroupIds = ["verified-group"], JudgeId = 456 };
        _userService.Setup(s => s.GetForPcssSyncAsync(NativeGuid, Email)).ReturnsAsync(user);
        _userService.Setup(s => s.GetByIdWithPermissionsAsync(UserId)).ReturnsAsync(persisted);
        _userService.Setup(s => s.UpdateFromPcssAsync(user)).ReturnsAsync(OperationResult.Success());
        _pcssSyncService.Setup(s => s.UpdateUserFromPcss(user))
            .Callback<UserDto>(dto =>
            {
                if (outcome == PcssSyncOutcome.NoAccess)
                {
                    dto.IsActive = false;
                    dto.GroupIds = [];
                    dto.RoleIds = [];
                }
            })
            .ReturnsAsync(new PcssSyncResult(outcome, hasChanges));

        var judges = new Mock<IJudgeService>();
        judges.Setup(s => s.GetJudges(null, null)).ReturnsAsync([
            new PCSSCommon.Models.PersonSearchItem { PersonId = 789, HomeLocationId = 10 },
            new PCSSCommon.Models.PersonSearchItem { PersonId = 456, HomeLocationId = 20 }
        ]);
        using var services = CreateServices(judgeService: judges.Object);
        var context = await ValidateTokenAsync(services, externalJudgeId: "789");

        Assert.Null(context.Result);
        Assert.Equal(UserId, context.Principal.FindFirst(CustomClaimTypes.UserId)?.Value);
        Assert.Equal(persisted.IsActive.ToString(), context.Principal.FindFirst(CustomClaimTypes.IsActive)?.Value);
        if (outcome == PcssSyncOutcome.NoAccess)
        {
            Assert.Null(context.Principal.FindFirst(CustomClaimTypes.Groups));
            Assert.Null(context.Principal.FindFirst(CustomClaimTypes.Role));
            _userService.Verify(s => s.UpdateFromPcssAsync(
                It.Is<UserDto>(dto => !dto.IsActive && dto.GroupIds.Count == 0 && dto.RoleIds.Count == 0)), Times.Once);
        }
        else
        {
            Assert.Equal("verified-group", context.Principal.FindFirst(CustomClaimTypes.Groups)?.Value);
            Assert.Equal("Verified role", context.Principal.FindFirst(CustomClaimTypes.Role)?.Value);
            Assert.Equal("456", context.Principal.FindFirst(CustomClaimTypes.JudgeId)?.Value);
            Assert.Equal("20", context.Principal.FindFirst(CustomClaimTypes.JudgeHomeLocationId)?.Value);
        }

        _userService.Verify(s => s.UpdateFromPcssAsync(user), hasChanges ? Times.Once() : Times.Never());
        _userService.Verify(s => s.GetByIdWithPermissionsAsync(UserId), Times.Once);
    }

    [Fact]
    public async Task TokenValidated_UsesConfiguredJudgeWhenPcssSynchronizationRemovesPersonalMapping()
    {
        var user = new UserDto { Id = UserId, NativeGuid = NativeGuid, IsActive = true, JudgeId = 456 };
        var persisted = new UserDto
        {
            Id = UserId,
            NativeGuid = NativeGuid,
            IsActive = true,
            Roles = ["Judge"],
            GroupIds = ["judiciary"]
        };
        _userService.Setup(s => s.GetForPcssSyncAsync(NativeGuid, Email)).ReturnsAsync(user);
        _userService.Setup(s => s.GetByIdWithPermissionsAsync(UserId)).ReturnsAsync(persisted);
        _userService.Setup(s => s.UpdateFromPcssAsync(user)).ReturnsAsync(OperationResult.Success());
        _pcssSyncService.Setup(s => s.UpdateUserFromPcss(user))
            .Callback<UserDto>(dto => dto.JudgeId = null)
            .ReturnsAsync(new PcssSyncResult(PcssSyncOutcome.Synchronized, HasChanges: true));

        using var services = CreateServices();
        var context = await ValidateTokenAsync(services);

        Assert.Null(context.Result);
        Assert.Equal("True", context.Principal.FindFirst(CustomClaimTypes.IsActive)?.Value);
        Assert.Equal("Judge", context.Principal.FindFirst(CustomClaimTypes.Role)?.Value);
        Assert.Equal("123", context.Principal.FindFirst(CustomClaimTypes.JudgeId)?.Value);
        Assert.Equal("1", context.Principal.FindFirst(CustomClaimTypes.JudgeHomeLocationId)?.Value);
    }

    [Theory]
    [InlineData(PcssSyncOutcome.Synchronized, false)]
    [InlineData(PcssSyncOutcome.Synchronized, true)]
    [InlineData(PcssSyncOutcome.NoAccess, false)]
    public async Task TokenValidated_FailsWhenSavingOrReloadingPcssUserFails(PcssSyncOutcome outcome, bool saveSucceeds)
    {
        var user = new UserDto { Id = UserId, NativeGuid = NativeGuid, IsActive = true };
        _userService.Setup(s => s.GetForPcssSyncAsync(NativeGuid, Email)).ReturnsAsync(user);
        _pcssSyncService.Setup(s => s.UpdateUserFromPcss(user))
            .ReturnsAsync(new PcssSyncResult(outcome, true));
        _userService.Setup(s => s.UpdateFromPcssAsync(user)).ReturnsAsync(saveSucceeds
            ? OperationResult.Success() : OperationResult.Failure("Save failed."));
        _userService.Setup(s => s.GetByIdWithPermissionsAsync(UserId)).ReturnsAsync(saveSucceeds
            ? null : new UserDto { Id = UserId, IsActive = true, Roles = ["Old role"], GroupIds = ["old-group"], JudgeId = 456 });

        using var services = CreateServices();
        var context = await ValidateTokenAsync(services);

        Assert.NotNull(context.Result?.Failure);
        Assert.Null(context.Principal.FindFirst(CustomClaimTypes.UserId));
        _userService.Verify(s => s.GetByIdWithPermissionsAsync(UserId), saveSucceeds ? Times.Once() : Times.Never());
    }

    [Theory]
    [InlineData("connection")]
    [InlineData("timeout")]
    [InlineData("exception")]
    [InlineData("incomplete roles")]
    [InlineData("group alias")]
    [InlineData("role alias")]
    public async Task TokenValidated_ContinuesUsingStoredGrantsWhenPcssSynchronizationFails(string failure)
    {
        var user = new UserDto
        {
            Id = UserId,
            NativeGuid = NativeGuid,
            IsActive = true,
            IsPendingRegistration = true,
            JudgeId = 456,
            GroupIds = ["judiciary"],
            RoleIds = ["judge"]
        };
        _userService.Setup(s => s.GetForPcssSyncAsync(NativeGuid, Email)).ReturnsAsync(user);
        var persisted = new UserDto
        {
            Id = UserId,
            NativeGuid = NativeGuid,
            IsActive = true,
            IsPendingRegistration = true,
            JudgeId = 456,
            GroupIds = ["judiciary"],
            RoleIds = ["judge"],
            Roles = ["Judge"],
            Permissions = ["Read"]
        };
        _userService.Setup(s => s.GetByIdWithPermissionsAsync(UserId)).ReturnsAsync(persisted);

        var pcss = new Mock<IPcssAuthorizationService>();
        var lookup = pcss.Setup(s => s.GetUserByGuid(NativeGuid, true));
        var lookupError = failure switch
        {
            "connection" => new HttpRequestException("PCSS unavailable"),
            "timeout" => new TimeoutException(),
            "exception" => new InvalidOperationException("PCSS lookup failed"),
            _ => (Exception)null
        };
        if (lookupError != null)
        {
            lookup.ThrowsAsync(lookupError);
        }
        else
        {
            lookup.ReturnsAsync(new PcssUser { UserId = 123 });
        }
        pcss.Setup(s => s.GetPcssUserRoleNames(123)).ReturnsAsync(failure == "incomplete roles"
            ? OperationResult<IEnumerable<string>>.Success(null)
            : OperationResult<IEnumerable<string>>.Success([RoleAlias.JUDGE]));
        var groups = new Mock<IGroupService>();
        groups.Setup(s => s.GetGroupsByAliases(It.IsAny<IEnumerable<string>>())).ReturnsAsync(
            failure == "group alias" ? OperationResult<IEnumerable<GroupDto>>.Failure("Missing group alias")
                : OperationResult<IEnumerable<GroupDto>>.Success([new GroupDto { Id = "judiciary", RoleIds = ["judge", "admin"] }]));
        var roleAliases = new Mock<IRepositoryBase<RoleAlias>>();
        roleAliases.Setup(r => r.FindAsync(It.IsAny<Expression<Func<RoleAlias, bool>>>())).ReturnsAsync([]);
        var roleRepo = new Mock<IRepositoryBase<Role>>();
        roleRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Role, bool>>>())).ReturnsAsync([]);
        var groupRepo = new Mock<IRepositoryBase<Group>>();
        groupRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Group, bool>>>())).ReturnsAsync([]);
        var judges = new Mock<IJudgeService>();
        judges.Setup(s => s.GetJudges(null, null)).ThrowsAsync(new HttpRequestException("PCSS judges unavailable"));
        var roles = new RoleService(Mock.Of<IAppCache>(), Mock.Of<IMapper>(), Mock.Of<ILogger<RoleService>>(),
            roleRepo.Object, Mock.Of<IPermissionRepository>(), groupRepo.Object, roleAliases.Object);
        var sync = new PcssSyncService(pcss.Object, groups.Object, judges.Object, groupRepo.Object, roleRepo.Object, roles,
            Mock.Of<ILogger<PcssSyncService>>());
        _pcssSyncService.Setup(s => s.UpdateUserFromPcss(user)).Returns(() => sync.UpdateUserFromPcss(user));

        using var services = CreateServices(judgeService: judges.Object);
        var context = await ValidateTokenAsync(services, externalJudgeId: "789");

        Assert.Null(context.Result);
        Assert.Equal(UserId, context.Principal.FindFirst(CustomClaimTypes.UserId)?.Value);
        Assert.Equal("Judge", Assert.Single(context.Principal.FindAll(CustomClaimTypes.Role)).Value);
        Assert.Equal("Read", Assert.Single(context.Principal.FindAll(CustomClaimTypes.Permission)).Value);
        Assert.Equal("456", context.Principal.FindFirst(CustomClaimTypes.JudgeId)?.Value);
        Assert.Equal("1", context.Principal.FindFirst(CustomClaimTypes.JudgeHomeLocationId)?.Value);
        Assert.Equal(new[] { "judge" }, user.RoleIds);
        Assert.Equal(new[] { "judiciary" }, user.GroupIds);
        Assert.True(user.IsActive);
        Assert.True(user.IsPendingRegistration);
        Assert.True(persisted.IsPendingRegistration);
        _userService.Verify(s => s.UpdateFromPcssAsync(It.IsAny<UserDto>()), Times.Never);
        _userService.Verify(s => s.AddAsync(It.IsAny<UserDto>()), Times.Never);
        _userService.Verify(s => s.GetByIdWithPermissionsAsync(UserId), Times.Once);
        judges.Verify(s => s.GetJudges(null, null), Times.Never);
    }

    [Theory]
    [InlineData(true, true, 456, "456")]
    [InlineData(true, true, null, "123")]
    [InlineData(true, true, 0, "123")]
    [InlineData(true, true, -1, "123")]
    [InlineData(false, true, 456, null)]
    [InlineData(false, true, null, null)]
    [InlineData(true, false, 456, null)]
    [InlineData(true, false, null, null)]
    public async Task TokenValidated_UsesOnlyPersistedEligibilityAndClaimsAfterPcssFailure(
        bool isActive, bool hasRoles, int? judgeId, string expectedJudgeClaim)
    {
        var user = new UserDto { Id = UserId, NativeGuid = NativeGuid };
        var persisted = new UserDto
        {
            Id = UserId,
            NativeGuid = NativeGuid,
            IsActive = isActive,
            IsPendingRegistration = true,
            JudgeId = judgeId,
            GroupIds = ["stored-group"],
            RoleIds = ["stored-role"],
            Roles = hasRoles ? ["Stored role"] : [],
            Permissions = ["Stored permission"]
        };
        _userService.Setup(s => s.GetForPcssSyncAsync(NativeGuid, Email)).ReturnsAsync(user);
        _userService.Setup(s => s.GetByIdWithPermissionsAsync(UserId)).ReturnsAsync(persisted);
        _pcssSyncService.Setup(s => s.UpdateUserFromPcss(user))
            .Callback<UserDto>(dto =>
            {
                dto.Id = "partial-user";
                dto.IsActive = !isActive;
                dto.IsPendingRegistration = false;
                dto.GroupIds = ["partial-group"];
                dto.Roles = ["Partial role"];
                dto.Permissions = ["Partial permission"];
                dto.JudgeId = 789;
            })
            .ReturnsAsync(new PcssSyncResult(PcssSyncOutcome.Failed, HasChanges: true));

        var judges = new Mock<IJudgeService>();
        using var services = CreateServices(judgeService: judges.Object);
        var context = await ValidateTokenAsync(services, externalJudgeId: "789");

        if (expectedJudgeClaim != null)
        {
            Assert.Null(context.Result);
            Assert.Equal(UserId, context.Principal.FindFirst(CustomClaimTypes.UserId)?.Value);
            Assert.Equal(expectedJudgeClaim, context.Principal.FindFirst(CustomClaimTypes.JudgeId)?.Value);
            Assert.Equal("1", context.Principal.FindFirst(CustomClaimTypes.JudgeHomeLocationId)?.Value);
            Assert.Equal("stored-group", Assert.Single(context.Principal.FindAll(CustomClaimTypes.Groups)).Value);
            Assert.Equal("Stored role", Assert.Single(context.Principal.FindAll(CustomClaimTypes.Role)).Value);
            Assert.Equal("Stored permission", Assert.Single(context.Principal.FindAll(CustomClaimTypes.Permission)).Value);
        }
        else
        {
            Assert.NotNull(context.Result?.Failure);
            Assert.Null(context.Principal.FindFirst(CustomClaimTypes.UserId));
            Assert.Null(context.Principal.FindFirst(CustomClaimTypes.Role));
            Assert.Null(context.Principal.FindFirst(CustomClaimTypes.JudgeId));
            Assert.Null(context.Principal.FindFirst(CustomClaimTypes.JudgeHomeLocationId));
        }

        Assert.True(persisted.IsPendingRegistration);
        _userService.Verify(s => s.GetByIdWithPermissionsAsync(UserId), Times.Once);
        _userService.Verify(s => s.GetByIdWithPermissionsAsync("partial-user"), Times.Never);
        _userService.Verify(s => s.UpdateFromPcssAsync(It.IsAny<UserDto>()), Times.Never);
        judges.Verify(s => s.GetJudges(null, null), Times.Never);
    }

    [Fact]
    public async Task TokenValidated_MismatchedGuidCannotUseStoredAuthorization()
    {
        var user = new UserDto
        {
            Id = UserId,
            NativeGuid = "02000000000000000000000000000000",
            IsActive = true,
            JudgeId = 456,
            Roles = ["Judge"],
            GroupIds = ["judiciary"]
        };
        _userService.Setup(s => s.GetForPcssSyncAsync(NativeGuid, Email)).ReturnsAsync(user);

        using var services = CreateServices();
        var context = await ValidateTokenAsync(services);

        Assert.NotNull(context.Result?.Failure);
        Assert.Null(context.Principal.FindFirst(CustomClaimTypes.UserId));
        _pcssSyncService.Verify(s => s.UpdateUserFromPcss(It.IsAny<UserDto>()), Times.Never);
        _userService.Verify(s => s.GetByIdWithPermissionsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task TokenValidated_PersistsNewGuidWhenSynchronizationMadeNoOtherChanges()
    {
        var user = new UserDto { Id = UserId, IsActive = true };
        _userService.Setup(s => s.GetForPcssSyncAsync(NativeGuid, Email)).ReturnsAsync(user);
        _pcssSyncService.Setup(s => s.UpdateUserFromPcss(user)).ReturnsAsync(new PcssSyncResult(PcssSyncOutcome.Synchronized));
        _userService.Setup(s => s.UpdateFromPcssAsync(user)).ReturnsAsync(OperationResult.Success());
        _userService.Setup(s => s.GetByIdWithPermissionsAsync(UserId)).ReturnsAsync(user);

        using var services = CreateServices();
        var context = await ValidateTokenAsync(services);

        Assert.Null(context.Result);
        _userService.Verify(s => s.UpdateFromPcssAsync(It.Is<UserDto>(dto => dto.NativeGuid == NativeGuid)), Times.Once);
    }

    [Theory]
    [InlineData("{\"sub\":\"user\"}")]
    [InlineData("{\"sub\":\"user\",\"provjud_user_object_guid\":\"invalid\"}")]
    public async Task TokenValidated_FailsWhenKeycloakReturnsNoValidGuid(string userInfo)
    {
        var responseHandler = new Mock<HttpMessageHandler>();
        responseHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(userInfo, Encoding.UTF8, "application/json") });
        using var client = new HttpClient(responseHandler.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(nameof(OpenIdConnectEvents))).Returns(client);
        using var services = CreateServices(factory.Object);

        var context = await ValidateTokenAsync(services, encodedGuid: null, email: "user@provincialcourt.bc.ca", accessToken: "test-token");

        Assert.NotNull(context.Result?.Failure);
        factory.Verify(f => f.CreateClient(nameof(OpenIdConnectEvents)), Times.Once);
        responseHandler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        _userService.Verify(s => s.GetWithPermissionsAsync(It.IsAny<string>()), Times.Never);
        _userService.Verify(s => s.AddAsync(It.IsAny<UserDto>()), Times.Never);
    }

    [Theory]
    [InlineData(PcssSyncOutcome.Synchronized)]
    [InlineData(PcssSyncOutcome.NoAccess)]
    [InlineData(PcssSyncOutcome.Failed)]
    public async Task TokenValidated_UsesPcssOutcomeBeforeCreatingUser(PcssSyncOutcome outcome)
    {
        var shouldBeActive = outcome == PcssSyncOutcome.Synchronized;
        var persistedUser = new UserDto { Id = UserId, IsActive = shouldBeActive };
        _userService.Setup(s => s.AddAsync(It.IsAny<UserDto>())).ReturnsAsync(OperationResult<UserDto>.Success(persistedUser));
        _userService.Setup(s => s.GetByIdWithPermissionsAsync(UserId)).ReturnsAsync(persistedUser);
        _pcssSyncService.Setup(s => s.UpdateUserFromPcss(It.IsAny<UserDto>()))
            .Callback<UserDto>(user => user.IsActive = shouldBeActive)
            .ReturnsAsync(new PcssSyncResult(outcome));

        using var services = CreateServices();
        var context = await ValidateTokenAsync(services);

        if (outcome == PcssSyncOutcome.Failed)
        {
            Assert.NotNull(context.Result?.Failure);
            Assert.Null(context.Principal.FindFirst(CustomClaimTypes.UserId));
            _userService.Verify(s => s.AddAsync(It.IsAny<UserDto>()), Times.Never);
        }
        else
        {
            Assert.Null(context.Result);
            Assert.Equal(UserId, context.Principal.FindFirst(CustomClaimTypes.UserId)?.Value);
            Assert.Equal(shouldBeActive.ToString(), context.Principal.FindFirst(CustomClaimTypes.IsActive)?.Value);
            _userService.Verify(s => s.AddAsync(It.Is<UserDto>(user => user.IsActive == shouldBeActive)), Times.Once);
        }
    }

    [Theory]
    [InlineData("user@idir")]
    [InlineData("user@vc")]
    public async Task TokenValidated_PreservesNonPcssAuthorization(string preferredUsername)
    {
        const string email = "user@provincialcourt.bc.ca";
        _userService.Setup(s => s.GetWithPermissionsAsync(email))
            .ReturnsAsync(new UserDto { Id = UserId, IsActive = true, Roles = ["Manual role"] });
        using var services = CreateServices();

        var context = await ValidateTokenAsync(services, encodedGuid: null, email: email, preferredUsername: preferredUsername);

        Assert.Null(context.Result);
        Assert.Equal("Manual role", context.Principal.FindFirst(CustomClaimTypes.Role)?.Value);
        Assert.Equal("123", context.Principal.FindFirst(CustomClaimTypes.JudgeId)?.Value);
        Assert.Equal("1", context.Principal.FindFirst(CustomClaimTypes.JudgeHomeLocationId)?.Value);
        _pcssSyncService.Verify(s => s.UpdateUserFromPcss(It.IsAny<UserDto>()), Times.Never);
        _userService.Verify(s => s.UpdateFromPcssAsync(It.IsAny<UserDto>()), Times.Never);
    }

    [Theory]
    [InlineData("", "/", "pcss_authorization_failed")]
    [InlineData("/jasper/test", "/jasper/test/", "pcss_authorization_failed")]
    [InlineData("", "/", "authentication_failed")]
    [InlineData("/jasper/test", "/jasper/test/", "authentication_failed")]
    [InlineData("//untrusted.example", "/", "authentication_failed")]
    public async Task OidcCallback_RedirectsOnFailureWithoutIssuingTicket(string pathBase, string frontendPath, string expectedError)
    {
        var isPcssFailure = expectedError == "pcss_authorization_failed";
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Request.Scheme = "https";
        httpContext.Request.PathBase = new PathString(pathBase);
        httpContext.Request.Path = "/api/auth/signin-oidc";
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.QueryString = new QueryString("?code=callback&redirectUri=https%3A%2F%2Funtrusted.example%2F");
        var cookieOptions = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var staleTicket = new AuthenticationTicket(CreatePrincipal(EncodedGuid, Email, null), CookieAuthenticationDefaults.AuthenticationScheme);
        httpContext.Request.Headers.Cookie = $"SCV={cookieOptions.TicketDataFormat.Protect(staleTicket)}";

        var options = GetOidcOptions(provider);
        var ticketReceived = false;
        options.Events.OnTicketReceived = _ =>
        {
            ticketReceived = true;
            return Task.CompletedTask;
        };
        if (isPcssFailure)
        {
            _pcssSyncService.Setup(s => s.UpdateUserFromPcss(It.IsAny<UserDto>()))
                .ReturnsAsync(new PcssSyncResult(PcssSyncOutcome.Failed));
            // Supply the token-validation failure at the protocol event without calling Keycloak.
            options.Events.OnMessageReceived = async message =>
            {
                var properties = new AuthenticationProperties { RedirectUri = "https://untrusted.example/" };
                var token = new TokenValidatedContext(httpContext, message.Scheme, options, CreatePrincipal(EncodedGuid, Email, null), properties);
                await options.Events.TokenValidated(token);
                Assert.NotNull(token.Result?.Failure);
                message.Fail(token.Result.Failure);
            };
        }

        var handler = await provider.GetRequiredService<IAuthenticationHandlerProvider>()
            .GetHandlerAsync(httpContext, OpenIdConnectDefaults.AuthenticationScheme);
        Assert.True(await Assert.IsAssignableFrom<IAuthenticationRequestHandler>(handler).HandleRequestAsync());

        Assert.False(ticketReceived);
        Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        Assert.Equal(frontendPath + "?authError=" + expectedError, httpContext.Response.Headers.Location.ToString());
        if (isPcssFailure)
        {
            Assert.Contains(httpContext.Response.Headers.SetCookie, cookie => cookie.StartsWith("SCV=;") && cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            Assert.DoesNotContain(httpContext.Response.Headers.SetCookie, cookie => cookie.StartsWith("SCV="));
        }
        _userService.Verify(s => s.AddAsync(It.IsAny<UserDto>()), Times.Never);
    }

    [Theory]
    [InlineData("pcss_authorization_failed", true)]
    [InlineData("authentication_failed", true)]
    [InlineData(null, false)]
    public async Task OidcChallenge_BlocksAutomaticRetryAndAllowsCleanReturnUrl(string authError, bool blocksRetry)
    {
        using var services = CreateServices();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.Path = "/api/auth/login";
        httpContext.Request.PathBase = "/jasper";
        var returnUrl = "https://app.example.com/jasper/";
        if (authError != null)
        {
            returnUrl += "?authError=" + authError;
        }
        httpContext.Request.QueryString = QueryString.Create("redirectUri", returnUrl);
        httpContext.Request.Headers["X-Forwarded-Host"] = "app.example.com";
        httpContext.Request.Headers["X-Forwarded-Port"] = "443";
        httpContext.Request.Headers["X-Base-Href"] = "/jasper/";
        var options = GetOidcOptions(services);
        var redirect = new RedirectContext(httpContext, CreateScheme(), options, new AuthenticationProperties())
        {
            ProtocolMessage = new OpenIdConnectMessage { RequestType = OpenIdConnectRequestType.Authentication }
        };

        await options.Events.RedirectToIdentityProvider(redirect);

        Assert.Equal(blocksRetry, redirect.Handled);
        if (blocksRetry)
        {
            Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        }
        else
        {
            Assert.Equal("https://app.example.com/jasper/api/auth/signin-oidc", redirect.ProtocolMessage.RedirectUri);
        }
    }

    private ServiceProvider CreateServices(IHttpClientFactory httpClientFactory = null, IJudgeService judgeService = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Keycloak:Authority"] = "https://identity.example.com",
            ["Keycloak:Client"] = "jasper",
            ["Keycloak:Secret"] = "test-secret",
            ["Keycloak:PresReqConfId"] = "test-config",
            ["MONGODB_CONNECTION_STRING"] = "configured",
            ["Request:PartId"] = "1",
            ["Request:AgencyIdentifierId"] = "1",
            ["PCSS:JudgeId"] = "123",
            ["PCSS:JudgeHomeLocationId"] = "1"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddSingleton<IConfiguration>(configuration);
        if (httpClientFactory != null)
        {
            services.AddSingleton(httpClientFactory);
        }
        if (judgeService != null)
        {
            services.AddSingleton(judgeService);
        }
        services.AddSingleton(_userService.Object);
        services.AddSingleton(_pcssSyncService.Object);
        services.AddScvAuthentication(Mock.Of<IWebHostEnvironment>(), configuration);
        return services.BuildServiceProvider();
    }

    private static async Task<TokenValidatedContext> ValidateTokenAsync(IServiceProvider services, string encodedGuid = EncodedGuid,
        string email = Email, string preferredUsername = null, string accessToken = null, string externalJudgeId = null)
    {
        var options = GetOidcOptions(services);
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var principal = CreatePrincipal(encodedGuid, email, preferredUsername);
        if (externalJudgeId != null)
        {
            ((ClaimsIdentity)principal.Identity).AddClaim(new Claim(CustomClaimTypes.ExternalJudgeId, externalJudgeId));
        }
        var context = new TokenValidatedContext(httpContext, CreateScheme(), options, principal, new AuthenticationProperties())
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = accessToken }
        };
        await options.Events.TokenValidated(context);
        return context;
    }

    private static OpenIdConnectOptions GetOidcOptions(IServiceProvider services)
        => services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

    private static AuthenticationScheme CreateScheme()
        => new(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));

    private static ClaimsPrincipal CreatePrincipal(string encodedGuid, string email, string preferredUsername)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], OpenIdConnectDefaults.AuthenticationScheme);
        if (encodedGuid != null)
        {
            identity.AddClaim(new Claim(CustomClaimTypes.ProvjudUserGuid, encodedGuid));
        }
        if (preferredUsername != null)
        {
            identity.AddClaim(new Claim(CustomClaimTypes.PreferredUsername, preferredUsername));
        }

        return new ClaimsPrincipal(identity);
    }
}
