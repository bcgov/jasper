using System;
using System.Linq;
using System.Linq.Expressions;
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
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AccessControlManagement;
using Xunit;

namespace tests.api.Services;

public class RoleServiceTests
{
    private readonly Faker _faker;
    private readonly Mock<IRepositoryBase<Role>> _mockRoleRepo;
    private readonly Mock<IPermissionRepository> _mockPermissionRepo;
    private readonly Mock<IRepositoryBase<Group>> _mockGroupRepo;
    private readonly Mock<IRepositoryBase<RoleAlias>> _mockRoleAliasRepo;
    private readonly RoleService _roleService;
    public RoleServiceTests()
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
        var logger = new Mock<ILogger<RoleService>>();

        _mockRoleRepo = new Mock<IRepositoryBase<Role>>();
        _mockPermissionRepo = new Mock<IPermissionRepository>();
        _mockGroupRepo = new Mock<IRepositoryBase<Group>>();
        _mockRoleAliasRepo = new Mock<IRepositoryBase<RoleAlias>>();

        _roleService = new RoleService(
            cachingService,
            mapper,
            logger.Object,
            _mockRoleRepo.Object,
            _mockPermissionRepo.Object,
            _mockGroupRepo.Object,
            _mockRoleAliasRepo.Object);
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

        var result = await _roleService.GetAllAsync();

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

        var result = await _roleService.GetByIdAsync(fakeId);

