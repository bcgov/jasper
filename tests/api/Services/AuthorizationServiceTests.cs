using Bogus;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.AuthorizationServices;
using Scv.Api.Infrastructure;
using Scv.Api.Models.AccessControlManagement;
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
    private readonly Mock<AuthorizationServicesClient> _mockPcssAuthorizationServiceClient;
    private readonly AuthorizationService _authorizationService;

    public AuthorizationServiceTests()
    {
        _faker = new Faker();

        // Mock IConfiguration
        var mockConfig = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        var mockLogger = new Mock<ILogger<AuthorizationService>>();
        mockSection.Setup(s => s.Value).Returns("60"); // Cache duration in minutes
        mockConfig.Setup(c => c.GetSection("Caching:UserExpiryMinutes")).Returns(mockSection.Object);

        // Setup Cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // Mock ILogger
        var logger = new Mock<ILogger<AuthorizationService>>();

        _mockPcssAuthorizationServiceClient = new Mock<AuthorizationServicesClient>(MockBehavior.Strict, new object[] { null });

        _authorizationService = new AuthorizationService(
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
    public async Task GetPcssUserRoleNames_ShouldReturnSuccess_WhenGroupsFound()
    {
        // Arrange
        var userId = _faker.Random.Int();
        var roleName = _faker.Random.Word();
        var user = new UserItem
        {
            UserId = userId,
            Roles = new List<EffectiveRoleItem>
            {
                new EffectiveRoleItem { Name = roleName }
            }
        };

        var groups = new List<GroupDto>
        {
            new GroupDto { Name = _faker.Company.CompanyName(), Description = _faker.Lorem.Paragraph() }
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
        _mockPcssAuthorizationServiceClient.Verify(client => client.GetUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}