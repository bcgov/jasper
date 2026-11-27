using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PCSSCommon.Clients.AuthorizationServices;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.AccessControlManagement;

namespace Scv.Api.Services
{
    public enum PcssSyncOutcome
    {
        Failed,
        Synchronized,
        NoAccess
    }

    public readonly record struct PcssSyncResult(PcssSyncOutcome Outcome, bool HasChanges = false);

    public interface IPcssSyncService
    {
        public Task<PcssSyncResult> UpdateUserFromPcss(UserDto userDto);
    }

    public class PcssSyncService(
        IPcssAuthorizationService authorizationService,
        IGroupService groupService,
        IJudgeService judgeService,
        IRepositoryBase<Group> groupRepo,
        IRepositoryBase<Role> roleRepo,
        IRoleService roleService,
        ILogger<PcssSyncService> logger) : IPcssSyncService
    {
        private readonly IPcssAuthorizationService _authorizationService = authorizationService;
        private readonly IGroupService _groupService = groupService;
        private readonly IJudgeService _judgeService = judgeService;
        private readonly IRepositoryBase<Group> _groupRepo = groupRepo;
        private readonly IRepositoryBase<Role> _roleRepo = roleRepo;
        private readonly IRoleService _roleService = roleService;
        private readonly ILogger<PcssSyncService> _logger = logger;

        public async Task<PcssSyncResult> UpdateUserFromPcss(UserDto userDto)
        {
            if (string.IsNullOrWhiteSpace(userDto.NativeGuid))
            {
                _logger.LogInformation("User {Email} has no ProvJud GUID, cannot map to PCSS", userDto.Email);
                return new(PcssSyncOutcome.NoAccess);
            }

            try
            {
                var matchingUser = await GetMatchingPcssUserAsync(userDto.NativeGuid, userDto.Email);
                List<string> groupIds = [];
                List<string> roleIds = [];
                var judgeId = userDto.JudgeId;

                if (matchingUser != null)
                {
                    if (matchingUser.UserId is not int pcssUserId)
                    {
                        _logger.LogWarning("The PCSS user matching {Email} has no valid user ID", userDto.Email);
                        return new(PcssSyncOutcome.Failed);
                    }

                    var roleNames = await GetPcssRoleNamesAsync(pcssUserId, userDto.Email);
                    if (roleNames == null)
                    {
                        return new(PcssSyncOutcome.Failed);
                    }

                    if (roleNames.Any())
                    {
                        groupIds = await GetGroupIdsForUserAsync(roleNames, userDto.Email);
                        if (groupIds == null)
                        {
                            return new(PcssSyncOutcome.Failed);
                        }

                        roleIds = await GetRoleIdsForUserAsync(roleNames, userDto.Email);
                        if (roleIds == null)
                        {
                            return new(PcssSyncOutcome.Failed);
                        }
                    }

                    judgeId = await GetJudgeIdForUserAsync(pcssUserId, userDto.Email);
                }

                var outcome = groupIds.Count > 0 ? PcssSyncOutcome.Synchronized : PcssSyncOutcome.NoAccess;
                if (outcome == PcssSyncOutcome.NoAccess)
                {
                    roleIds.Clear();
                }

                // For testing manually-assigned groups/roles that are used for Transitory Documents.
                var currentGroupIds = userDto.GroupIds ?? [];
                _logger.LogDebug("Current group ids: {CurrentGroupIds}, PCSS group ids: {PcssGroupIds} for user {Email}",
                    currentGroupIds, groupIds, userDto.Email);
                if (currentGroupIds.Count > 0)
                {
                    var testingGroupId = (await _groupRepo.FindAsync(g => g.Name == Group.TESTING))
                        .FirstOrDefault()
                        ?.Id;
                    var developerRoleId = (await _roleRepo.FindAsync(g => g.Name == Role.DEVELOPER))
                        .FirstOrDefault()
                        ?.Id;

                    if (!string.IsNullOrWhiteSpace(testingGroupId) && !string.IsNullOrWhiteSpace(developerRoleId)
                        && currentGroupIds.Contains(testingGroupId)
                        && !groupIds.Contains(testingGroupId))
                    {
                        groupIds.Add(testingGroupId);
                        // With group-inherited roles, TESTING already supplies DEVELOPER.
                        if (roleIds.Count > 0 || outcome == PcssSyncOutcome.NoAccess)
                        {
                            roleIds.Add(developerRoleId);
                        }
                    }
                }

                return new(outcome, ApplyUserChanges(userDto, groupIds, roleIds, judgeId, matchingUser));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to get user or groups from PCSS for {Email}.", userDto.Email);
                return new(PcssSyncOutcome.Failed);
            }
        }

        private async Task<UserItem> GetMatchingPcssUserAsync(string nativeGuid, string email)
        {
            _logger.LogInformation("Requesting user information from PCSS for {Email}.", email);
            var matchingUser = await _authorizationService.GetUserByGuid(nativeGuid, forceRefresh: true);

            _logger.LogDebug("PCSS user lookup by GUID {UserGuid} returned: {MatchingUser}",
                nativeGuid,
                matchingUser != null ? JsonConvert.SerializeObject(matchingUser) : "No match");

            if (matchingUser == null)
            {
                _logger.LogWarning("No matching PCSS user found for {Email} with GUID {UserGuid}", email, nativeGuid);
            }

            return matchingUser;
        }

        private Task<List<string>> GetGroupIdsForUserAsync(IEnumerable<string> roleNames, string email) =>
            GetIdsForUserAsync(roleNames, email, "groups", _groupService.GetGroupsByAliases, g => g.Id);

        private Task<List<string>> GetRoleIdsForUserAsync(IEnumerable<string> roleNames, string email) =>
            GetIdsForUserAsync(roleNames, email, "roles", _roleService.GetRolesByAliases, r => r.Id);

        private async Task<IEnumerable<string>> GetPcssRoleNamesAsync(int pcssUserId, string email)
        {
            var roleNameResult = await _authorizationService.GetPcssUserRoleNames(pcssUserId);
            if (roleNameResult?.Succeeded != true || roleNameResult.Payload == null)
            {
                _logger.LogWarning("Failed to get PCSS roles for user {Email}: {Errors}",
                    email,
                    roleNameResult?.Errors != null ? string.Join(", ", roleNameResult.Errors) : "No result");
                return null;
            }

            return roleNameResult.Payload;
        }

        private async Task<int?> GetJudgeIdForUserAsync(int pcssUserId, string email)
        {
            var judges = await _judgeService.GetJudges();
            var judge = judges.FirstOrDefault(j => j.UserId == pcssUserId);

            if (judge != null)
            {
                _logger.LogInformation("Mapped user {Email} to judge PersonId {PersonId}", email, judge.PersonId);
                return judge.PersonId;
            }

            _logger.LogInformation("No judge mapping found for user {Email} with PCSS UserId {UserId}", email, pcssUserId);
            return null;
        }

        private bool ApplyUserChanges(UserDto userDto, List<string> groupIds, List<string> roleIds, int? judgeId, UserItem user)
        {
            var isActive = groupIds.Count > 0;
            var hasChanges = false;

            if (userDto.JudgeId != judgeId)
            {
                userDto.JudgeId = judgeId;
                hasChanges = true;
            }

            if (user?.GivenName != null && userDto.FirstName != user.GivenName)
            {
                userDto.FirstName = user.GivenName;
                hasChanges = true;
            }

            if (user?.Surname != null && userDto.LastName != user.Surname)
            {
                userDto.LastName = user.Surname;
                hasChanges = true;
            }

            if (user?.Email != null && userDto.Email != null && userDto.Email != user.Email)
            {
                userDto.Email = user.Email;
                hasChanges = true;
            }

            if (userDto.IsActive != isActive)
            {
                userDto.IsActive = isActive;
                hasChanges = true;
            }

            if (user != null && userDto.IsPendingRegistration != false)
            {
                userDto.IsPendingRegistration = false;
                hasChanges = true;
            }

            var currentGroupIds = userDto.GroupIds ?? [];
            if (!new HashSet<string>(currentGroupIds).SetEquals(groupIds))
            {
                userDto.GroupIds = groupIds;
                hasChanges = true;
            }

            var currentRoleIds = userDto.RoleIds ?? [];
            if (!new HashSet<string>(currentRoleIds).SetEquals(roleIds))
            {
                userDto.RoleIds = roleIds;
                hasChanges = true;
            }

            if (hasChanges)
            {
                _logger.LogInformation("Updated user {Email} with {GroupCount} groups and judgeId {JudgeId}",
                    userDto.Email, groupIds.Count, userDto.JudgeId);
                return true;
            }

            _logger.LogDebug("No changes detected for user {Email} from PCSS update.", userDto.Email);
            return false;
        }

        private async Task<List<string>> GetIdsForUserAsync<T>(
            IEnumerable<string> roleNames,
            string email,
            string entityName,
            Func<IEnumerable<string>, Task<OperationResult<IEnumerable<T>>>> getByAliases,
            Func<T, string> getId)
        {
            var result = await getByAliases(roleNames);
            if (result?.Succeeded != true || result.Payload == null)
            {
                _logger.LogWarning("Failed to get {EntityName} for user {Email}: {Errors}",
                    entityName,
                    email,
                    result?.Errors != null ? string.Join(", ", result.Errors) : "No result");
                return null;
            }

            return [.. result.Payload.Select(getId)];
        }
    }
}
