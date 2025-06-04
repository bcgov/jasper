using System.Collections.Generic;
using System.Collections.Immutable;
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


public interface IUserService : IAccessControlManagementService<UserDto>
{
    Task<UserDto> GetWithPermissionsAsync(string email);
}

public class UserService(
    IAppCache cache,
    IMapper mapper,
    ILogger<UserService> logger,
    IRepositoryBase<User> userRepo,
    IRepositoryBase<Group> groupRepo,
    IRepositoryBase<Role> roleRepo,
    IPermissionRepository permissionRepo
) : AccessControlManagementServiceBase<IRepositoryBase<User>, User, UserDto>(
        cache,
        mapper,
        logger,
        userRepo), IUserService
{
    private readonly IRepositoryBase<Group> _groupRepo = groupRepo;
    private readonly IRepositoryBase<Role> _roleRepo = roleRepo;
    private readonly IPermissionRepository _permissionRepo = permissionRepo;

    public override string CacheName => "GetUsersAsync";

    public override async Task<OperationResult<UserDto>> ValidateAsync(UserDto dto, bool isEdit = false)
    {
        var errors = new List<string>();

        var userNotExist = await this.Repo.GetByIdAsync(dto.Id);
        if (isEdit && userNotExist == null)
        {
            errors.Add("User ID is not found.");
        }

        // When editing, only throw error if the email belongs to a different user
        var user = (await this.Repo.FindAsync(u => u.Email == dto.Email)).SingleOrDefault();
        if (user != null && (!isEdit || user.Id != dto.Id))
        {
            errors.Add("Email address is already used.");
        }

        // Check if group ids are all valid
        var existingGroupIds = (await _groupRepo.GetAllAsync()).Select(p => p.Id);
        if (!dto.GroupIds.All(id => existingGroupIds.Contains(id)))
        {
            errors.Add("Found one or more invalid group IDs.");
        }

        return errors.Count != 0
            ? OperationResult<UserDto>.Failure([.. errors])
            : OperationResult<UserDto>.Success(dto);
    }

    public async Task<UserDto> GetWithPermissionsAsync(string email)
    {
        var result = await this.Repo.FindAsync(u => u.Email == email);

        // Check if user exists
        if (result == null || !result.Any())
        {
            this.Logger.LogInformation("User with email: {email} is not found", email);
            return null;
        }

        var user = this.Mapper.Map<UserDto>(result.Single());

        // Find user's groups
        if (user.GroupIds.Count == 0)
        {
            this.Logger.LogInformation("User does not have any Groups");
            return user;
        }

        // Get all role ids from groups
        var roleIds = (await _groupRepo.FindAsync(g => user.GroupIds.Contains(g.Id))).SelectMany(g => g.RoleIds);
        if (!roleIds.Any())
        {
            this.Logger.LogInformation("User's group(s) does not have any Roles.");
            return user;
        }

        // Get all permission codes
        var permissionIds = (await _roleRepo.FindAsync(r => roleIds.Contains(r.Id))).SelectMany(r => r.PermissionIds);
        if (!permissionIds.Any())
        {
            this.Logger.LogInformation("Role does not have any Permissions.");
            return user;
        }

        var permissions = (await _permissionRepo.FindAsync(p => permissionIds.Contains(p.Id))).Select(p => p.Code).ToList();
        var roles = (await _roleRepo.FindAsync(r => roleIds.Contains(r.Id))).Select(r => r.Name).ToList();
        user.Permissions = permissions;
        user.Roles = roles;

        return user;
    }
}
