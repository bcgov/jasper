using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Db.Contants;
using Scv.Db.Repositories;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Services;

public interface IUserArtifactService : ICrudService<UserArtifactDto>
{
    Task<UserArtifactDto> GetByIdAsync(string id, bool includeDeleted);
    Task<List<UserArtifactDto>> SearchAsync(SearchArtifactsCriteria criteria = null);
}

public class UserArtifactService(
    IAppCache cache,
    IMapper mapper,
    ILogger<UserArtifactService> logger,
    IRepositoryBase<UserArtifact> userArtifactRepo,
    IHttpContextAccessor httpContextAccessor) : CrudServiceBase<IRepositoryBase<UserArtifact>, UserArtifact, UserArtifactDto>(
        cache,
        mapper,
        logger,
        userArtifactRepo), IUserArtifactService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public override string CacheName => nameof(UserArtifactService);

    private string CurrentUserId => _httpContextAccessor.HttpContext?.User?.UserId();

    public override async Task<List<UserArtifactDto>> GetAllAsync()
    {
        var entities = await this.Repo.FindAsync(a => a.UserId == this.CurrentUserId && a.DeletedDate == null);

        // Map item-by-item - see SearchAsync for why Mapper.Map<List<TDto>> is avoided here.
        return [.. entities.Select(e => this.Mapper.Map<UserArtifact, UserArtifactDto>(e))];
    }

    public override async Task<UserArtifactDto> GetByIdAsync(string id) => await GetByIdAsync(id, includeDeleted: false);

    public async Task<UserArtifactDto> GetByIdAsync(string id, bool includeDeleted)
    {
        var entity = await this.Repo.GetByIdAsync(id);
        if (entity == null || entity.UserId != this.CurrentUserId || (entity.DeletedDate != null && !includeDeleted))
        {
            return null;
        }

        // Note: this must be a 2-type-param Map call (TSource, TDestination both explicit). Mapster's
        // single-type-param Map<TDestination>(object source) overload - as used by CrudServiceBase - does
        // not resolve Note/Annotation polymorphically even with .Include<>() configured; see
        // UserArtifactMappingTests for the behavior this relies on.
        return this.Mapper.Map<UserArtifact, UserArtifactDto>(entity);
    }

    public override async Task<OperationResult<UserArtifactDto>> AddAsync(UserArtifactDto dto)
    {
        try
        {
            // Ownership and soft-delete state are always server-controlled, never client-supplied.
            dto.UserId = this.CurrentUserId;
            dto.DeletedDate = null;
            dto.DeletedByUserId = null;

            var entity = this.Mapper.Map<UserArtifactDto, UserArtifact>(dto);

            await this.Repo.AddAsync(entity);

            this.InvalidateCache(this.CacheName);

            return OperationResult<UserArtifactDto>.Success(this.Mapper.Map<UserArtifact, UserArtifactDto>(entity));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error adding user artifact: {Message}", ex.Message);
            return OperationResult<UserArtifactDto>.Failure("Error when adding the artifact.");
        }
    }

    public override async Task<OperationResult<UserArtifactDto>> UpdateAsync(UserArtifactDto dto)
    {
        try
        {
            var existing = await this.Repo.GetByIdAsync(dto.Id);
            if (existing == null || existing.DeletedDate != null)
            {
                return OperationResult<UserArtifactDto>.Failure("Artifact not found.");
            }

            if (existing.UserId != this.CurrentUserId)
            {
                return OperationResult<UserArtifactDto>.Failure("You do not have permission to modify this artifact.");
            }

            // Compared by CLR type (rather than the ArtifactType discriminator field) since that field is
            // only guaranteed to be populated by EF Core's discriminator mapping on save/load.
            var typeChanged = (existing, dto) switch
            {
                (Note, NoteDto) => false,
                (Annotation, AnnotationDto) => false,
                _ => true
            };

            if (typeChanged)
            {
                return OperationResult<UserArtifactDto>.Failure("Artifact type cannot be changed.");
            }

            // Ownership and soft-delete state can only change via AddAsync/DeleteAsync, not Update.
            dto.UserId = existing.UserId;
            dto.DeletedDate = existing.DeletedDate;
            dto.DeletedByUserId = existing.DeletedByUserId;

            // Map(source, destination) populates the existing (already concrete Note/Annotation) instance.
            // Both arguments' compile-time types (UserArtifactDto, UserArtifact) match the Include<>-configured
            // pair, so - unlike the single-type-param Map<TDestination>(object) overload - this dispatches
            // to the Note/Annotation-specific mapping correctly.
            this.Mapper.Map(dto, existing);

            await this.Repo.UpdateAsync(existing);

            this.InvalidateCache(this.CacheName);

            return OperationResult<UserArtifactDto>.Success(this.Mapper.Map<UserArtifact, UserArtifactDto>(existing));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error updating user artifact: {Message}", ex.Message);
            return OperationResult<UserArtifactDto>.Failure("Error when updating the artifact.");
        }
    }

    public override async Task<OperationResult> DeleteAsync(string id)
    {
        try
        {
            var existing = await this.Repo.GetByIdAsync(id);
            if (existing == null || existing.UserId != this.CurrentUserId)
            {
                return OperationResult.Failure("Artifact not found.");
            }

            if (existing.DeletedDate == null)
            {
                existing.DeletedDate = DateTime.UtcNow;
                existing.DeletedByUserId = this.CurrentUserId;

                await this.Repo.UpdateAsync(existing);
                this.InvalidateCache(this.CacheName);
            }

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error deleting user artifact: {Message}", ex.Message);
            return OperationResult.Failure("Error when deleting the artifact.");
        }
    }

    public override Task<OperationResult<UserArtifactDto>> ValidateAsync(UserArtifactDto dto, bool isEdit = false)
    {
        var errors = new List<string>();

        switch (dto)
        {
            case NoteDto note when string.IsNullOrWhiteSpace(note.Content):
                errors.Add("Note content is required.");
                break;
            case AnnotationDto annotation:
                if (string.IsNullOrWhiteSpace(annotation.DocumentId))
                {
                    errors.Add("Annotation document ID is required.");
                }
                if (string.IsNullOrWhiteSpace(annotation.DocumentHash))
                {
                    errors.Add("Annotation document hash is required.");
                }
                break;
        }

        if (isEdit && string.IsNullOrWhiteSpace(dto.Id))
        {
            errors.Add("Artifact ID is required.");
        }

        return Task.FromResult(errors.Count != 0
            ? OperationResult<UserArtifactDto>.Failure([.. errors])
            : OperationResult<UserArtifactDto>.Success(dto));
    }

    public async Task<List<UserArtifactDto>> SearchAsync(SearchArtifactsCriteria criteria = null)
    {
        criteria ??= new SearchArtifactsCriteria();

        var filter = BuildSearchFilter(criteria, this.CurrentUserId);

        var entities = await this.Repo.FindAsync(
            CollectionNameConstants.USER_ARTIFACTS,
            filter,
            options: null,
            limit: criteria.Limit,
            skip: criteria.Skip);

        // Map item-by-item with explicit 2-type-param Map<TSource,TDestination> calls (rather than
        // Mapper.Map<List<TDto>> or the single-type-param Map<TDto>(object)) so each Note/Annotation
        // resolves to its concrete DTO type - see UserArtifactMappingTests for the behavior this relies on.
        return [.. entities.Select(e => this.Mapper.Map<UserArtifact, UserArtifactDto>(e))];
    }

    private static FilterDefinition<UserArtifact> BuildSearchFilter(SearchArtifactsCriteria criteria, string userId)
    {
        var filterBuilder = Builders<UserArtifact>.Filter;
        var filter = filterBuilder.Eq(a => a.UserId, userId);

        if (!criteria.IncludeDeleted)
        {
            filter &= filterBuilder.Eq(a => a.DeletedDate, null);
        }

        if (criteria.ArtifactType.HasValue)
        {
            filter &= filterBuilder.Eq("ArtifactType", criteria.ArtifactType.Value.ToString());
        }

        if (criteria.LabelKeysExist?.Count > 0)
        {
            filter = criteria.LabelKeysExist.Aggregate(
                filter,
                (current, key) => current & filterBuilder.Exists($"Labels.{key}", true));
        }

        if (criteria.LabelMatches?.Count > 0)
        {
            filter = criteria.LabelMatches.Aggregate(
                filter,
                (current, label) => current & filterBuilder.Eq($"Labels.{label.Key}", label.Value));
        }

        if (criteria.Tags?.Count > 0)
        {
            filter &= filterBuilder.AnyIn("Tags", criteria.Tags);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            filter &= filterBuilder.Regex("Content", new BsonRegularExpression(Regex.Escape(criteria.Keyword), "i"));
        }

        if (criteria.CreatedAfter.HasValue)
        {
            filter &= filterBuilder.Gte(a => a.Ent_Dtm, criteria.CreatedAfter.Value);
        }

        if (criteria.CreatedBefore.HasValue)
        {
            filter &= filterBuilder.Lte(a => a.Ent_Dtm, criteria.CreatedBefore.Value);
        }

        if (criteria.UpdatedAfter.HasValue)
        {
            filter &= filterBuilder.Gte(a => a.Upd_Dtm, criteria.UpdatedAfter.Value);
        }

        if (criteria.UpdatedBefore.HasValue)
        {
            filter &= filterBuilder.Lte(a => a.Upd_Dtm, criteria.UpdatedBefore.Value);
        }

        return filter;
    }
}
