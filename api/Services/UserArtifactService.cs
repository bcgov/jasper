using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Core.Infrastructure;
using Scv.Db.Contants;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Services;

public class UserArtifactService(
    IAppCache cache,
    IMapper mapper,
    ILogger<UserArtifactService> logger,
    IRepositoryBase<UserArtifact> userArtifactRepo,
    ICurrentUser currentUser,
    ICourtFileService courtFileService) : CrudServiceBase<IRepositoryBase<UserArtifact>, UserArtifact, UserArtifactDto>(
        cache,
        mapper,
        logger,
        userArtifactRepo), IUserArtifactService
{
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ICourtFileService _courtFileService = courtFileService;

    public override string CacheName => nameof(UserArtifactService);

    public async override Task<OperationResult<UserArtifactDto>> ValidateAsync(UserArtifactDto dto, bool isEdit = false)
    {
        var errors = new List<string>();
        dto.Labels.TryGetValue(LabelConstants.PHYSICAL_FILE_ID, out var fileIdValue);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(fileIdValue);

        dto.Labels.TryGetValue(LabelConstants.COURT_CLASS_CD, out var courtClassCdValue);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(courtClassCdValue);

        var courtFileResult = await _courtFileService.ValidateFileExistsAsync(courtClassCdValue, fileIdValue);
        if (!courtFileResult.Succeeded)
        {
            errors.AddRange([.. courtFileResult.Errors]);
        }

        if (isEdit && dto.UserId != _currentUser.UserId)
        {
            errors.Add($"User {dto.UserId} is not allowed to access this user artifact.");
        }

        return errors.Count != 0
            ? OperationResult<UserArtifactDto>.Failure([.. errors])
            : OperationResult<UserArtifactDto>.Success(dto);
    }

    public override async Task<List<UserArtifactDto>> GetAllAsync()
    {
        var currentUserId = this.GetCurrentUserId();
        var entities = await this.Repo.FindAsync(a => a.UserId == currentUserId && a.DeletedDate == null);

        return [.. entities.Select(e => this.Mapper.Map<UserArtifact, UserArtifactDto>(e))];
    }

    public override async Task<UserArtifactDto> GetByIdAsync(string id) =>
        await GetByIdAsync(id, includeDeleted: false);

    public async Task<UserArtifactDto> GetByIdAsync(string id, bool includeDeleted = false)
    {
        var currentUserId = this.GetCurrentUserId();
        var entity = await this.Repo.GetByIdAsync(id);
        if (entity == null || entity.UserId != currentUserId || (entity.DeletedDate != null && !includeDeleted))
        {
            return null;
        }

        return this.Mapper.Map<UserArtifact, UserArtifactDto>(entity);
    }

    public override async Task<OperationResult<UserArtifactDto>> AddAsync(UserArtifactDto dto)
    {
        var currentUserId = this.GetCurrentUserId();

        try
        {
            dto.UserId = currentUserId;
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

    public override async Task<OperationResult<UserArtifactDto>> AddRangeAsync(List<UserArtifactDto> dtos)
    {
        var currentUserId = this.GetCurrentUserId();

        try
        {
            if (dtos == null || dtos.Count == 0)
            {
                return OperationResult<UserArtifactDto>.Failure("No user artifacts provided.");
            }

            foreach (var dto in dtos)
            {
                dto.UserId = currentUserId;
                dto.DeletedDate = null;
                dto.DeletedByUserId = null;
            }

            var entities = dtos.Select(d => this.Mapper.Map<UserArtifactDto, UserArtifact>(d)).ToList();

            await this.Repo.AddRangeAsync(entities);

            this.InvalidateCache(this.CacheName);

            // The result type carries a single DTO, so there is no payload for a range.
            return OperationResult<UserArtifactDto>.Success(null);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error adding user artifacts: {Message}", ex.Message);
            return OperationResult<UserArtifactDto>.Failure("Error when adding user artifacts.");
        }
    }

    public override async Task<OperationResult> DeleteAsync(string id)
    {
        var currentUserId = this.GetCurrentUserId();

        try
        {
            var existing = await this.Repo.GetByIdAsync(id);
            if (existing == null || existing.DeletedDate != null)
            {
                return OperationResult.Failure("User artifact not found.");
            }

            if (existing.UserId != currentUserId)
            {
                return OperationResult.Failure("User is not allowed to delete this user artifact.");
            }

            MarkAsDeleted(existing, currentUserId);

            await this.Repo.UpdateAsync(existing);

            this.InvalidateCache(this.CacheName);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error deleting user artifact: {Message}", ex.Message);
            return OperationResult.Failure("Error when deleting user artifact.");
        }
    }

    public override async Task<OperationResult> DeleteRangeAsync(List<string> ids)
    {
        var currentUserId = this.GetCurrentUserId();

        try
        {
            var existing = (await this.Repo.FindAsync(a => ids.Contains(a.Id) && a.DeletedDate == null)).ToList();

            if (existing.Count == 0)
            {
                return OperationResult.Failure("No user artifacts found.");
            }

            if (existing.Any(e => e.UserId != currentUserId))
            {
                return OperationResult.Failure("User is not allowed to delete one or more of these user artifacts.");
            }

            existing.ForEach(entity => MarkAsDeleted(entity, currentUserId));

            await this.Repo.UpdateRangeAsync(existing);

            this.InvalidateCache(this.CacheName);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error deleting user artifacts: {Message}", ex.Message);
            return OperationResult.Failure("Error when deleting user artifacts.");
        }
    }

    public override async Task<OperationResult<UserArtifactDto>> UpdateAsync(UserArtifactDto dto)
    {
        var currentUserId = this.GetCurrentUserId();

        try
        {
            var existing = await this.Repo.GetByIdAsync(dto.Id);
            if (existing == null || existing.DeletedDate != null)
            {
                return OperationResult<UserArtifactDto>.Failure("User artifact not found.");
            }

            if (existing.UserId != currentUserId)
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

    #region Private Methods

    private string GetCurrentUserId()
    {
        var userId = _currentUser.UserId;
        return string.IsNullOrWhiteSpace(userId)
            ? throw new InvalidOperationException("No valid user id found.")
            : userId;
    }

    private static void MarkAsDeleted(UserArtifact entity, string userId)
    {
        entity.DeletedDate = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
    }

    #endregion
}
