using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Models.UserManagement;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;
public class RoleServiceTests
{
    private readonly Faker _faker;
    private readonly Mock<IRoleRepository> _mockRoleRepo;
    private readonly Mock<IPermissionRepository> _mockPermissionRepo;
    private readonly RoleService _roleService;
    public RoleServiceTests()
    {
        _faker = new Faker();

        // Setup Cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // IMapper setup
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Role, RoleDto>();
            cfg.CreateMap<RoleCreateDto, Role>();
            cfg.CreateMap<RoleUpdateDto, Role>();
        });
        var mapper = config.CreateMapper();

        // ILogger setup
        var logger = new Mock<ILogger<RoleService>>();

        _mockRoleRepo = new Mock<IRoleRepository>();
        _mockPermissionRepo = new Mock<IPermissionRepository>();

        _roleService = new RoleService(
            cachingService,
            mapper,
            logger.Object,
            _mockRoleRepo.Object,
            _mockPermissionRepo.Object);
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnMappedRoles()
    {
        var mockRole = new Role
        {
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph(),
        };

        _mockRoleRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([mockRole]);

        var result = await _roleService.GetRolesAsync();

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.All(result, dto =>
        {
            Assert.Equal(mockRole.Name, dto.Name);
        });
        _mockRoleRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetRoleByIdAsync_ShouldReturnMappedRole()
    {
        var fakeId = _faker.Random.AlphaNumeric(5);
        var mockRole = new Role
        {
            Id = fakeId,
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph(),
        };

        _mockRoleRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync(mockRole);

        var result = await _roleService.GetRoleByIdAsync(fakeId);

        Assert.NotNull(result);
        Assert.Equal(mockRole.Id, result.Id);
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldReturnSuccess()
    {
        var mockRole = new RoleCreateDto();
        _mockRoleRepo.Setup(r => r.AddAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

        var result = await _roleService.CreateRoleAsync(mockRole);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRoleRepo.Verify(r => r.AddAsync(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldReturnFailure_WhenExceptionOccurs()
    {
        var mockRole = new RoleCreateDto();
        _mockRoleRepo.Setup(r => r.AddAsync(It.IsAny<Role>())).Throws<InvalidOperationException>();

        var result = await _roleService.CreateRoleAsync(mockRole);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Error when creating role.", result.Errors.First());
        _mockRoleRepo.Verify(r => r.AddAsync(It.IsAny<Role>()), Times.Once);
    }


    [Fact]
    public async Task ValidateRoleCreateDtoAsync_ShouldReturnFailure_WhenPermissionIdIsInvalid()
    {
        var fakeInvalidId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleCreateDto
        {
            PermissionIds = [fakeInvalidId]
        };

        _mockPermissionRepo
            .Setup(p => p.GetActivePermissionsAsync())
            .ReturnsAsync([new Permission
            {
                Id = _faker.Random.AlphaNumeric(10),
                Name = _faker.Random.AlphaNumeric(10),
                Code = _faker.Random.AlphaNumeric(5),
                Description = _faker.Lorem.Paragraph()
            }]);

        var result = await _roleService.ValidateRoleCreateDtoAsync(mockRole);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Found one or more invalid permission IDs.", result.Errors.First());
        _mockPermissionRepo.Verify(p => p.GetActivePermissionsAsync(), Times.Once);
    }

    [Fact]
    public async Task ValidateRoleCreateDtoAsync_ShouldReturnSuccess_WhenPermissionIdIsValid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleCreateDto
        {
            PermissionIds = [fakeId]
        };

        _mockPermissionRepo
            .Setup(p => p.GetActivePermissionsAsync())
            .ReturnsAsync([new Permission
            {
                Id = fakeId,
                Name = _faker.Random.AlphaNumeric(10),
                Code = _faker.Random.AlphaNumeric(5),
                Description = _faker.Lorem.Paragraph()
            }]);

        var result = await _roleService.ValidateRoleCreateDtoAsync(mockRole);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockPermissionRepo.Verify(p => p.GetActivePermissionsAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldReturnSuccess()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleUpdateDto();
        _mockRoleRepo.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

        var result = await _roleService.UpdateRoleAsync(fakeId, mockRole);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRoleRepo.Verify(r => r.UpdateAsync(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldReturnFailure_WhenExceptionOccurs()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleUpdateDto();
        _mockRoleRepo.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Throws<InvalidOperationException>();

        var result = await _roleService.UpdateRoleAsync(fakeId, mockRole);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Error when updating role.", result.Errors.First());
        _mockRoleRepo.Verify(r => r.UpdateAsync(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task ValidateRoleUpdateDtoAsync_ShouldReturnFailure_WhenRoleIdIsNotFoundAndPermissionIdIsValid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleUpdateDto
        {
            Id = fakeId,
            PermissionIds = [fakeId]
        };

        _mockRoleRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync((Role)null);
        _mockPermissionRepo
            .Setup(p => p.GetActivePermissionsAsync())
            .ReturnsAsync([new Permission
            {
                Id = _faker.Random.AlphaNumeric(10),
                Name = _faker.Random.AlphaNumeric(10),
                Code = _faker.Random.AlphaNumeric(5),
                Description = _faker.Lorem.Paragraph()
            }]);

        var result = await _roleService.ValidateRoleUpdateDtoAsync(mockRole);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("Role ID is not found.", result.Errors[0]);
        Assert.Equal("Found one or more invalid permission IDs.", result.Errors[1]);
        _mockPermissionRepo.Verify(p => p.GetActivePermissionsAsync(), Times.Once);
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
    }

    [Fact]
    public async Task ValidateRoleUpdateDtoAsync_ShouldReturnSuccess_WhenPermissionIdIsValid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleUpdateDto
        {
            Id = _faker.Random.AlphaNumeric(10),
            PermissionIds = [fakeId]
        };

        _mockRoleRepo.Setup(r => r.GetByIdAsync(mockRole.Id)).ReturnsAsync(new Role
        {
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph(),
        });
        _mockPermissionRepo
            .Setup(p => p.GetActivePermissionsAsync())
            .ReturnsAsync([new Permission
            {
                Id = fakeId,
                Name = _faker.Random.AlphaNumeric(10),
                Code = _faker.Random.AlphaNumeric(5),
                Description = _faker.Lorem.Paragraph()
            }]);

        var result = await _roleService.ValidateRoleUpdateDtoAsync(mockRole);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockPermissionRepo.Verify(p => p.GetActivePermissionsAsync(), Times.Once);
        _mockRoleRepo.Verify(r => r.GetByIdAsync(mockRole.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldReturnSuccess()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockRoleRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync(new Role
        {
            Name = fakeId,
            Description = _faker.Lorem.Paragraph(),
        });
        _mockRoleRepo.Setup(r => r.DeleteAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

        var result = await _roleService.DeleteRoleAsync(fakeId);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockRoleRepo.Verify(r => r.DeleteAsync(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldReturnFailure_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockRoleRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync((Role)null);
        _mockRoleRepo.Setup(r => r.DeleteAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

        var result = await _roleService.DeleteRoleAsync(fakeId);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Role ID not found.", result.Errors.First());
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockRoleRepo.Verify(r => r.DeleteAsync(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldReturnFailure_WhenExceptionIsThrown()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockRoleRepo.Setup(r => r.GetByIdAsync(fakeId)).Throws<InvalidOperationException>();
        _mockRoleRepo.Setup(r => r.DeleteAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

        var result = await _roleService.DeleteRoleAsync(fakeId);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Error when deleting role.", result.Errors.First());
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockRoleRepo.Verify(r => r.DeleteAsync(It.IsAny<Role>()), Times.Never);
    }
}
