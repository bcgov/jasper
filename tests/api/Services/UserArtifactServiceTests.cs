using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Db.Contants;
using Scv.Db.Repositories;
using Scv.Models.UserArtifacts;
using Xunit;

namespace tests.api.Services;

public class UserArtifactServiceTests
{
    private const string CurrentUserId = "user-1";

    private readonly Mock<IRepositoryBase<UserArtifact>> _mockRepo;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly IMapper _mapper;
    private readonly UserArtifactService _service;

    public UserArtifactServiceTests()
    {
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var config = new TypeAdapterConfig();
        config.Apply(new UserArtifactMapping());
        _mapper = new Mapper(config);

        _mockRepo = new Mock<IRepositoryBase<UserArtifact>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        SetCurrentUser(CurrentUserId);

        _service = new UserArtifactService(
            cachingService,
            _mapper,
            new Mock<ILogger<UserArtifactService>>().Object,
            _mockRepo.Object,
            _mockHttpContextAccessor.Object);
    }

    private void SetCurrentUser(string userId)
    {
        var claims = new List<Claim> { new(CustomClaimTypes.UserId, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithNoteDto_ShouldSetOwnerAndPersistAsNote()
    {
        UserArtifactDto dto = new NoteDto { Content = "My note", Tags = ["tag1"] };

        UserArtifact savedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<UserArtifact>()))
            .Callback<UserArtifact>(e => savedEntity = e)
            .Returns(Task.CompletedTask);

        var result = await _service.AddAsync(dto);

        Assert.True(result.Succeeded);
        var note = Assert.IsType<Note>(savedEntity);
        Assert.Equal(CurrentUserId, note.UserId);
        Assert.Equal("My note", note.Content);
        Assert.IsType<NoteDto>(result.Payload);
    }

    [Fact]
    public async Task AddAsync_ShouldIgnoreClientSuppliedOwnershipAndDeletionFields()
    {
        UserArtifactDto dto = new NoteDto
        {
            Content = "My note",
            UserId = "someone-else",
            DeletedByUserId = "someone-else",
            DeletedDate = DateTime.UtcNow
        };

        UserArtifact savedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<UserArtifact>()))
            .Callback<UserArtifact>(e => savedEntity = e)
            .Returns(Task.CompletedTask);

        await _service.AddAsync(dto);

