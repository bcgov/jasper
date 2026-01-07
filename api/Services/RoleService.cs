using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AccessControlManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scv.Api.Services;

public class RoleService(
    IAppCache cache,
    IMapper mapper,
    ILogger<RoleService> logger,
    IRepositoryBase<Role> roleRepo,
    IPermissionRepository permissionRepo,
    IRepositoryBase<Group> groupRepo
    ) : CrudServiceBase<IRepositoryBase<Role>, Role, RoleDto>(
        cache,
        mapper,
        logger,
        roleRepo)
{
    private readonly IPermissionRepository _permissionRepo = permissionRepo;
    private readonly IRepositoryBase<Group> _groupRepo = groupRepo;

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

    public override async Task<OperationResult> DeleteAsync(string id)
    {
        try
        {
            var result = await base.DeleteAsync(id);
            if (!result.Succeeded)
            {
                return result;
            }

            // Update groups that uses the deleted role
            var groupsWithRef = await _groupRepo.FindAsync(g => g.RoleIds.Contains(id));
            var updateTasks = groupsWithRef.Select(g =>
            {
                g.RoleIds = g.RoleIds.Where(roleId => roleId != id).ToList();
                return _groupRepo.UpdateAsync(g);
            });

            await Task.WhenAll(updateTasks);

            this.InvalidateCache(this.CacheName);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error deleting data: {message}", ex.Message);
            return OperationResult.Failure("Error when deleting data.");
        }
    }
}
