using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AccessControlManagement;

namespace Scv.Api.Services;

public interface IRoleService
{
    Task<OperationResult<IEnumerable<RoleDto>>> GetRolesByAliases(IEnumerable<string> aliases);
}

public class RoleService(
    IAppCache cache,
    IMapper mapper,
    ILogger<RoleService> logger,
    IRepositoryBase<Role> roleRepo,
    IPermissionRepository permissionRepo,
    IRepositoryBase<Group> groupRepo,
    IRepositoryBase<RoleAlias> roleAliasRepo
    ) : CrudServiceBase<IRepositoryBase<Role>, Role, RoleDto>(
        cache,
        mapper,
        logger,
        roleRepo), IRoleService
{
    private readonly IPermissionRepository _permissionRepo = permissionRepo;
    private readonly IRepositoryBase<Group> _groupRepo = groupRepo;
    private readonly IRepositoryBase<RoleAlias> _roleAliasRepo = roleAliasRepo;

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
                g.RoleIds = [.. g.RoleIds.Where(roleId => roleId != id)];
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

    public async Task<OperationResult<IEnumerable<RoleDto>>> GetRolesByAliases(IEnumerable<string> aliases)
    {
        var errors = new List<string>();
        var roleAliases = await _roleAliasRepo.FindAsync(ra => aliases.Contains(ra.Name));
        Logger.LogDebug("Found {RoleAliases} in JASPER based on input aliases: {Aliases}", JsonConvert.SerializeObject(roleAliases), aliases);

        if (!roleAliases.Any())
        {
            errors.Add("Role alias not found.");
        }
        else
        {
            var roleIds = roleAliases.Select(ra => ra.RoleId).Distinct().Where(r => r != null).ToArray();
            var roles = await this.Repo.FindAsync(r => roleIds.Contains(r.Id));

            if (roleIds.Count() != roles.Count())
            {
                Logger.LogError("One or more role aliases do not have a corresponding role.");
            }

            return OperationResult<IEnumerable<RoleDto>>.Success(this.Mapper.Map<IEnumerable<RoleDto>>(roles));
        }
        return OperationResult<IEnumerable<RoleDto>>.Failure([.. errors]);
    }
}
