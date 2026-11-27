using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LazyCache;
using LazyCache.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;
using PCSSCommon.Clients.AuthorizationServices;
using PCSSCommon.Models;
using Scv.Api.Infrastructure.Authentication;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AccessControlManagement;
using Xunit;
using Role = Scv.Db.Models.Role;

namespace tests.api.Infrastructure.Authentication;

public class AuthenticationServiceCollectionExtensionTests
{
    [Fact]
    public void OpenIdConnectUsesTwentyMinutesForNonceAndCorrelation()
    {
        var options = GetOptions();
        var issuedAt = DateTimeOffset.UtcNow;
        var expectedExpiry = issuedAt.AddMinutes(20);
        var context = new DefaultHttpContext();

        Assert.Equal(TimeSpan.FromMinutes(20), options.ProtocolValidator.NonceLifetime);
        Assert.Equal(TimeSpan.FromMinutes(20), options.RemoteAuthenticationTimeout);
        Assert.Equal(expectedExpiry, options.NonceCookie.Build(context, issuedAt).Expires);
        Assert.Equal(expectedExpiry, options.CorrelationCookie.Build(context, issuedAt).Expires);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(17)]
    public void TokenResponseAcceptsMatchingNonceWithinWindow(int nonceAgeMinutes)
    {
        var nonce = CreateNonce(nonceAgeMinutes);
        GetOptions().ProtocolValidator.ValidateTokenResponse(CreateTokenResponse(nonce, nonce));
    }

    [Theory]
    [InlineData(21, "matching")]
    [InlineData(5, null)]
    [InlineData(5, "different")]
    public void TokenResponseRejectsExpiredMissingAndMismatchedNonce(int nonceAgeMinutes, string cookieNonce)
    {
        var nonce = CreateNonce(nonceAgeMinutes);
        var validationContext = CreateTokenResponse(nonce, cookieNonce == "matching" ? nonce : cookieNonce);

        Assert.Throws<OpenIdConnectProtocolInvalidNonceException>(() =>
            GetOptions().ProtocolValidator.ValidateTokenResponse(validationContext));
    }

    [Theory]
    [InlineData(false, false, "PCSS first", "PCSS last", "pcss@example.test")]
    [InlineData(false, true, "Stored first", "Stored last", "stored@example.test")]
    [InlineData(true, true, "Keycloak first", "Keycloak last", "keycloak@example.test")]
    public async Task PcssLoginPersistsLiveAuthorizationAndPreservesProfileDuringFallback(
        bool isNewUser, bool useFallback, string firstName, string lastName, string email)
    {
        using var login = new PcssLogin(isNewUser);
        if (useFallback)
        {
            await login.PrimeStaleList();
            login.FailUserList(HttpStatusCode.GatewayTimeout);
        }

        var context = await login.ValidateToken();

        Assert.Null(context.Result);
        var savedUser = Assert.IsType<UserDto>(login.SavedUser);
        Assert.Equal(firstName, savedUser.FirstName);
        Assert.Equal(lastName, savedUser.LastName);
        Assert.Equal(email, savedUser.Email);
        Assert.Equal("live-group", Assert.Single(savedUser.GroupIds));
        Assert.Equal("live-role-id", Assert.Single(savedUser.RoleIds));
        Assert.True(savedUser.IsActive);
        Assert.Equal(77, savedUser.JudgeId);
        Assert.Equal(PcssLogin.UserId, context.Principal.FindFirstValue(CustomClaimTypes.UserId));
        login.Client.Verify(client => client.GetUserAsync(PcssLogin.PcssUserId, CancellationToken.None), Times.Once);
        login.Users.Verify(users => users.AddAsync(It.IsAny<UserDto>()), isNewUser ? Times.Once() : Times.Never());
        login.Users.Verify(users => users.UpdateAsync(It.IsAny<UserDto>()), isNewUser ? Times.Never() : Times.Once());
    }

