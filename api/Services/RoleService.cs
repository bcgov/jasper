using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LazyCache;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure;
using Scv.Api.Models.UserManagement;
using Scv.Db.Models;
using Scv.Db.Repositories;


namespace Scv.Api.Services;

public interface IRoleService
{
    Task<List<RoleDto>> GetRolesAsync();
    Task<RoleDto> GetRoleByIdAsync(string id);
    Task<OperationResult<RoleDto>> CreateRoleAsync(RoleCreateDto roleDto);
    Task<OperationResult<RoleCreateDto>> ValidateRoleCreateDtoAsync(RoleCreateDto role);
    Task<OperationResult<RoleDto>> UpdateRoleAsync(string id, RoleUpdateDto roleDto);
    Task<OperationResult<RoleUpdateDto>> ValidateRoleUpdateDtoAsync(RoleUpdateDto role);
    Task<OperationResult> DeleteRoleAsync(string id);
}

public class RoleService(
    IAppCache cache,
    IMapper mapper,
    ILogger<RoleService> logger,
    IRoleRepository roleRepo,
    IPermissionRepository permissionRepo
    ) : ServiceBase(cache), IRoleService
{
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<RoleService> _logger = logger;
    private readonly IRoleRepository _roleRepo = roleRepo;
    private readonly IPermissionRepository _permissionRepo = permissionRepo;

    public async Task<List<RoleDto>> GetRolesAsync() =>
        await this.GetDataFromCache(
            "GetRolesAsync",
            async () =>
            {
                var roles = (await _roleRepo.GetAllAsync()).OrderBy(r => r.Name);
                return _mapper.Map<List<RoleDto>>(roles);
            });

    public async Task<RoleDto> GetRoleByIdAsync(string id)
    {
        var role = await _roleRepo.GetByIdAsync(id);

        return _mapper.Map<RoleDto>(role);
    }

    public async Task<OperationResult<RoleDto>> CreateRoleAsync(RoleCreateDto roleDto)
    {
        try
        {
            var role = _mapper.Map<Role>(roleDto);

            await _roleRepo.AddAsync(role);

            return OperationResult<RoleDto>.Success(_mapper.Map<RoleDto>(role));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when creating role: {message}", ex.Message);
            return OperationResult<RoleDto>.Failure("Error when creating role.");
        }
    }

    public async Task<OperationResult<RoleCreateDto>> ValidateRoleCreateDtoAsync(RoleCreateDto role)
    {
        var errors = new List<string>();

        var existingPermissionsIds = (await _permissionRepo.GetActivePermissionsAsync()).Select(p => p.Id);

        // Check if permission ids are all valid
        if (!role.PermissionIds.All(id => existingPermissionsIds.Contains(id)))
        {
            errors.Add("Found one or more invalid permission IDs.");
        }

        return errors.Count != 0
            ? OperationResult<RoleCreateDto>.Failure([.. errors])
            : OperationResult<RoleCreateDto>.Success(role);
    }

    public async Task<OperationResult<RoleDto>> UpdateRoleAsync(string id, RoleUpdateDto roleDto)
    {
        try
        {
            var role = await _roleRepo.GetByIdAsync(id);

            _mapper.Map(roleDto, role);

            await _roleRepo.UpdateAsync(role);

            return OperationResult<RoleDto>.Success(_mapper.Map<RoleDto>(role));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when update role: {message}", ex.Message);
            return OperationResult<RoleDto>.Failure("Error when updating role.");
        }
    }

    public async Task<OperationResult<RoleUpdateDto>> ValidateRoleUpdateDtoAsync(RoleUpdateDto role)
    {
        var errors = new List<string>();

        var existingPermissionsIds = (await _permissionRepo.GetActivePermissionsAsync()).Select(p => p.Id);

        if (await _roleRepo.GetByIdAsync(role.Id) == null)
        {
            errors.Add("Role ID is not found.");
        }

        if (!role.PermissionIds.All(id => existingPermissionsIds.Contains(id)))
        {
            errors.Add("Found one or more invalid permission IDs.");
        }

        return errors.Count != 0
            ? OperationResult<RoleUpdateDto>.Failure([.. errors])
            : OperationResult<RoleUpdateDto>.Success(role);
    }

    public async Task<OperationResult> DeleteRoleAsync(string id)
    {
        try
        {
            var existingRole = await _roleRepo.GetByIdAsync(id);
            if (existingRole == null)
            {
                var err = new List<string> { "Role ID not found." };
                return OperationResult.Failure([.. err]);
            }

            await _roleRepo.DeleteAsync(existingRole);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {message}", ex.Message);
            return OperationResult.Failure("Error when deleting role.");
        }
    }
}
