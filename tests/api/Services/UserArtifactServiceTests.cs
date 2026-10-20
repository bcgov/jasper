using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
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
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.UserArtifacts;
using Xunit;

namespace tests.api.Services;

public class UserArtifactServiceTests
{
    private const string CurrentUserId = "user-1";
    private const string OtherUserId = "user-2";

    private readonly Mock<IRepositoryBase<UserArtifact>> _repo = new();
    private readonly Mock<ILogger<UserArtifactService>> _logger = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly CachingService _cache;
    private readonly UserArtifactService _service;

    public UserArtifactServiceTests()
    {
        _cache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        var config = new TypeAdapterConfig();
        config.Apply(new UserArtifactMapping());
        var mapper = new Mapper(config);

        SetCurrentUser(CurrentUserId);

        _service = new UserArtifactService(
            _cache,
            mapper,
            _logger.Object,
            _repo.Object,
            _httpContextAccessor.Object);
    }

    private void SetCurrentUser(string userId)
    {
        var claims = userId == null ? [] : new[] { new Claim(CustomClaimTypes.UserId, userId) };
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(context);
    }

    private static Note CreateNote(string id = "note-1", string userId = CurrentUserId, DateTime? deletedDate = null) => new()
    {
        Id = id,
        UserId = userId,
        ArtifactType = ArtifactType.Note,
        Content = "content",
        Tags = ["tag"],
        DeletedDate = deletedDate,
        DeletedByUserId = deletedDate == null ? null : userId,
        Ent_Dtm = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Upd_Dtm = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Annotation CreateAnnotation(string id = "ann-1", string userId = CurrentUserId) => new()
    {
        Id = id,
        UserId = userId,
        ArtifactType = ArtifactType.Annotation,
        DocumentId = "doc-1",
        DocumentHash = "hash-1",
        AnnotationData = """{"page":1}"""
    };

    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_ValidNote_ReturnsSuccess()
    {
        var dto = new NoteDto { Content = "hello" };

        var result = await _service.ValidateAsync(dto);

        result.Succeeded.Should().BeTrue();
        result.Payload.Should().BeSameAs(dto);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_NoteWithoutContent_ReturnsFailure(string content)
    {
        var result = await _service.ValidateAsync(new NoteDto { Content = content });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Note content is required.");
    }

    [Fact]
    public async Task ValidateAsync_ValidAnnotation_ReturnsSuccess()
    {
        var result = await _service.ValidateAsync(new AnnotationDto { DocumentId = "d", DocumentHash = "h" });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_AnnotationMissingDocumentIdAndHash_ReturnsBothErrors()
    {
        var result = await _service.ValidateAsync(new AnnotationDto());

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo(
            "Annotation document ID is required.",
            "Annotation document hash is required.");
    }

    [Fact]
    public async Task ValidateAsync_AnnotationMissingDocumentId_ReturnsDocumentIdError()
    {
        var result = await _service.ValidateAsync(new AnnotationDto { DocumentHash = "h" });

        result.Errors.Should().ContainSingle().Which.Should().Be("Annotation document ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_AnnotationMissingDocumentHash_ReturnsDocumentHashError()
    {
        var result = await _service.ValidateAsync(new AnnotationDto { DocumentId = "d" });

        result.Errors.Should().ContainSingle().Which.Should().Be("Annotation document hash is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateAsync_EditWithoutId_ReturnsFailure(string id)
    {
        var result = await _service.ValidateAsync(new NoteDto { Id = id, Content = "x" }, isEdit: true);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Artifact ID is required.");
    }

    [Fact]
    public async Task ValidateAsync_EditWithId_ReturnsSuccess()
    {
        var result = await _service.ValidateAsync(new NoteDto { Id = "id", Content = "x" }, isEdit: true);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_AddWithoutId_ReturnsSuccess()
    {
        var result = await _service.ValidateAsync(new NoteDto { Content = "x" }, isEdit: false);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_InvalidEditNote_ReturnsAllErrors()
    {
        var result = await _service.ValidateAsync(new NoteDto { Id = null, Content = null }, isEdit: true);

        result.Errors.Should().BeEquivalentTo("Note content is required.", "Artifact ID is required.");
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsMappedArtifacts_OfDifferentTypes()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserArtifact, bool>>>()))
            .ReturnsAsync([CreateNote(), CreateAnnotation()]);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
        var note = result.OfType<NoteDto>().Should().ContainSingle().Subject;
        note.Id.Should().Be("note-1");
        note.Content.Should().Be("content");
        note.CreatedDate.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        note.UpdatedDate.Should().Be(new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        var annotation = result.OfType<AnnotationDto>().Should().ContainSingle().Subject;
        annotation.DocumentId.Should().Be("doc-1");
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCurrentUserAndExcludesDeleted()
    {
        Expression<Func<UserArtifact, bool>> captured = null;
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserArtifact, bool>>>()))
            .Callback<Expression<Func<UserArtifact, bool>>>(e => captured = e)
            .ReturnsAsync([]);

        await _service.GetAllAsync();

        captured.Should().NotBeNull();
        var predicate = captured.Compile();
        predicate(CreateNote(userId: CurrentUserId)).Should().BeTrue();
        predicate(CreateNote(userId: OtherUserId)).Should().BeFalse();
        predicate(CreateNote(deletedDate: DateTime.UtcNow)).Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_NoResults_ReturnsEmptyList()
    {
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserArtifact, bool>>>())).ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_OwnedArtifact_ReturnsDto()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote());

        var result = await _service.GetByIdAsync("note-1");

        result.Should().BeOfType<NoteDto>().Which.Content.Should().Be("content");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((UserArtifact)null);

        var result = await _service.GetByIdAsync("missing");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_OtherUsersArtifact_ReturnsNull()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote(userId: OtherUserId));

        var result = await _service.GetByIdAsync("note-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_OtherUsersArtifact_ReturnsNull_EvenWhenIncludingDeleted()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote(userId: OtherUserId, deletedDate: DateTime.UtcNow));

        var result = await _service.GetByIdAsync("note-1", includeDeleted: true);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedArtifact_ReturnsNullByDefault()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote(deletedDate: DateTime.UtcNow));

        (await _service.GetByIdAsync("note-1")).Should().BeNull();
        (await _service.GetByIdAsync("note-1", includeDeleted: false)).Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedArtifact_ReturnsDtoWhenIncludeDeleted()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote(deletedDate: DateTime.UtcNow));

        var result = await _service.GetByIdAsync("note-1", includeDeleted: true);

        result.Should().NotBeNull();
        result.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_NoAuthenticatedUser_Throws()
    {
        SetCurrentUser(null);
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote());

        var act = () => _service.GetByIdAsync("note-1");

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_NoHttpContext_Throws()
    {
        _httpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext)null);
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote());

        var act = () => _service.GetByIdAsync("note-1");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhitespaceUserId_Throws()
    {
        SetCurrentUser("   ");

        var act = () => _service.GetByIdAsync("note-1");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllAsync_NoAuthenticatedUser_Throws()
    {
        SetCurrentUser(null);

        var act = () => _service.GetAllAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<UserArtifact, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_NoAuthenticatedUser_ThrowsInsteadOfReturningFailure()
    {
        SetCurrentUser(null);

        var act = () => _service.AddAsync(new NoteDto { Content = "x" });

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(r => r.AddAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NoAuthenticatedUser_ThrowsInsteadOfReturningFailure()
    {
        SetCurrentUser(null);

        var act = () => _service.UpdateAsync(new NoteDto { Id = "note-1", Content = "x" });

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    #endregion

    #region AddAsync

    [Fact]
    public async Task AddAsync_Note_SetsCurrentUserAndClearsDeletionFields()
    {
        UserArtifact added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<UserArtifact>()))
            .Callback<UserArtifact>(e => added = e)
            .Returns(Task.CompletedTask);
        var dto = new NoteDto
        {
            Content = "hello",
            Tags = ["a"],
            UserId = OtherUserId,
            DeletedDate = DateTime.UtcNow,
            DeletedByUserId = OtherUserId
        };

        var result = await _service.AddAsync(dto);

        result.Succeeded.Should().BeTrue();
        var note = added.Should().BeOfType<Note>().Subject;
        note.UserId.Should().Be(CurrentUserId);
        note.DeletedDate.Should().BeNull();
        note.DeletedByUserId.Should().BeNull();
        note.Content.Should().Be("hello");
        note.Tags.Should().Equal("a");
        note.ArtifactType.Should().Be(ArtifactType.Note);

        var payload = result.Payload.Should().BeOfType<NoteDto>().Subject;
        payload.UserId.Should().Be(CurrentUserId);
        payload.Content.Should().Be("hello");
        payload.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_Annotation_MapsToAnnotationEntity()
    {
        UserArtifact added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<UserArtifact>()))
            .Callback<UserArtifact>(e => added = e)
            .Returns(Task.CompletedTask);

        var result = await _service.AddAsync(new AnnotationDto { DocumentId = "d", DocumentHash = "h", AnnotationData = "x" });

        result.Succeeded.Should().BeTrue();
        var annotation = added.Should().BeOfType<Annotation>().Subject;
        annotation.DocumentId.Should().Be("d");
        annotation.DocumentHash.Should().Be("h");
        annotation.AnnotationData.Should().Be("x");
        result.Payload.Should().BeOfType<AnnotationDto>();
    }

    [Fact]
    public async Task AddAsync_DoesNotUseClientProvidedId()
    {
        UserArtifact added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<UserArtifact>()))
            .Callback<UserArtifact>(e => added = e)
            .Returns(Task.CompletedTask);

        await _service.AddAsync(new NoteDto { Id = "client-id", Content = "x" });

        added.Id.Should().NotBe("client-id");
    }

    [Fact]
    public async Task AddAsync_InvalidatesCache()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<UserArtifact>())).Returns(Task.CompletedTask);
        _cache.Add(_service.CacheName, "cached");

