using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using PCSSCommon.Clients.PersonServices;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Models.Location;
using Scv.Db.Models;
using Scv.Db.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Scv.Api.Services;

public interface IUserService : ICrudService<UserDto>
{
    Task<UserDto> GetWithPermissionsAsync(string email);
    Task<UserDto> GetByGuidWithPermissionsAsync(string guid);
    Task<UserDto> GetByIdWithPermissionsAsync(string userId);
    Task<UserDto> GetByJudgeIdAsync(int judgeId);
    Task<List<Location>> GetCourtCalendarLocations(ClaimsPrincipal user);
    Task<List<Location>> GetJudicialListingLocations(ClaimsPrincipal user);
    Task<List<Location>> GetRotaAdminLocations(ClaimsPrincipal user);
}

public class UserService(
    IAppCache cache,
    IMapper mapper,
    ILogger<UserService> logger,
    IRepositoryBase<User> userRepo,
    IRepositoryBase<Group> groupRepo,
    IRepositoryBase<Role> roleRepo,
    IPermissionRepository permissionRepo,
    LocationService locationService
) : CrudServiceBase<IRepositoryBase<User>, User, UserDto>(
        cache,
        mapper,
        logger,
        userRepo), IUserService
{
    private readonly IRepositoryBase<Group> _groupRepo = groupRepo;
    private readonly IRepositoryBase<Role> _roleRepo = roleRepo;
    private readonly IPermissionRepository _permissionRepo = permissionRepo;
    private readonly LocationService _locationService = locationService;

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
        if (result == null || !result.Any())
        {
            this.Logger.LogInformation("User with email: {Email} is not found", email.Replace(Environment.NewLine, ""));
            return null;
        }

        return await PopulateUserPermissionsAndRolesAsync(this.Mapper.Map<UserDto>(result.Single()));
    }

    public async Task<UserDto> GetByGuidWithPermissionsAsync(string guid)
    {
        var result = await this.Repo.FindAsync(u => u.NativeGuid == guid);
        if (result == null || !result.Any())
        {
            this.Logger.LogInformation("User with guid: {Guid} is not found", guid.Replace(Environment.NewLine, ""));
            return null;
        }

        return await PopulateUserPermissionsAndRolesAsync(this.Mapper.Map<UserDto>(result.Single()));
    }

    public async Task<UserDto> GetByIdWithPermissionsAsync(string userId)
    {
        var user = await GetByIdAsync(userId);
        if (user == null)
        {
            this.Logger.LogInformation("User with id: {UserId} is not found", userId);
            return null;
        }

        return await PopulateUserPermissionsAndRolesAsync(user);
    }

    private async Task<UserDto> PopulateUserPermissionsAndRolesAsync(UserDto user)
    {
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

    public async Task<UserDto> GetByJudgeIdAsync(int judgeId)
    {
        var result = await this.Repo.FindAsync(u => u.JudgeId == judgeId);
        if (result == null || !result.Any())
        {
            this.Logger.LogInformation("User with judge id: {JudgeId} is not found", judgeId);
            return null;
        }

        return Mapper.Map<UserDto>(result.Single());
    }

    public async Task<List<Location>> GetCourtCalendarLocations(ClaimsPrincipal user)
    {
        if (!TryGetHomeLocationIdValue(user, out var homeLocationIdValue))
        {
            return [];
        }

        var homeLocations = await GetUserHomeLocations(homeLocationIdValue);
        return await GetLocationsForPermissions(
            user,
            Permission.COURT_CALENDAR_ACTIVITY_REGION,
            Permission.COURT_CALENDAR_ACTIVITY_PROVINCE,
            homeLocationIdValue,
            homeLocations);
    }

    public async Task<List<Location>> GetJudicialListingLocations(ClaimsPrincipal user)
    {
        if (!TryGetHomeLocationIdValue(user, out var homeLocationIdValue))
        {
            return [];
        }

        var homeLocations = await GetUserHomeLocations(homeLocationIdValue);
        return await GetLocationsForPermissions(
            user,
            Permission.JUDICIAL_LISTING_ACTIVITY_REGION,
            Permission.JUDICIAL_LISTING_ACTIVITY_PROVINCE,
            homeLocationIdValue,
            homeLocations);
    }

    public async Task<List<Location>> GetRotaAdminLocations(ClaimsPrincipal user)
    {
        if (!TryGetHomeLocationIdValue(user, out var homeLocationIdValue))
        {
            return [];
        }

        var homeLocations = await GetUserHomeLocations(homeLocationIdValue);
        return await GetLocationsForPermissions(
            user,
            Permission.ROTA_ADMIN_REGION,
            Permission.ROTA_ADMIN_PROVINCE,
            homeLocationIdValue,
            homeLocations);
    }

    private async Task<List<Location>> GetLocationsForPermissions(
        ClaimsPrincipal user,
        string regionPermission,
        string provincePermission,
        string homeLocationIdValue,
        List<Location> homeLocations)
    {
        var permissions = user.Permissions();

        if (permissions.Contains(regionPermission))
        {
            return await GetUserRegionLocations(homeLocationIdValue);
        }

        if (permissions.Contains(provincePermission))
        {
            return (await _locationService.GetLocations()).ToList();
        }

        return homeLocations;
    }

    private async Task<List<Location>> GetUserRegionLocations(string homeLocationIdValue)
    {
        var locations = await _locationService.GetLocations();
        var homeLocation = locations.FirstOrDefault(loc => loc.LocationId == homeLocationIdValue);
        if (homeLocation == null)
        {
            return locations.ToList();
        }

        var regionCd = homeLocation.RegionCd;
        if (string.IsNullOrWhiteSpace(regionCd))
        {
            return locations.ToList();
        }

        return locations
            .Where(loc => !string.IsNullOrWhiteSpace(loc.RegionCd) &&
                string.Equals(loc.RegionCd, regionCd, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<List<Location>> GetUserHomeLocations(string homeLocationIdValue)
    {
        var locations = await _locationService.GetLocations();
        return locations
            .Where(loc => loc.LocationId == homeLocationIdValue)
            .ToList();
    }

    private static bool TryGetHomeLocationIdValue(ClaimsPrincipal user, out string homeLocationIdValue)
    {
        homeLocationIdValue = null;

        var homeLocationId = user?.JudgeHomeLocationId();
        if (homeLocationId == null || homeLocationId == 0)
        {
            return false;
        }

        homeLocationIdValue = homeLocationId.ToString();
        return true;
    }

}