    [Theory]
    [InlineData(false, "different-guid", PcssLogin.PcssUserId)]
    [InlineData(true, "different-guid", PcssLogin.PcssUserId)]
    [InlineData(false, PcssLogin.ProvjudGuid, 43)]
    public async Task PcssLoginRejectsContradictedFallbackIdentityBeforeWritingUser(bool isNewUser, string guid, int id)
    {
        using var login = new PcssLogin(isNewUser);
        await login.PrimeStaleList();
        login.FailUserList(HttpStatusCode.GatewayTimeout);
        login.Client.Setup(client => client.GetUserAsync(PcssLogin.PcssUserId, CancellationToken.None))
            .ReturnsAsync(new UserItem { GUID = guid, UserId = id });

        var context = await login.ValidateToken();

        Assert.IsType<PcssFallbackIdentityMismatchException>(context.Result?.Failure);
        Assert.Null(context.Principal.FindFirstValue(CustomClaimTypes.UserId));
        Assert.Null(context.Principal.FindFirstValue(CustomClaimTypes.Permission));
        login.VerifyNoWrites();
    }

    [Theory]
    [InlineData(false, HttpStatusCode.ServiceUnavailable)]
    [InlineData(true, HttpStatusCode.ServiceUnavailable)]
    [InlineData(false, HttpStatusCode.GatewayTimeout)]
    [InlineData(true, HttpStatusCode.GatewayTimeout)]
    public async Task PcssLoginPreservesOrdinaryFailureBehaviorWhenNoFallbackIsAvailable(
        bool isNewUser, HttpStatusCode statusCode)
    {
        using var login = new PcssLogin(isNewUser);
        login.FailUserList(statusCode);

        var context = await login.ValidateToken();

        Assert.Null(context.Result);
        Assert.Equal(PcssLogin.UserId, context.Principal.FindFirstValue(CustomClaimTypes.UserId));
        login.Client.Verify(client => client.GetUserAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        login.Users.Verify(users => users.UpdateAsync(It.IsAny<UserDto>()), Times.Never);
        if (isNewUser)
        {
            Assert.Equal("keycloak@example.test", login.SavedUser.Email);
            Assert.False(login.SavedUser.IsActive);
        }
        else
        {
            Assert.Null(login.SavedUser);
            Assert.Equal("stored-permission", context.Principal.FindFirstValue(CustomClaimTypes.Permission));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PcssLoginCanceledDuringSyncRejectsAuthenticationBeforeWritingUser(bool isNewUser)
    {
        using var login = new PcssLogin(isNewUser);
        using var cancellation = new CancellationTokenSource();
        var roles = new TaskCompletionSource<UserItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        login.Client.Setup(client => client.GetUserAsync(PcssLogin.PcssUserId, CancellationToken.None)).Returns(roles.Task);

        var validation = login.ValidateToken(cancellation.Token);
        login.Client.Verify(client => client.GetUserAsync(PcssLogin.PcssUserId, CancellationToken.None), Times.Once);
        cancellation.Cancel();
        roles.SetResult(PcssLogin.LiveUser());
        var context = await validation;

        Assert.IsAssignableFrom<OperationCanceledException>(context.Result?.Failure);
        Assert.Null(context.Principal.FindFirstValue(CustomClaimTypes.UserId));
        login.VerifyNoWrites();
    }

    [Fact]
    public async Task ProvincialCourtLoginCanceledDuringGuidRecoveryRejectsAuthenticationBeforeCreatingUser()
    {
        const string email = "new.user@provincialcourt.bc.ca";
        using var cancellation = new CancellationTokenSource();
        var userInfoHandler = new Mock<HttpMessageHandler>();
        userInfoHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                cancellation.Cancel();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"sub":"test-user"}""", null, "application/json")
                };
            });
        using var userInfoClient = new HttpClient(userInfoHandler.Object);
        var users = new Mock<IUserService>();
        var sync = new Mock<IPcssSyncService>();
        sync.Setup(service => service.UpdateUserFromPcss(It.IsAny<UserDto>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        using var services = CreateAuthenticationServices(CreateConfiguration())
            .AddSingleton(users.Object)
            .AddSingleton(sync.Object)
            .AddSingleton(Mock.Of<IHttpClientFactory>(factory => factory.CreateClient(nameof(CookieAuthenticationEvents)) == userInfoClient))
            .BuildServiceProvider();
        var options = GetOptions(services);
        var httpContext = new DefaultHttpContext { RequestServices = services, RequestAborted = cancellation.Token };
        var scheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, email)], scheme.Name));
        var context = new TokenValidatedContext(httpContext, scheme, options, principal, new AuthenticationProperties())
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = "test-access-token" }
        };

        await options.Events.TokenValidated(context);

        Assert.IsAssignableFrom<OperationCanceledException>(context.Result?.Failure);
        Assert.Null(context.Principal.FindFirstValue(CustomClaimTypes.ProvjudUserGuid));
        Assert.Null(context.Principal.FindFirstValue(CustomClaimTypes.UserId));
        users.Verify(service => service.AddAsync(It.IsAny<UserDto>()), Times.Never);
    }

    [Fact]
    public async Task PcssLoginCanceledAfterTokenValidationDoesNotIssueSession()
    {
        using var login = new PcssLogin(isNewUser: false);
        using var cancellation = new CancellationTokenSource();
        var validation = await login.ValidateToken(cancellation.Token);
        Assert.Null(validation.Result);
        cancellation.Cancel();
        var ticket = new AuthenticationTicket(validation.Principal, validation.Properties, validation.Scheme.Name);
        var context = new TicketReceivedContext(validation.HttpContext, validation.Scheme, validation.Options, ticket);

        await validation.Options.Events.TicketReceived(context);

        Assert.True(context.Result?.Handled);
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Keycloak:Authority"] = "https://identity.example.test",
            ["Keycloak:Client"] = "jasper-test",
            ["Keycloak:Secret"] = "test-secret",
            ["MONGODB_CONNECTION_STRING"] = "mongodb://example.test",
            ["Caching:UserExpiryMinutes"] = "2",
            ["Request:PartId"] = "1",
            ["Request:AgencyIdentifierId"] = "1",
            ["PCSS:JudgeId"] = "1",
            ["PCSS:JudgeHomeLocationId"] = "1"
        }).Build();

    private static ServiceCollection CreateAuthenticationServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScvAuthentication(Mock.Of<IWebHostEnvironment>(), configuration);
        return services;
    }

    private static OpenIdConnectOptions GetOptions(IServiceProvider provider) =>
        provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

    private static OpenIdConnectOptions GetOptions()
    {
        using var provider = CreateAuthenticationServices(CreateConfiguration()).BuildServiceProvider();
        return GetOptions(provider);
    }

    private static string CreateNonce(int ageMinutes) =>
        DateTime.UtcNow.AddMinutes(-ageMinutes).ToBinary().ToString(CultureInfo.InvariantCulture) + ".test-value";

    private static OpenIdConnectProtocolValidationContext CreateTokenResponse(string nonce, string cookieNonce)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken("https://identity.example.test", "jasper-test",
        [
            new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
            new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Nonce, nonce)
        ], now.AddMinutes(-1), now.AddMinutes(10));

        return new OpenIdConnectProtocolValidationContext
        {
            ClientId = "jasper-test",
            Nonce = cookieNonce,
            ProtocolMessage = new OpenIdConnectMessage { IdToken = "id-token", AccessToken = "access-token" },
            ValidatedIdToken = token
        };
    }

    private sealed class PcssLogin : IDisposable
    {
        public const string UserId = "jasper-user";
        public const string ProvjudGuid = "1234";
        public const int PcssUserId = 42;
        private const string LiveRole = "live-role";
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private readonly ServiceProvider _services;
        private readonly PcssAuthorizationService _authorization;
        private readonly PcssLoginTimeProvider _time = new();

        public Mock<IUserService> Users { get; } = new();
        public Mock<IAuthorizationServicesClient> Client { get; } = new();
        public UserDto SavedUser { get; private set; }

        public PcssLogin(bool isNewUser)
        {
            if (!isNewUser)
            {
                Users.Setup(users => users.GetByGuidWithPermissionsAsync(ProvjudGuid)).ReturnsAsync(new UserDto
                {
                    Id = UserId, NativeGuid = ProvjudGuid, FirstName = "Stored first", LastName = "Stored last",
                    Email = "stored@example.test", Permissions = ["stored-permission"]
                });
            }
            Users.Setup(users => users.AddAsync(It.IsAny<UserDto>())).ReturnsAsync((UserDto user) => Save(user));
            Users.Setup(users => users.UpdateAsync(It.IsAny<UserDto>())).ReturnsAsync((UserDto user) => Save(user));
            Users.Setup(users => users.GetByIdWithPermissionsAsync(UserId)).ReturnsAsync(() => SavedUser);

            Client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
            {
                new UserItem
                {
                    GUID = ProvjudGuid, UserId = PcssUserId, GivenName = "PCSS first", Surname = "PCSS last",
                    Email = "pcss@example.test", Roles = [new EffectiveRoleItem { Name = "cached-role", EffectiveDate = "2000-01-01" }]
                }
            });
            Client.Setup(client => client.GetUserAsync(PcssUserId, CancellationToken.None)).ReturnsAsync(LiveUser());

            var groups = new Mock<IGroupService>();
            groups.Setup(service => service.GetGroupsByAliases(It.Is<IEnumerable<string>>(names => names.SequenceEqual(new[] { LiveRole }))))
                .ReturnsAsync(OperationResult<IEnumerable<GroupDto>>.Success([new GroupDto { Id = "live-group" }]));
            var roles = new Mock<IRoleService>();
            roles.Setup(service => service.GetRolesByAliases(It.Is<IEnumerable<string>>(names => names.SequenceEqual(new[] { LiveRole }))))
                .ReturnsAsync(OperationResult<IEnumerable<RoleDto>>.Success([new RoleDto { Id = "live-role-id" }]));
            var judges = new Mock<IJudgeService>();
            judges.Setup(service => service.GetJudges(null, null))
                .ReturnsAsync(new[] { new PersonSearchItem { PersonId = 77, UserId = PcssUserId, HomeLocationId = 4 } });

            var configuration = CreateConfiguration();
            var cache = new CachingService(new MemoryCacheProvider(_cache));
            _authorization = new PcssAuthorizationService(configuration, Client.Object, cache,
                NullLogger<PcssAuthorizationService>.Instance, timeProvider: _time);
            var sync = new PcssSyncService(_authorization, groups.Object, judges.Object, Mock.Of<IRepositoryBase<Group>>(),
                Mock.Of<IRepositoryBase<Role>>(), roles.Object, NullLogger<PcssSyncService>.Instance);
            _services = CreateAuthenticationServices(configuration)
                .AddSingleton<IUserService>(Users.Object)
                .AddSingleton<IPcssSyncService>(sync)
                .AddSingleton<IJudgeService>(judges.Object)
                .BuildServiceProvider();
        }

        public async Task PrimeStaleList()
        {
            await _authorization.RefreshUsers();
            _time.UtcNow += TimeSpan.FromMinutes(2);
        }

        public void FailUserList(HttpStatusCode statusCode) =>
            Client.Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ApiException("PCSS returned an error.", (int)statusCode, null,
                    new Dictionary<string, IEnumerable<string>>(), null));

        public async Task<TokenValidatedContext> ValidateToken(CancellationToken cancellationToken = default)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _services, RequestAborted = cancellationToken };
            var scheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(CustomClaimTypes.ProvjudUserGuid, Convert.ToBase64String(Convert.FromHexString(ProvjudGuid))),
                new Claim(ClaimTypes.GivenName, "Keycloak first"),
                new Claim(ClaimTypes.Surname, "Keycloak last"),
                new Claim(ClaimTypes.Email, "keycloak@example.test")
            ], scheme.Name));
            var options = GetOptions(_services);
            var context = new TokenValidatedContext(httpContext, scheme, options, principal, new AuthenticationProperties());
            await options.Events.TokenValidated(context);
            return context;
        }

        public static UserItem LiveUser() => new()
        {
            GUID = ProvjudGuid, UserId = PcssUserId,
            Roles = [new EffectiveRoleItem { Name = LiveRole, EffectiveDate = "2000-01-01" }]
        };

        public void VerifyNoWrites()
        {
            Users.Verify(users => users.AddAsync(It.IsAny<UserDto>()), Times.Never);
            Users.Verify(users => users.UpdateAsync(It.IsAny<UserDto>()), Times.Never);
        }

        private OperationResult<UserDto> Save(UserDto user)
        {
            user.Id = UserId;
            SavedUser = user;
            return OperationResult<UserDto>.Success(user);
        }

        public void Dispose()
        {
            _services.Dispose();
            _cache.Dispose();
        }
    }

    private sealed class PcssLoginTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
