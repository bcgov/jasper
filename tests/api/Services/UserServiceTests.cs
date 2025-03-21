using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Moq;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;
public class UserServiceTests
{
    private readonly Faker _faker;
    private readonly Mock<IRepositoryBase<User>> _mockUserRepo;
    private readonly Mock<IRepositoryBase<Group>> _mockGroupRepo;
    private readonly Mock<IRepositoryBase<Role>> _mockRoleRepo;
    private readonly Mock<IPermissionRepository> _mockPermissionRepo;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _faker = new Faker();

        // Setup Cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // IMapper setup
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<User, UserDto>();
            cfg.CreateMap<UserDto, User>();
        });
        var mapper = config.CreateMapper();

        // ILogger setup
        var logger = new Mock<ILogger<UserService>>();

        _mockUserRepo = new Mock<IRepositoryBase<User>>();
        _mockGroupRepo = new Mock<IRepositoryBase<Group>>();
        _mockRoleRepo = new Mock<IRepositoryBase<Role>>();
        _mockPermissionRepo = new Mock<IPermissionRepository>();

        _userService = new UserService(
            cachingService,
            mapper,
            logger.Object,
            _mockUserRepo.Object,
            _mockGroupRepo.Object,
            _mockRoleRepo.Object,
            _mockPermissionRepo.Object);
    }

    [Fact]
    public async Task ValidateUserDto_ShouldReturnFailure_WhenUserIsNullOnEdit()
    {
        var dto = new UserDto
        {
            Id = ObjectId.GenerateNewId().ToString()
        };

        _mockUserRepo.Setup(g => g.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((User)null);

        var result = await _userService.ValidateAsync(dto, true);

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("User ID is not found.", result.Errors[0]);
    }

    [Fact]
    public async Task ValidateUserDto_ShouldReturnFailure_WhenGroupIdsAreInvalid()
    {
        var dto = new UserDto
        {
            Id = ObjectId.GenerateNewId().ToString(),
            GroupIds = [ObjectId.GenerateNewId().ToString()]
        };

        _mockUserRepo.Setup(g => g.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(
            new User
            {
                FirstName = _faker.Person.FirstName,
                LastName = _faker.Person.LastName,
                Email = _faker.Person.Email,
            });
        _mockGroupRepo
            .Setup(g => g.GetAllAsync())
            .ReturnsAsync([
                new Group
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph()
                }
            ]);

        var result = await _userService.ValidateAsync(dto, false);

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Found one or more invalid group IDs.", result.Errors[0]);
    }

    [Fact]
    public async Task ValidateUserDto_ShouldReturnSuccess()
    {
        var dto = new UserDto
        {
            Id = ObjectId.GenerateNewId().ToString(),
            GroupIds = [ObjectId.GenerateNewId().ToString()]
        };

        _mockUserRepo.Setup(g => g.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(
            new User
            {
                FirstName = _faker.Person.FirstName,
                LastName = _faker.Person.LastName,
                Email = _faker.Person.Email,
            });
        _mockGroupRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([
                new Group {
                    Id = dto.GroupIds.First(),
                    Name = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph()
                }
            ]);

        var result = await _userService.ValidateAsync(dto, false);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnSuccess()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockUserRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync(new User
        {
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Email = _faker.Person.Email,
        });
        _mockUserRepo.Setup(r => r.DeleteAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _userService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockUserRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockUserRepo.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnFailure_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockUserRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync((User)null);
        _mockUserRepo.Setup(r => r.DeleteAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _userService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Entity not found.", result.Errors.First());
        _mockUserRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockUserRepo.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldReturnFailure_WhenExceptionIsThrown()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockUserRepo.Setup(r => r.GetByIdAsync(fakeId)).Throws<InvalidOperationException>();
        _mockUserRepo.Setup(r => r.DeleteAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _userService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Error when deleting data.", result.Errors.First());
        _mockUserRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockUserRepo.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ValidateUserDto_ShouldReturnFailure_WhenEmailIsAlreadyUsedOnEdit()
    {
        var dto = new UserDto
        {
            Id = ObjectId.GenerateNewId().ToString()
        };

        _mockUserRepo.Setup(g => g.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new User
        {
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Email = dto.Email,
        });
        _mockUserRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([new User
            {
                FirstName = _faker.Person.FirstName,
                LastName = _faker.Person.LastName,
                Email = dto.Email,
            }]);

        var result = await _userService.ValidateAsync(dto, true);

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Email address is already used.", result.Errors[0]);
    }

    [Fact]
    public async Task ValidateUserDto_ShouldReturnFailure_WhenEmailIsAlreadyUsedOnAdd()
    {
        var dto = new UserDto
        {
            Id = ObjectId.GenerateNewId().ToString()
        };

        var mockUser = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Email = dto.Email,
        };

        _mockUserRepo.Setup(g => g.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(mockUser);
        _mockUserRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([mockUser]);

        var result = await _userService.ValidateAsync(dto, false);

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Email address is already used.", result.Errors[0]);
    }

    [Fact]
    public async Task GetWithPermissions_ShouldReturnNull_WhenEmailDoesNotExist()
    {
        _mockUserRepo
            .Setup(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((IEnumerable<User>)null);

        var result = await _userService.GetWithPermissionsAsync(_faker.Internet.Email());

        Assert.Null(result);
        _mockUserRepo.Verify(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once());
    }

    [Fact]
    public async Task GetWithPermissions_ShouldReturnNull_WhenEmailDoesNotExistAndReturnsEmpty()
    {
        _mockUserRepo
            .Setup(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((IEnumerable<User>)[]);

        var result = await _userService.GetWithPermissionsAsync(_faker.Internet.Email());

        Assert.Null(result);
        _mockUserRepo.Verify(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once());
    }

    [Fact]
    public async Task GetWithPermissions_ShouldReturnUserWithoutPermissions_WhenUserGroupIsEmpty()
    {
        _mockUserRepo
            .Setup(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([
                new() {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = _faker.Person.FirstName,
                    LastName = _faker.Person.LastName,
                    Email = _faker.Internet.Email()
                }
            ]);

        var result = await _userService.GetWithPermissionsAsync(_faker.Internet.Email());

        Assert.NotNull(result);
        _mockUserRepo.Verify(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once());
    }

    [Fact]
    public async Task GetWithPermissions_ShouldReturnUserWithoutPermissions_WhenRoleIdsIsEmpty()
    {
        _mockUserRepo
            .Setup(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([
                new() {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = _faker.Person.FirstName,
                    LastName = _faker.Person.LastName,
                    Email = _faker.Internet.Email(),
                    GroupIds = [ObjectId.GenerateNewId().ToString()]
                }
            ]);
        _mockGroupRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()))
            .ReturnsAsync((IEnumerable<Group>)[]);

        var result = await _userService.GetWithPermissionsAsync(_faker.Internet.Email());

        Assert.NotNull(result);
        _mockUserRepo.Verify(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once());
        _mockGroupRepo.Verify(g => g.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()), Times.Once());
    }

    [Fact]
    public async Task GetWithPermissions_ShouldReturnUserWithoutPermissions_WhenPermissionIdsIsEmpty()
    {
        _mockUserRepo
            .Setup(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([
                new() {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = _faker.Person.FirstName,
                    LastName = _faker.Person.LastName,
                    Email = _faker.Internet.Email(),
                    GroupIds = [ObjectId.GenerateNewId().ToString()]
                }
            ]);
        _mockGroupRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()))
            .ReturnsAsync([
                new() {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph(),
                    RoleIds = [ObjectId.GenerateNewId().ToString()]
                }
            ]);
        _mockRoleRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([
                new() {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph(),
                }
            ]);

        var result = await _userService.GetWithPermissionsAsync(_faker.Internet.Email());

        Assert.NotNull(result);
        _mockUserRepo.Verify(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once());
        _mockGroupRepo.Verify(g => g.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()), Times.Once());
        _mockRoleRepo.Verify(g => g.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()), Times.Once());
    }

    [Fact]
    public async Task GetWithPermissions_ShouldReturnUserPermissions()
    {
        _mockUserRepo
            .Setup(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([
                new() {
                    Id = ObjectId.GenerateNewId().ToString(),
                    FirstName = _faker.Person.FirstName,
                    LastName = _faker.Person.LastName,
                    Email = _faker.Internet.Email(),
                    GroupIds = [ObjectId.GenerateNewId().ToString()]
                }
            ]);
        _mockGroupRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()))
            .ReturnsAsync([
                new() {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph(),
                    RoleIds = [ObjectId.GenerateNewId().ToString()]
                }
            ]);
        _mockRoleRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync([
                new() {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph(),
                    PermissionIds = [ObjectId.GenerateNewId().ToString()]
                }
            ]);
        _mockPermissionRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync([
                new() {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = _faker.Lorem.Word(),
                    Code = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph(),
                }
            ]);

        var result = await _userService.GetWithPermissionsAsync(_faker.Internet.Email());

        Assert.NotNull(result);
        _mockUserRepo.Verify(u => u.FindAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once());
        _mockGroupRepo.Verify(g => g.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()), Times.Once());
        _mockRoleRepo.Verify(g => g.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()), Times.Once());
        _mockPermissionRepo.Verify(g => g.FindAsync(It.IsAny<Expression<Func<Permission, bool>>>()), Times.Once());
    }
}
