using Bogus;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.AuthorizationServices;
using Scv.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public async Task GetUsers_ShouldReturnUsersFromPcss()
    {
        // Arrange
        var users = new List<UserItem>
        {
            new UserItem { UserId = _faker.Random.Int(), GivenName = _faker.Name.FirstName(), Surname = _faker.Name.LastName() },
            new UserItem { UserId = _faker.Random.Int(), GivenName = _faker.Name.FirstName(), Surname = _faker.Name.LastName() }
        };

        _mockPcssAuthorizationServiceClient
            .Setup(client => client.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _authorizationService.GetUsers();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(users.Count, result.Count);
        Assert.Equal(users.First().UserId, result.First().UserId);
        _mockPcssAuthorizationServiceClient.Verify(client => client.GetUsersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPcssUserRoleNames_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userId = _faker.Random.Int();
        _mockPcssAuthorizationServiceClient
            .Setup(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserItem)null);

        // Act
        var result = await _authorizationService.GetPcssUserRoleNames(userId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("User not found or has no roles.", result.Errors.First());
        _mockPcssAuthorizationServiceClient.Verify(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPcssUserRoleNames_ShouldReturnFailure_WhenUserHasNoRoles()
    {
        // Arrange
        var userId = _faker.Random.Int();
        var user = new UserItem
        {
            UserId = userId,
            Roles = new List<EffectiveRoleItem>() // Empty roles
        };

        _mockPcssAuthorizationServiceClient
            .Setup(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authorizationService.GetPcssUserRoleNames(userId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("User not found or has no roles.", result.Errors.First());
        _mockPcssAuthorizationServiceClient.Verify(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
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
            Roles = new List<EffectiveRoleItem>
            {
                new EffectiveRoleItem
                {
                    Name = roleName,
                    EffectiveDate = DateTime.Now.AddDays(-1).ToString("o"),
                    ExpiryDate = DateTime.Now.AddDays(30).ToString("o")
                }
            }
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