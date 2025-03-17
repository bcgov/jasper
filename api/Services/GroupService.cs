﻿using System;
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

public class GroupService(
    IAppCache cache,
    IMapper mapper,
    ILogger<GroupService> logger,
    IRepositoryBase<Group> groupRepo,
    IRepositoryBase<Role> roleRepo,
    IRepositoryBase<User> userRepo
    ) : AccessControlManagementServiceBase<IRepositoryBase<Group>, Group, GroupDto>(
        cache,
        mapper,
        logger,
        groupRepo)
{
    private readonly IRepositoryBase<Role> _roleRepo = roleRepo;
    private readonly IRepositoryBase<User> _userRepo = userRepo;

    public override string CacheName => "GetGroupsAsync";

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
            foreach (var user in usersWithRef)
            {
                user.GroupIds = user.GroupIds.Where(roleId => roleId != id).ToList();
                await _userRepo.UpdateAsync(user);
            }

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