        await _service.AddAsync(new NoteDto { Content = "x" });

        _cache.Get<string>(_service.CacheName).Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_RepositoryThrows_ReturnsFailureAndLogsError()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<UserArtifact>())).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _service.AddAsync(new NoteDto { Content = "x" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Error when adding user artifact.");
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    #endregion

    #region AddRangeAsync

    [Fact]
    public async Task AddRangeAsync_SetsCurrentUserAndClearsDeletionFields_ForAllItems()
    {
        List<UserArtifact> added = null;
        _repo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()))
            .Callback<IEnumerable<UserArtifact>>(e => added = [.. e])
            .Returns(Task.CompletedTask);
        var dtos = new List<UserArtifactDto>
        {
            new NoteDto { Content = "a", UserId = OtherUserId, DeletedDate = DateTime.UtcNow, DeletedByUserId = OtherUserId },
            new AnnotationDto { DocumentId = "d", DocumentHash = "h", UserId = OtherUserId }
        };

        var result = await _service.AddRangeAsync(dtos);

        result.Succeeded.Should().BeTrue();
        added.Should().HaveCount(2);
        added[0].Should().BeOfType<Note>();
        added[1].Should().BeOfType<Annotation>();
        added.Should().AllSatisfy(a =>
        {
            a.UserId.Should().Be(CurrentUserId);
            a.DeletedDate.Should().BeNull();
            a.DeletedByUserId.Should().BeNull();
        });
        _repo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()), Times.Once);
    }

    [Fact]
    public async Task AddRangeAsync_DoesNotUseClientProvidedIds()
    {
        List<UserArtifact> added = null;
        _repo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()))
            .Callback<IEnumerable<UserArtifact>>(e => added = [.. e])
            .Returns(Task.CompletedTask);

        await _service.AddRangeAsync([new NoteDto { Id = "client-id", Content = "x" }]);

        added.Single().Id.Should().NotBe("client-id");
    }

    [Fact]
    public async Task AddRangeAsync_NullOrEmpty_ReturnsFailure()
    {
        var empty = await _service.AddRangeAsync([]);
        var nullList = await _service.AddRangeAsync(null);

        empty.Succeeded.Should().BeFalse();
        nullList.Succeeded.Should().BeFalse();
        empty.Errors.Should().ContainSingle().Which.Should().Be("No user artifacts provided.");
        _repo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()), Times.Never);
    }

    [Fact]
    public async Task AddRangeAsync_InvalidatesCache()
    {
        _repo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserArtifact>>())).Returns(Task.CompletedTask);
        _cache.Add(_service.CacheName, "cached");

        await _service.AddRangeAsync([new NoteDto { Content = "x" }]);

        _cache.Get<string>(_service.CacheName).Should().BeNull();
    }

    [Fact]
    public async Task AddRangeAsync_NoAuthenticatedUser_Throws()
    {
        SetCurrentUser(null);

        var act = () => _service.AddRangeAsync([new NoteDto { Content = "x" }]);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()), Times.Never);
    }

    [Fact]
    public async Task AddRangeAsync_RepositoryThrows_ReturnsFailureAndLogsError()
    {
        _repo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<UserArtifact>>())).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _service.AddRangeAsync([new NoteDto { Content = "x" }]);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Error when adding user artifacts.");
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ValidNote_UpdatesEntityAndReturnsDto()
    {
        var existing = CreateNote();
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<UserArtifact>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(new NoteDto { Id = "note-1", Content = "updated", Tags = ["x", "y"] });

        result.Succeeded.Should().BeTrue();
        var payload = result.Payload.Should().BeOfType<NoteDto>().Subject;
        payload.Content.Should().Be("updated");
        payload.Tags.Should().Equal("x", "y");
        existing.Content.Should().Be("updated");
        existing.UserId.Should().Be(CurrentUserId);
        existing.Id.Should().Be("note-1");
        _repo.Verify(r => r.UpdateAsync(existing), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ValidAnnotation_UpdatesEntity()
    {
        var existing = CreateAnnotation();
        _repo.Setup(r => r.GetByIdAsync("ann-1")).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<UserArtifact>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(new AnnotationDto
        {
            Id = "ann-1",
            DocumentId = "doc-1",
            DocumentHash = "hash-2",
            AnnotationData = "new"
        });

        result.Succeeded.Should().BeTrue();
        existing.DocumentHash.Should().Be("hash-2");
        existing.AnnotationData.Should().Be("new");
        result.Payload.Should().BeOfType<AnnotationDto>();
    }

    [Fact]
    public async Task UpdateAsync_ClientCannotOverrideOwnerOrDeletionFields()
    {
        var existing = CreateNote();
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<UserArtifact>())).Returns(Task.CompletedTask);

        var result = await _service.UpdateAsync(new NoteDto
        {
            Id = "note-1",
            Content = "updated",
            UserId = OtherUserId,
            DeletedDate = DateTime.UtcNow,
            DeletedByUserId = OtherUserId
        });

        result.Succeeded.Should().BeTrue();
        existing.UserId.Should().Be(CurrentUserId);
        existing.DeletedDate.Should().BeNull();
        existing.DeletedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsFailure()
    {
        _repo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((UserArtifact)null);

        var result = await _service.UpdateAsync(new NoteDto { Id = "missing", Content = "x" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("User artifact not found.");
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeletedArtifact_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote(deletedDate: DateTime.UtcNow));

        var result = await _service.UpdateAsync(new NoteDto { Id = "note-1", Content = "x" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("User artifact not found.");
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_OtherUsersArtifact_ReturnsNotAllowed()
    {
        var existing = CreateNote(userId: OtherUserId);
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        var result = await _service.UpdateAsync(new NoteDto { Id = "note-1", Content = "hijack" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("User is not allowed to modify this user artifact.");
        existing.Content.Should().Be("content");
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ArtifactTypeChanged_ReturnsFailure()
    {
        var existing = CreateNote();
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        var result = await _service.UpdateAsync(new AnnotationDto { Id = "note-1", DocumentId = "d", DocumentHash = "h" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Artifact type cannot be changed.");
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_AnnotationEntityWithNoteDto_ReturnsTypeChangedFailure()
    {
        _repo.Setup(r => r.GetByIdAsync("ann-1")).ReturnsAsync(CreateAnnotation());

        var result = await _service.UpdateAsync(new NoteDto { Id = "ann-1", Content = "x" });

        result.Errors.Should().ContainSingle().Which.Should().Be("Artifact type cannot be changed.");
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote());
        _repo.Setup(r => r.UpdateAsync(It.IsAny<UserArtifact>())).Returns(Task.CompletedTask);
        _cache.Add(_service.CacheName, "cached");

        await _service.UpdateAsync(new NoteDto { Id = "note-1", Content = "x" });

        _cache.Get<string>(_service.CacheName).Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrows_ReturnsFailureAndLogsError()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote());
        _repo.Setup(r => r.UpdateAsync(It.IsAny<UserArtifact>())).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _service.UpdateAsync(new NoteDto { Id = "note-1", Content = "x" });

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Error when updating user artifact.");
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_OwnedArtifact_SoftDeletesAndReturnsSuccess()
    {
        var existing = CreateNote();
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<UserArtifact>())).Returns(Task.CompletedTask);
        var before = DateTime.UtcNow;

        var result = await _service.DeleteAsync("note-1");

        result.Succeeded.Should().BeTrue();
        existing.DeletedDate.Should().NotBeNull().And.BeOnOrAfter(before);
        existing.DeletedByUserId.Should().Be(CurrentUserId);
        _repo.Verify(r => r.UpdateAsync(existing), Times.Once);
        _repo.Verify(r => r.DeleteAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Annotation_SoftDeletes()
    {
        var existing = CreateAnnotation();
        _repo.Setup(r => r.GetByIdAsync("ann-1")).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<UserArtifact>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync("ann-1");

        result.Succeeded.Should().BeTrue();
        existing.DeletedDate.Should().NotBeNull();
        existing.DeletedByUserId.Should().Be(CurrentUserId);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFailure()
    {
        _repo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((UserArtifact)null);

        var result = await _service.DeleteAsync("missing");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("User artifact not found.");
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeleted_ReturnsNotFoundAndKeepsOriginalDeletion()
    {
        var deletedDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var existing = CreateNote(deletedDate: deletedDate);
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        var result = await _service.DeleteAsync("note-1");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("User artifact not found.");
        existing.DeletedDate.Should().Be(deletedDate);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_OtherUsersArtifact_ReturnsNotAllowed()
    {
        var existing = CreateNote(userId: OtherUserId);
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(existing);

        var result = await _service.DeleteAsync("note-1");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("User is not allowed to delete this user artifact.");
        existing.DeletedDate.Should().BeNull();
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
        _repo.Verify(r => r.DeleteAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NoAuthenticatedUser_Throws()
    {
        SetCurrentUser(null);

        var act = () => _service.DeleteAsync("note-1");

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote());
        _repo.Setup(r => r.UpdateAsync(It.IsAny<UserArtifact>())).Returns(Task.CompletedTask);
        _cache.Add(_service.CacheName, "cached");

        await _service.DeleteAsync("note-1");

        _cache.Get<string>(_service.CacheName).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrows_ReturnsFailureAndLogsError()
    {
        _repo.Setup(r => r.GetByIdAsync("note-1")).ReturnsAsync(CreateNote());
        _repo.Setup(r => r.UpdateAsync(It.IsAny<UserArtifact>())).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _service.DeleteAsync("note-1");

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Error when deleting user artifact.");
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    #endregion

    #region DeleteRangeAsync

    // Applies the predicate the service passes, so the ID and not-deleted filters are exercised.
    private void SetupFind(params UserArtifact[] artifacts) =>
        _repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UserArtifact, bool>>>()))
            .ReturnsAsync((Expression<Func<UserArtifact, bool>> predicate) => artifacts.Where(predicate.Compile()).ToList());

    [Fact]
    public async Task DeleteRangeAsync_OwnedArtifacts_SoftDeletesAll()
    {
        var note = CreateNote("n1");
        var annotation = CreateAnnotation("a1");
        SetupFind(note, annotation);
        _repo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteRangeAsync(["n1", "a1"]);

        result.Succeeded.Should().BeTrue();
        new UserArtifact[] { note, annotation }.Should().AllSatisfy(a =>
        {
            a.DeletedDate.Should().NotBeNull();
            a.DeletedByUserId.Should().Be(CurrentUserId);
        });
        _repo.Verify(r => r.UpdateRangeAsync(
            It.Is<IEnumerable<UserArtifact>>(e => e.SequenceEqual(new UserArtifact[] { note, annotation }))), Times.Once);
        _repo.Verify(r => r.UpdateAsync(It.IsAny<UserArtifact>()), Times.Never);
        _repo.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRangeAsync_SkipsMissingAndAlreadyDeleted()
    {
        var note = CreateNote("n1");
        var deletedDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var alreadyDeleted = CreateNote("n2", deletedDate: deletedDate);
        var notRequested = CreateNote("n3");
        SetupFind(note, alreadyDeleted, notRequested);
        _repo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteRangeAsync(["n1", "n2", "missing"]);

        result.Succeeded.Should().BeTrue();
        note.DeletedDate.Should().NotBeNull();
        alreadyDeleted.DeletedDate.Should().Be(deletedDate);
        notRequested.DeletedDate.Should().BeNull();
        _repo.Verify(r => r.UpdateRangeAsync(
            It.Is<IEnumerable<UserArtifact>>(e => e.SequenceEqual(new UserArtifact[] { note }))), Times.Once);
    }

    [Fact]
    public async Task DeleteRangeAsync_DuplicateIds_DeletesEachArtifactOnce()
    {
        SetupFind(CreateNote("n1"));
        _repo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteRangeAsync(["n1", "n1"]);

        result.Succeeded.Should().BeTrue();
        _repo.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<UserArtifact>>(e => e.Count() == 1)), Times.Once);
    }

    [Fact]
    public async Task DeleteRangeAsync_LoadsArtifactsWithASingleQuery()
    {
        SetupFind(CreateNote("n1"), CreateNote("n2"), CreateNote("n3"));
        _repo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>())).Returns(Task.CompletedTask);

        await _service.DeleteRangeAsync(["n1", "n2", "n3"]);

        _repo.Verify(r => r.FindAsync(It.IsAny<Expression<Func<UserArtifact, bool>>>()), Times.Once);
        _repo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()), Times.Once);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRangeAsync_NoneFound_ReturnsFailure()
    {
        SetupFind();

        var result = await _service.DeleteRangeAsync(["x", "y"]);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("No user artifacts found.");
        _repo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRangeAsync_EmptyIds_ReturnsFailure()
    {
        SetupFind(CreateNote("n1"));

        var result = await _service.DeleteRangeAsync([]);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("No user artifacts found.");
        _repo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRangeAsync_AllDeleted_ReturnsFailure()
    {
        SetupFind(CreateNote("n1", deletedDate: DateTime.UtcNow));

        var result = await _service.DeleteRangeAsync(["n1"]);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("No user artifacts found.");
    }

    [Fact]
    public async Task DeleteRangeAsync_AnyArtifactOwnedByOtherUser_RejectsWholeRequest()
    {
        var mine = CreateNote("n1");
        var theirs = CreateNote("n2", userId: OtherUserId);
        SetupFind(mine, theirs);

        var result = await _service.DeleteRangeAsync(["n1", "n2"]);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Be("User is not allowed to delete one or more of these user artifacts.");
        mine.DeletedDate.Should().BeNull();
        theirs.DeletedDate.Should().BeNull();
        _repo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRangeAsync_NoAuthenticatedUser_Throws()
    {
        SetCurrentUser(null);

        var act = () => _service.DeleteRangeAsync(["n1"]);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteRangeAsync_InvalidatesCache()
    {
        SetupFind(CreateNote("n1"));
        _repo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>())).Returns(Task.CompletedTask);
        _cache.Add(_service.CacheName, "cached");

        await _service.DeleteRangeAsync(["n1"]);

        _cache.Get<string>(_service.CacheName).Should().BeNull();
    }

    [Fact]
    public async Task DeleteRangeAsync_RepositoryThrows_ReturnsFailureAndLogsError()
    {
        SetupFind(CreateNote("n1"));
        _repo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<UserArtifact>>())).ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _service.DeleteRangeAsync(["n1"]);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Should().Be("Error when deleting user artifacts.");
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    #endregion

    [Fact]
    public void CacheName_IsServiceName()
    {
        _service.CacheName.Should().Be(nameof(UserArtifactService));
    }
}