        Assert.NotNull(result);
        Assert.Equal(mockRole.Id, result.Id);
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldReturnSuccess()
    {
        var mockRole = new RoleDto();
        _mockRoleRepo.Setup(r => r.AddAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

        var result = await _roleService.AddAsync(mockRole);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRoleRepo.Verify(r => r.AddAsync(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldReturnFailure_WhenExceptionOccurs()
    {
        var mockRole = new RoleDto();
        _mockRoleRepo.Setup(r => r.AddAsync(It.IsAny<Role>())).Throws<InvalidOperationException>();

        var result = await _roleService.AddAsync(mockRole);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Error when adding data.", result.Errors[0]);
        _mockRoleRepo.Verify(r => r.AddAsync(It.IsAny<Role>()), Times.Once);
    }


    [Fact]
    public async Task ValidateRoleDtoAsync_ShouldReturnFailure_WhenPermissionIdIsInvalid()
    {
        var fakeInvalidId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleDto
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

        var result = await _roleService.ValidateAsync(mockRole);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Found one or more invalid permission IDs.", result.Errors[0]);
        _mockPermissionRepo.Verify(p => p.GetActivePermissionsAsync(), Times.Once);
    }

    [Fact]
    public async Task ValidateRoleDtoAsync_ShouldReturnSuccess_WhenPermissionIdIsValidOnCreate()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleDto
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

        var result = await _roleService.ValidateAsync(mockRole);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockPermissionRepo.Verify(p => p.GetActivePermissionsAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldReturnSuccess()
    {
        var mockRole = new RoleDto();
        _mockRoleRepo.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

        var result = await _roleService.UpdateAsync(mockRole);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRoleRepo.Verify(r => r.UpdateAsync(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleAsync_ShouldReturnFailure_WhenExceptionOccurs()
    {
        var mockRole = new RoleDto();
        _mockRoleRepo.Setup(r => r.UpdateAsync(It.IsAny<Role>())).Throws<InvalidOperationException>();

        var result = await _roleService.UpdateAsync(mockRole);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Error when updating data.", result.Errors[0]);
        _mockRoleRepo.Verify(r => r.UpdateAsync(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task ValidateRoleDtoAsync_ShouldReturnFailure_WhenRoleIdIsNotFoundAndPermissionIdIsValidAndEdit()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleDto
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

        var result = await _roleService.ValidateAsync(mockRole, true);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal("Role ID is not found.", result.Errors[0]);
        Assert.Equal("Found one or more invalid permission IDs.", result.Errors[1]);
        _mockPermissionRepo.Verify(p => p.GetActivePermissionsAsync(), Times.Once);
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
    }

    [Fact]
    public async Task ValidateRoleDtoAsync_ShouldReturnSuccess_WhenPermissionIdIsValid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        var mockRole = new RoleDto
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

        var result = await _roleService.ValidateAsync(mockRole, true);

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

        var result = await _roleService.DeleteAsync(fakeId);

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

        var result = await _roleService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Entity not found.", result.Errors[0]);
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockRoleRepo.Verify(r => r.DeleteAsync(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldReturnFailure_WhenExceptionIsThrown()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockRoleRepo.Setup(r => r.GetByIdAsync(fakeId)).Throws<InvalidOperationException>();
        _mockRoleRepo.Setup(r => r.DeleteAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);

        var result = await _roleService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Error when deleting data.", result.Errors[0]);
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockRoleRepo.Verify(r => r.DeleteAsync(It.IsAny<Role>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRoleAsync_ShouldUpdateAffectedGroups()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync(new Role
            {
                Name = _faker.Random.AlphaNumeric(10),
                Description = _faker.Lorem.Paragraph(),
            });
        _mockRoleRepo.Setup(r => r.DeleteAsync(It.IsAny<Role>())).Returns(Task.CompletedTask);
        _mockGroupRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()))
            .ReturnsAsync([
                new()
                {
                    Name = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph(),
                    RoleIds = [fakeId]
                }
            ]);
        _mockGroupRepo.Setup(g => g.UpdateAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);

        var result = await _roleService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockRoleRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockRoleRepo.Verify(r => r.DeleteAsync(It.IsAny<Role>()), Times.Once);
        _mockGroupRepo.Verify(g => g.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()), Times.Once);
        _mockGroupRepo.Verify(g => g.UpdateAsync(It.IsAny<Group>()), Times.Once);
    }

    [Fact]
    public async Task GetRolesByAliases_ShouldReturnMappedRoles_WhenAliasesMatch()
    {
        var fakeAlias = _faker.Random.AlphaNumeric(10);
        var fakeRoleId = _faker.Random.AlphaNumeric(10);
        var fakeRoleName = _faker.Random.AlphaNumeric(10);

        _mockRoleAliasRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RoleAlias, bool>>>()))
            .ReturnsAsync([new RoleAlias { Name = fakeAlias, RoleId = fakeRoleId }]);

        _mockRoleRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([new Role { Id = fakeRoleId, Name = fakeRoleName, Description = _faker.Lorem.Paragraph() }]);

        var result = await _roleService.GetRolesByAliases([fakeAlias]);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Single(result.Payload);
        Assert.Equal(fakeRoleId, result.Payload.First().Id);
        _mockRoleAliasRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<RoleAlias, bool>>>()), Times.Once);
        _mockRoleRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetRolesByAliases_ShouldReturnFailure_WhenNoAliasesMatch()
    {
        var fakeAlias = _faker.Random.AlphaNumeric(10);

        _mockRoleAliasRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RoleAlias, bool>>>()))
            .ReturnsAsync([]);

        var result = await _roleService.GetRolesByAliases([fakeAlias]);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Role alias not found.", result.Errors[0]);
        _mockRoleAliasRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<RoleAlias, bool>>>()), Times.Once);
        _mockRoleRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetRolesByAliases_ShouldReturnEmptyPayload_WhenAliasHasNoMatchingRole()
    {
        var fakeAlias = _faker.Random.AlphaNumeric(10);
        var fakeRoleId = _faker.Random.AlphaNumeric(10);

        _mockRoleAliasRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RoleAlias, bool>>>()))
            .ReturnsAsync([new RoleAlias { Name = fakeAlias, RoleId = fakeRoleId }]);

        _mockRoleRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([]);

        var result = await _roleService.GetRolesByAliases([fakeAlias]);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Payload);
        _mockRoleAliasRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<RoleAlias, bool>>>()), Times.Once);
        _mockRoleRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetRolesByAliases_ShouldDeduplicateRoles_WhenMultipleAliasesMapToSameRole()
    {
        var fakeAlias1 = _faker.Random.AlphaNumeric(10);
        var fakeAlias2 = _faker.Random.AlphaNumeric(10);
        var fakeRoleId = _faker.Random.AlphaNumeric(10);
        var fakeRoleName = _faker.Random.AlphaNumeric(10);

        _mockRoleAliasRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RoleAlias, bool>>>()))
            .ReturnsAsync([
                new RoleAlias { Name = fakeAlias1, RoleId = fakeRoleId },
                new RoleAlias { Name = fakeAlias2, RoleId = fakeRoleId }
            ]);

        _mockRoleRepo
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([new Role { Id = fakeRoleId, Name = fakeRoleName, Description = _faker.Lorem.Paragraph() }]);

        var result = await _roleService.GetRolesByAliases([fakeAlias1, fakeAlias2]);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Single(result.Payload);
        _mockRoleAliasRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<RoleAlias, bool>>>()), Times.Once);
        _mockRoleRepo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()), Times.Once);
    }
}
