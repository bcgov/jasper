using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LazyCache;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure;
using Scv.Api.Models.AccessControlManagement;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public class RoleService(
    IAppCache cache,
    IMapper mapper,
    ILogger<RoleService> logger,
    IRepositoryBase<Role> roleRepo,
    IPermissionRepository permissionRepo
    ) : AccessControlManagementServiceBase<IRepositoryBase<Role>, Role, RoleDto>(
        cache,
        mapper,
        logger,
        roleRepo)
{
    private readonly IPermissionRepository _permissionRepo = permissionRepo;

    public override string CacheName => "GetRolesAsync";

    public override async Task<List<RoleDto>> GetAllAsync()
    {
        var roles = (await base.GetAllAsync()).OrderBy(r => r.Name).ToList();
        return roles;
    }

    public override async Task<OperationResult<RoleDto>> ValidateAsync(RoleDto role, bool isEdit = false)
    {
        var errors = new List<string>();

        if (isEdit && await this.Repo.GetByIdAsync(role.Id) == null)
        {
            errors.Add("Role ID is not found.");
        }

        // Check if permission ids are all valid
        var existingPermissionIds = (await _permissionRepo.GetActivePermissionsAsync()).Select(p => p.Id);
        if (!role.PermissionIds.All(id => existingPermissionIds.Contains(id)))
        {
            errors.Add("Found one or more invalid permission IDs.");
        }

        return errors.Count != 0
            ? OperationResult<RoleDto>.Failure([.. errors])
            : OperationResult<RoleDto>.Success(role);
    }
}
