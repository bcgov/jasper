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

public interface IGroupService
{
    Task<OperationResult<IEnumerable<GroupDto>>> GetGroupsByAliases(IEnumerable<string> aliases);
}

public class GroupService(
    IAppCache cache,
    IMapper mapper,
    ILogger<GroupService> logger,
    IRepositoryBase<Group> groupRepo,
    IRepositoryBase<Role> roleRepo,
    IRepositoryBase<User> userRepo,
    IRepositoryBase<GroupAlias> groupAliasRepo
    ) : CrudServiceBase<IRepositoryBase<Group>, Group, GroupDto>(
        cache,
        mapper,
        logger,
        groupRepo), IGroupService
{
    private readonly IRepositoryBase<Role> _roleRepo = roleRepo;
    private readonly IRepositoryBase<User> _userRepo = userRepo;
    private readonly IRepositoryBase<GroupAlias> _groupAliasRepo = groupAliasRepo;

    public override string CacheName => "GetGroupsAsync";

    public async Task<OperationResult<IEnumerable<GroupDto>>> GetGroupsByAliases(IEnumerable<string> aliases)
    {
        var groupAliases = await this._groupAliasRepo.FindAsync(ga => aliases.Contains(ga.Name));
        Logger.LogDebug("Found {GroupAliases} in JASPER based on input aliases: {Aliases}", JsonConvert.SerializeObject(groupAliases), aliases);

        var missingAlias = GroupAlias.ALL_GROUP_ALIASES.FirstOrDefault(seed =>
            aliases.Contains(seed.Name) && !groupAliases.Any(alias => alias.Name == seed.Name));
        if (missingAlias != null)
        {
            Logger.LogError("Seeded group alias {Alias} is missing.", missingAlias.Name);
            return OperationResult<IEnumerable<GroupDto>>.Failure("A seeded group alias is missing.");
        }

        // The seeder assigns a group to every built-in alias. Custom aliases may be intentionally unmapped.
        if (groupAliases.Any(alias => alias.GroupId == null && GroupAlias.ALL_GROUP_ALIASES.Any(seed => seed.Name == alias.Name)))
        {
            Logger.LogError("A seeded group alias is missing its target.");
            return OperationResult<IEnumerable<GroupDto>>.Failure("A seeded group alias is missing its target.");
        }

        var groupIds = groupAliases.Select(alias => alias.GroupId).Where(id => id != null).Distinct().ToArray();
        if (groupIds.Length == 0)
        {
            return OperationResult<IEnumerable<GroupDto>>.Success([]);
        }

        var groups = await this.Repo.FindAsync(g => groupIds.Contains(g.Id));
        if (groupIds.Except(groups.Select(group => group.Id)).Any())
        {
            Logger.LogError("One or more group aliases do not have a corresponding group.");
            return OperationResult<IEnumerable<GroupDto>>.Failure("One or more group aliases do not have a corresponding group.");
        }

        return OperationResult<IEnumerable<GroupDto>>.Success(this.Mapper.Map<IEnumerable<GroupDto>>(groups));
    }

    public override async Task<OperationResult<GroupDto>> ValidateAsync(GroupDto dto, bool isEdit = false)
    {
        var errors = new List<string>();

        if (isEdit && await this.Repo.GetByIdAsync(dto.Id) == null)
        {
            errors.Add("Group ID is not found.");
        }

        // Check if role ids are all valid
        var existingRoleIds = (await _roleRepo.GetAllAsync()).Select(p => p.Id);
        if (!dto.RoleIds.All(id => existingRoleIds.Contains(id)))
        {
            errors.Add("Found one or more invalid role IDs.");
        }

        return errors.Count != 0
            ? OperationResult<GroupDto>.Failure([.. errors])
            : OperationResult<GroupDto>.Success(dto);
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

            // Update users that uses the deleted group
            var usersWithRef = await _userRepo.FindAsync(g => g.GroupIds.Contains(id));
            var updateTasks = usersWithRef.Select(u =>
            {
                u.GroupIds = [.. u.GroupIds.Where(roleId => roleId != id)];
                return _userRepo.UpdateAsync(u);
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
