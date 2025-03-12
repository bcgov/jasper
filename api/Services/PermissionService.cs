using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LazyCache;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure;
using Scv.Api.Models.UserManagement;
using Scv.Db.Repositories;


namespace Scv.Api.Services;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetPermissionsAsync();
    Task<PermissionDto> GetPermissionByIdAsync(string id);
    Task<OperationResult<PermissionDto>> UpdatePermissionAsync(string id, PermissionUpdateDto permissionDto);
    Task<OperationResult<PermissionUpdateDto>> ValidatePermissionUpdateDtoAsync(PermissionUpdateDto permission);
}

public class PermissionService(
    IAppCache cache,
    IMapper mapper,
    ILogger<PermissionService> logger,
    IPermissionRepository permissionRepo
) : ServiceBase(cache), IPermissionService
{
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<PermissionService> _logger = logger;
    private readonly IPermissionRepository _permissionRepo = permissionRepo;

    public async Task<List<PermissionDto>> GetPermissionsAsync() =>
        await this.GetDataFromCache(
            "GetPermissionsAsync",
            async () =>
            {
                var permissions = await _permissionRepo.GetActivePermissionsAsync();
                return _mapper.Map<List<PermissionDto>>(permissions);
            });

    public async Task<PermissionDto> GetPermissionByIdAsync(string id)
    {
        var permission = await _permissionRepo.GetByIdAsync(id);

        return _mapper.Map<PermissionDto>(permission);
    }

    public async Task<OperationResult<PermissionDto>> UpdatePermissionAsync(string id, PermissionUpdateDto permissionDto)
    {
        try
        {
            var permission = await _permissionRepo.GetByIdAsync(id);

            _mapper.Map(permissionDto, permission);

            await _permissionRepo.UpdateAsync(permission);

            return OperationResult<PermissionDto>.Success(_mapper.Map<PermissionDto>(permission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when update role: {message}", ex.Message);
            return OperationResult<PermissionDto>.Failure("Error when updating permission.");
        }
    }

    public async Task<OperationResult<PermissionUpdateDto>> ValidatePermissionUpdateDtoAsync(PermissionUpdateDto permission)
    {
        var errors = new List<string>();

        if (await _permissionRepo.GetByIdAsync(permission.Id) == null)
        {
            errors.Add("Permission ID is not found.");
        }

        return errors.Count != 0
            ? OperationResult<PermissionUpdateDto>.Failure([.. errors])
            : OperationResult<PermissionUpdateDto>.Success(permission);
    }
}
