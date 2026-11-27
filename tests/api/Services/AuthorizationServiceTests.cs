using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.AuthorizationServices;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;

public class AuthorizationServiceTests
{
    private readonly Faker _faker;
    private readonly Mock<IAuthorizationServicesClient> _mockPcssAuthorizationServiceClient;
    private readonly PcssAuthorizationService _authorizationService;

    public AuthorizationServiceTests()
    {
        _faker = new Faker();

        var mockConfig = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        var mockLogger = new Mock<ILogger<PcssAuthorizationService>>();
        mockSection.Setup(s => s.Value).Returns("60");
        mockConfig.Setup(c => c.GetSection("Caching:UserExpiryMinutes")).Returns(mockSection.Object);

        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        _mockPcssAuthorizationServiceClient = new Mock<IAuthorizationServicesClient>();

        _authorizationService = new PcssAuthorizationService(
            mockConfig.Object,
            _mockPcssAuthorizationServiceClient.Object,
            cachingService,
            mockLogger.Object);
    }

    [Fact]
    public async Task GetUserByGuid_ShouldFail_WhenRefreshedUserListIsMissing()
    {
        _mockPcssAuthorizationServiceClient
            .SetupSequence(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserItem>())
            .ReturnsAsync((ICollection<UserItem>)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _authorizationService.GetUserByGuid("guid", forceRefresh: true));
    }

    [Fact]
    public async Task GetUserByGuid_MatchesGuidFromRefreshedListRegardlessOfHexCase()
    {
        var user = new UserItem { GUID = "ABC", UserId = 123 };
        _mockPcssAuthorizationServiceClient
            .SetupSequence(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserItem>())
            .ReturnsAsync(new List<UserItem> { user });

        var result = await _authorizationService.GetUserByGuid("abc", forceRefresh: true);

        Assert.Same(user, result);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnUsersFromPcss()
    {
        // Arrange
        var users = new List<UserItem>
        {
            new() { UserId = _faker.Random.Int(), GivenName = _faker.Name.FirstName(), Surname = _faker.Name.LastName() },
            new() { UserId = _faker.Random.Int(), GivenName = _faker.Name.FirstName(), Surname = _faker.Name.LastName() }
        };

        _mockPcssAuthorizationServiceClient
            .Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _authorizationService.GetUsers();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(users.Count, result.Count);
        Assert.Equal(users[0].UserId, result.First().UserId);
        _mockPcssAuthorizationServiceClient.Verify(client => client.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetPcssUserRoleNames_ShouldReturnFailure_WhenUserOrRoleDataIsMissing(bool hasUser)
    {
        // Arrange
        var userId = _faker.Random.Int();
        _mockPcssAuthorizationServiceClient
            .Setup(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasUser ? new UserItem { UserId = userId, Roles = null } : null);

        // Act
        var result = await _authorizationService.GetPcssUserRoleNames(userId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("PCSS did not return user role data.", result.Errors[0]);
        _mockPcssAuthorizationServiceClient.Verify(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPcssUserRoleNames_ShouldReturnEmptyRoles_WhenUserHasNoRoles()
    {
        // Arrange
        var userId = _faker.Random.Int();
        var user = new UserItem
        {
            UserId = userId,
            Roles = [] // Empty roles
        };

        _mockPcssAuthorizationServiceClient
            .Setup(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authorizationService.GetPcssUserRoleNames(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Payload);
        _mockPcssAuthorizationServiceClient.Verify(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPcssUserRoleNames_ShouldReturnOnlyCurrentlyEffectiveUnexpiredRoles()
    {
        var now = DateTime.Now;
        var past = now.AddDays(-1).ToString("o", CultureInfo.InvariantCulture);
        var future = now.AddDays(1).ToString("o", CultureInfo.InvariantCulture);
        _mockPcssAuthorizationServiceClient
            .Setup(client => client.GetUserAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserItem
            {
                UserId = 123,
                Roles =
                [
                    new EffectiveRoleItem { Name = "Invalid effective date", EffectiveDate = "invalid", ExpiryDate = future },
                    new EffectiveRoleItem { Name = "Invalid expiry date", EffectiveDate = past, ExpiryDate = "invalid" },
                    new EffectiveRoleItem { Name = "Missing effective date", ExpiryDate = future },
                    new EffectiveRoleItem { Name = "Expired", EffectiveDate = past, ExpiryDate = past },
                    new EffectiveRoleItem { Name = "Future", EffectiveDate = future },
                    new EffectiveRoleItem { Name = "Current", EffectiveDate = past, ExpiryDate = future },
                    new EffectiveRoleItem { Name = "No expiry", EffectiveDate = past }
                ]
            });

        var result = await _authorizationService.GetPcssUserRoleNames(123);

        Assert.True(result.Succeeded);
        Assert.Equal(new[] { "Current", "No expiry" }, result.Payload);
    }

    [Fact]
    public async Task GetPcssUserRoleNames_ShouldReturnSuccess_WhenRolesFound()
    {
        // Arrange
        var userId = _faker.Random.Int();
        var roleName = _faker.Random.Word();
        var user = new UserItem
        {
            UserId = userId,
            Roles =
            [
                new EffectiveRoleItem
                {
                    Name = roleName,
                    EffectiveDate = DateTime.Now.AddDays(-1).ToString("o"),
                    ExpiryDate = DateTime.Now.AddDays(30).ToString("o")
                }
            ]
        };

        _mockPcssAuthorizationServiceClient
            .Setup(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authorizationService.GetPcssUserRoleNames(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Single(result.Payload);
        Assert.Equal(roleName, result.Payload.First());
        _mockPcssAuthorizationServiceClient.Verify(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