        Assert.Equal(CurrentUserId, savedEntity.UserId);
        Assert.Null(savedEntity.DeletedByUserId);
        Assert.Null(savedEntity.DeletedDate);
    }

    [Fact]
    public async Task AddAsync_WithAnnotationDto_ShouldPersistAsAnnotation()
    {
        UserArtifactDto dto = new AnnotationDto { DocumentId = "doc-1", DocumentHash = "hash-1" };

        UserArtifact savedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<UserArtifact>()))
            .Callback<UserArtifact>(e => savedEntity = e)
            .Returns(Task.CompletedTask);

        var result = await _service.AddAsync(dto);

        Assert.True(result.Succeeded);
        var annotation = Assert.IsType<Annotation>(savedEntity);
        Assert.Equal("doc-1", annotation.DocumentId);
        Assert.IsType<AnnotationDto>(result.Payload);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WhenOwnedByCurrentUserAndNotDeleted_ShouldReturnDto()
    {
        var entity = new Note { Id = "note-1", UserId = CurrentUserId, Content = "hello" };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(entity);

        var result = await _service.GetByIdAsync("note-1");

        Assert.NotNull(result);
        Assert.IsType<NoteDto>(result);
        Assert.Equal("hello", ((NoteDto)result).Content);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOwnedByAnotherUser_ShouldReturnNull()
    {
        var entity = new Note { Id = "note-1", UserId = "someone-else", Content = "hello" };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(entity);

        var result = await _service.GetByIdAsync("note-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSoftDeletedAndIncludeDeletedFalse_ShouldReturnNull()
    {
        var entity = new Note { Id = "note-1", UserId = CurrentUserId, DeletedDate = DateTime.UtcNow };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(entity);

        var result = await _service.GetByIdAsync("note-1", includeDeleted: false);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenSoftDeletedAndIncludeDeletedTrue_ShouldReturnDto()
    {
        var entity = new Note { Id = "note-1", UserId = CurrentUserId, DeletedDate = DateTime.UtcNow };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(entity);

        var result = await _service.GetByIdAsync("note-1", includeDeleted: true);

        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldReturnNull()
    {
        _mockRepo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((UserArtifact)null);

        var result = await _service.GetByIdAsync("missing");

        Assert.Null(result);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenOwnedByCurrentUser_ShouldUpdateContent()
    {
        var existing = new Note { Id = "note-1", UserId = CurrentUserId, Content = "old content" };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        UserArtifactDto dto = new NoteDto { Id = "note-1", Content = "new content" };

        var result = await _service.UpdateAsync(dto);

        Assert.True(result.Succeeded);
        Assert.Equal("new content", existing.Content);
        Assert.Equal(CurrentUserId, existing.UserId);
        _mockRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenOwnedByAnotherUser_ShouldFail()
    {
        var existing = new Note { Id = "note-1", UserId = "someone-else", Content = "old content" };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        UserArtifactDto dto = new NoteDto { Id = "note-1", Content = "new content" };

        var result = await _service.UpdateAsync(dto);

        Assert.False(result.Succeeded);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenArtifactSoftDeleted_ShouldFail()
    {
        var existing = new Note { Id = "note-1", UserId = CurrentUserId, DeletedDate = DateTime.UtcNow };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        UserArtifactDto dto = new NoteDto { Id = "note-1", Content = "new content" };

        var result = await _service.UpdateAsync(dto);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task UpdateAsync_WhenArtifactTypeDoesNotMatch_ShouldFail()
    {
        var existing = new Annotation { Id = "a-1", UserId = CurrentUserId, DocumentId = "doc-1" };
        _mockRepo.Setup(r => r.GetByIdAsync("a-1")).ReturnsAsync(existing);

        UserArtifactDto dto = new NoteDto { Id = "a-1", Content = "new content" };

        var result = await _service.UpdateAsync(dto);

        Assert.False(result.Succeeded);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenOwnedByCurrentUser_ShouldSoftDelete()
    {
        var existing = new Note { Id = "note-1", UserId = CurrentUserId };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        var result = await _service.DeleteAsync("note-1");

        Assert.True(result.Succeeded);
        Assert.NotNull(existing.DeletedDate);
        Assert.Equal(CurrentUserId, existing.DeletedByUserId);
        _mockRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenOwnedByAnotherUser_ShouldFail()
    {
        var existing = new Note { Id = "note-1", UserId = "someone-else" };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        var result = await _service.DeleteAsync("note-1");

        Assert.False(result.Succeeded);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenAlreadyDeleted_ShouldBeIdempotent()
    {
        var deletedDate = DateTime.UtcNow.AddDays(-1);
        var existing = new Note { Id = "note-1", UserId = CurrentUserId, DeletedDate = deletedDate, DeletedByUserId = CurrentUserId };
        _mockRepo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        var result = await _service.DeleteAsync("note-1");

        Assert.True(result.Succeeded);
        Assert.Equal(deletedDate, existing.DeletedDate);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    #endregion

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_WithEmptyNoteContent_ShouldFail()
    {
        var result = await _service.ValidateAsync(new NoteDto { Content = "" });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_WithMissingAnnotationFields_ShouldFail()
    {
        var result = await _service.ValidateAsync(new AnnotationDto());

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task ValidateAsync_WithValidNote_ShouldSucceed()
    {
        var result = await _service.ValidateAsync(new NoteDto { Content = "valid" });

        Assert.True(result.Succeeded);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNullCriteria_ShouldScopeToCurrentUserAndExcludeDeleted()
    {
        var entities = new List<UserArtifact>
        {
            new Note { Id = "n1", UserId = CurrentUserId, Content = "note content" },
            new Annotation { Id = "a1", UserId = CurrentUserId, DocumentId = "doc-1" }
        };

        _mockRepo.Setup(r => r.FindAsync(
                CollectionNameConstants.USER_ARTIFACTS,
                It.IsAny<MongoDB.Driver.FilterDefinition<UserArtifact>>(),
                null,
                100,
                null))
            .ReturnsAsync(entities);

        var result = await _service.SearchAsync(null);

        Assert.Equal(2, result.Count);
        Assert.IsType<NoteDto>(result[0]);
        Assert.IsType<AnnotationDto>(result[1]);
    }

    [Fact]
    public async Task SearchAsync_WithLimitAndSkip_ShouldPassThroughToRepo()
    {
        var criteria = new SearchArtifactsCriteria { Limit = 10, Skip = 5 };

        _mockRepo.Setup(r => r.FindAsync(
                CollectionNameConstants.USER_ARTIFACTS,
                It.IsAny<MongoDB.Driver.FilterDefinition<UserArtifact>>(),
                null,
                10,
                5))
            .ReturnsAsync([]);

        var result = await _service.SearchAsync(criteria);

        Assert.Empty(result);
        _mockRepo.Verify(r => r.FindAsync(
            CollectionNameConstants.USER_ARTIFACTS,
            It.IsAny<MongoDB.Driver.FilterDefinition<UserArtifact>>(),
            null,
            10,
            5), Times.Once);
    }

    #endregion
}
