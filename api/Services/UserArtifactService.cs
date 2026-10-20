using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Services;

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

    public override async Task<List<UserArtifactDto>> GetAllAsync()
    {
        var entities = await this.Repo.FindAsync(a => a.UserId == this.CurrentUserId && a.DeletedDate == null);

        return [.. entities.Select(e => this.Mapper.Map<UserArtifact, UserArtifactDto>(e))];
    }

    public override async Task<UserArtifactDto> GetByIdAsync(string id) =>
        await GetByIdAsync(id, includeDeleted: false);

    public async Task<UserArtifactDto> GetByIdAsync(string id, bool includeDeleted)
    {
        var entity = await this.Repo.GetByIdAsync(id);
        if (entity == null || entity.UserId != this.CurrentUserId || (entity.DeletedDate != null && !includeDeleted))
        {
            return null;
        }

        return this.Mapper.Map<UserArtifact, UserArtifactDto>(entity);
    }

    public override async Task<OperationResult<UserArtifactDto>> AddAsync(UserArtifactDto dto)
    {
        try
        {
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
            return OperationResult<UserArtifactDto>.Failure("Error when adding user artifact.");
        }
    }

    public override async Task<OperationResult<UserArtifactDto>> UpdateAsync(UserArtifactDto dto)
    {
        try
        {
            var existing = await this.Repo.GetByIdAsync(dto.Id);
            if (existing == null || existing.DeletedDate != null)
            {
                return OperationResult<UserArtifactDto>.Failure("User artifact not found.");
            }

            if (existing.UserId != this.CurrentUserId)
            {
                return OperationResult<UserArtifactDto>.Failure("User is not allowed to modify this user artifact.");
            }

            // Check if the existing entity and the DTO are of the same type, the type has not changed
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

            dto.UserId = existing.UserId;
            dto.DeletedDate = existing.DeletedDate;
            dto.DeletedByUserId = existing.DeletedByUserId;

            this.Mapper.Map(dto, existing);

            await this.Repo.UpdateAsync(existing);

            this.InvalidateCache(this.CacheName);

            return OperationResult<UserArtifactDto>.Success(this.Mapper.Map<UserArtifact, UserArtifactDto>(existing));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error updating user artifact: {Message}", ex.Message);
            return OperationResult<UserArtifactDto>.Failure("Error when updating user artifact.");
        }
    }
}
