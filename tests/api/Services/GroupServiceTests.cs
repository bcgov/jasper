using System;
using System.Collections.Generic;
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
using MongoDB.Bson;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Services;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;
public class GroupServiceTests
{
    private readonly Faker _faker;
    private readonly Mock<IRepositoryBase<Group>> _mockGroupRepo;
    private readonly Mock<IRepositoryBase<GroupAlias>> _mockGroupAliasRepo;
    private readonly Mock<IRepositoryBase<Role>> _mockRoleRepo;
    private readonly Mock<IRepositoryBase<User>> _mockUserRepo;
    private readonly GroupService _groupService;

    public GroupServiceTests()
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
        var logger = new Mock<ILogger<GroupService>>();

        _mockGroupRepo = new Mock<IRepositoryBase<Group>>();
        _mockGroupAliasRepo = new Mock<IRepositoryBase<GroupAlias>>();
        _mockRoleRepo = new Mock<IRepositoryBase<Role>>();
        _mockUserRepo = new Mock<IRepositoryBase<User>>();

        _groupService = new GroupService(
            cachingService,
            mapper,
            logger.Object,
            _mockGroupRepo.Object,
            _mockRoleRepo.Object,
            _mockUserRepo.Object,
            _mockGroupAliasRepo.Object);
    }

    [Fact]
    public async Task ValidateGroupDto_ShouldReturnFailure_WhenGroupIsNullOnEdit()
    {
        var dto = new GroupDto
        {
            Id = ObjectId.GenerateNewId().ToString()
        };

        _mockGroupRepo.Setup(g => g.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Group)null);

        var result = await _groupService.ValidateAsync(dto, true);

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Group ID is not found.", result.Errors[0]);
    }

    [Fact]
    public async Task ValidateGroupDto_ShouldReturnFailure_WhenRoleIdsAreInvalid()
    {
        var dto = new GroupDto
        {
            Id = ObjectId.GenerateNewId().ToString(),
            RoleIds = [ObjectId.GenerateNewId().ToString()]
        };

        _mockGroupRepo.Setup(g => g.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(
            new Group
            {
                Name = _faker.Lorem.Word(),
                Description = _faker.Lorem.Paragraph()
            });
        _mockRoleRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([
                new Role {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph()
                }
            ]);

        var result = await _groupService.ValidateAsync(dto, false);

        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Found one or more invalid role IDs.", result.Errors[0]);
    }

    [Fact]
    public async Task ValidateGroupDto_ShouldReturnSuccess()
    {
        var dto = new GroupDto
        {
            Id = ObjectId.GenerateNewId().ToString(),
            RoleIds = [ObjectId.GenerateNewId().ToString()]
        };

        _mockGroupRepo.Setup(g => g.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(
            new Group
            {
                Name = _faker.Lorem.Word(),
                Description = _faker.Lorem.Paragraph()
            });
        _mockRoleRepo
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync([
                new Role {
                    Id = dto.RoleIds.First(),
                    Name = _faker.Lorem.Word(),
                    Description = _faker.Lorem.Paragraph()
                }
            ]);

        var result = await _groupService.ValidateAsync(dto, false);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldReturnSuccess()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockGroupRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync(new Group
        {
            Name = fakeId,
            Description = _faker.Lorem.Paragraph(),
        });
        _mockGroupRepo.Setup(r => r.DeleteAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);

        var result = await _groupService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockGroupRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockGroupRepo.Verify(r => r.DeleteAsync(It.IsAny<Group>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldReturnFailure_WhenIdIsInvalid()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockGroupRepo.Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync((Group)null);
        _mockGroupRepo.Setup(r => r.DeleteAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);

        var result = await _groupService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Entity not found.", result.Errors.First());
        _mockGroupRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockGroupRepo.Verify(r => r.DeleteAsync(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldReturnFailure_WhenExceptionIsThrown()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);

        _mockGroupRepo.Setup(r => r.GetByIdAsync(fakeId)).Throws<InvalidOperationException>();
        _mockGroupRepo.Setup(r => r.DeleteAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);

        var result = await _groupService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Error when deleting data.", result.Errors.First());
        _mockGroupRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockGroupRepo.Verify(r => r.DeleteAsync(It.IsAny<Group>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldUpdateAffectedUsers()
    {
        var fakeId = _faker.Random.AlphaNumeric(10);
        _mockGroupRepo
            .Setup(r => r.GetByIdAsync(fakeId)).ReturnsAsync(new Group
            {
                Name = _faker.Random.AlphaNumeric(10),
                Description = _faker.Lorem.Paragraph(),
            });
        _mockGroupRepo.Setup(r => r.DeleteAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);
        _mockUserRepo
            .Setup(g => g.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([
                new()
                {
                    Email = _faker.Person.Email,
                    FirstName = _faker.Person.FirstName,
                    LastName = _faker.Person.LastName,
                    GroupIds = [fakeId]
                }
            ]);
        _mockGroupRepo.Setup(g => g.UpdateAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);

        var result = await _groupService.DeleteAsync(fakeId);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        _mockGroupRepo.Verify(r => r.GetByIdAsync(fakeId), Times.Once);
        _mockGroupRepo.Verify(r => r.DeleteAsync(It.IsAny<Group>()), Times.Once);
        _mockUserRepo.Verify(g => g.FindAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
        _mockUserRepo.Verify(g => g.UpdateAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task GetGroupsByAliases_ShouldReturnFailure_WhenAliasesNotFound()
    {
        // Arrange
        var aliases = new List<string> { _faker.Random.Word() };
        _mockGroupAliasRepo.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<GroupAlias, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<GroupAlias>());

        // Act
        var result = await _groupService.GetGroupsByAliases(aliases);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("Group alias not found.", result.Errors.First());
    }

    [Fact]
    public async Task GetGroupsByAliases_ShouldSucceed_WhenGroupIdsMismatch()
    {
        // Arrange
        var aliases = new List<string> { _faker.Random.Word() };
        var groupAlias = new GroupAlias { Name = aliases.First(), GroupId = ObjectId.GenerateNewId().ToString() };
        _mockGroupAliasRepo.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<GroupAlias, bool>>>()))
            .ReturnsAsync(new[] { groupAlias });

        _mockGroupRepo.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()))
            .ReturnsAsync(Enumerable.Empty<Group>());

        // Act
        var result = await _groupService.GetGroupsByAliases(aliases);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task GetGroupsByAliases_ShouldReturnSuccess_WhenGroupsFound()
    {
        // Arrange
        var aliases = new List<string> { _faker.Random.Word() };
        var groupId = ObjectId.GenerateNewId().ToString();
        var groupAlias = new GroupAlias { Name = aliases.First(), GroupId = groupId };
        var group = new Group { Id = groupId, Name = _faker.Company.CompanyName(), Description = _faker.Lorem.Paragraph() };

        _mockGroupAliasRepo.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<GroupAlias, bool>>>()))
            .ReturnsAsync(new[] { groupAlias });

        _mockGroupRepo.Setup(repo => repo.FindAsync(It.IsAny<Expression<Func<Group, bool>>>()))
            .ReturnsAsync(new[] { group });

        // Act
        var result = await _groupService.GetGroupsByAliases(aliases);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Single(result.Payload);
        Assert.Equal(group.Name, result.Payload.First().Name);
        Assert.Equal(group.Description, result.Payload.First().Description);
    }
}
