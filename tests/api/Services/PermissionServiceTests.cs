using System;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Models.AccessControlManagement;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;

public class PermissionServiceTests
{
    private readonly Faker _faker;
    private readonly Mock<IPermissionRepository> _mockPermissionRepo;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests()
    {
        _faker = new Faker();

        // Setup Cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new AccessControlManagementMapping());
        var mapper = new Mapper(config);

        // ILogger setup
        var logger = new Mock<ILogger<PermissionService>>();

        _mockPermissionRepo = new Mock<IPermissionRepository>();
        _permissionService = new PermissionService(cachingService, mapper, logger.Object, _mockPermissionRepo.Object);
    }

    [Fact]
    public async Task GetPermissionsAsync_ShouldReturnMappedPermissions()
    {
        var count = _faker.Random.Number(1, Permission.ALL_PERMISIONS.Count);
        var permissions = Permission.ALL_PERMISIONS.Take(count);

        _mockPermissionRepo.Setup(r => r.GetActivePermissionsAsync()).ReturnsAsync(permissions);

        var result = await _permissionService.GetAllAsync();

        Assert.NotNull(result);
        Assert.Equal(count, result.Count);
        Assert.All(result, dto =>
        {
            var expected = permissions.First(p => p.Id == dto.Id);
            Assert.Equal(expected.Name, dto.Name);
        });
        _mockPermissionRepo.Verify(r => r.GetActivePermissionsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_ShouldReturnMappedPermission()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var randomIndex = _faker.Random.Number(0, Permission.ALL_PERMISIONS.Count - 1);
        var permission = Permission.ALL_PERMISIONS[randomIndex];
        _mockPermissionRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync(permission);

        var result = await _permissionService.GetByIdAsync(fakeId);

        Assert.NotNull(result);
        Assert.Equal(permission.Id, result.Id);
        _mockPermissionRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
    }

    [Fact]
    public async Task UpdatePermissionAsync_ShouldUpdateAndReturnMappedPermission()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var randomIndex = _faker.Random.Number(0, Permission.ALL_PERMISIONS.Count - 1);
        var permission = Permission.ALL_PERMISIONS[randomIndex];
        var updateDto = new PermissionDto
        {
            Id = fakeId,
            Description = _faker.Lorem.Paragraph(),
            IsActive = _faker.Random.Bool()
        };
        _mockPermissionRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync(permission);
        _mockPermissionRepo.Setup(r => r.UpdateAsync(It.IsAny<Permission>())).Returns(Task.CompletedTask);

        var result = await _permissionService.UpdateAsync(updateDto);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(updateDto.Description, result.Payload.Description);
        Assert.Equal(updateDto.IsActive, result.Payload.IsActive);
        _mockPermissionRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockPermissionRepo.Verify(r => r.UpdateAsync(It.IsAny<Permission>()), Times.Once);
    }

    [Fact]
    public async Task ValidatePermissionDtoAsync_ShouldReturnFailedResult_WhenPermissionIdIsNull()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockPermissionRepo
            .Setup(p => p.GetByIdAsync(fakeId))
            .ReturnsAsync((Permission)null);

        var result = await _permissionService.ValidateAsync(new PermissionDto
        {
            Id = fakeId,
        });

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Permission ID is not found.", result.Errors.First());
        _mockPermissionRepo.Verify(p => p.GetByIdAsync(fakeId), Times.Once);
    }

    [Fact]
    public async Task ValidatePermissionDtoAsync_ShouldReturnSuccessResult_WhenPermissionIdIsValid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockPermissionRepo
            .Setup(p => p.GetByIdAsync(fakeId))
            .ReturnsAsync(new Permission
            {
                Name = _faker.Random.AlphaNumeric(10),
                Code = _faker.Random.AlphaNumeric(5),
                Description = _faker.Lorem.Paragraph()
            });

        var result = await _permissionService.ValidateAsync(new PermissionDto
        {
            Id = fakeId,
        });

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockPermissionRepo.Verify(p => p.GetByIdAsync(fakeId), Times.Once);
    }
}