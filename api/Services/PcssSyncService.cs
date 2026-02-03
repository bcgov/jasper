using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PCSSCommon.Clients.AuthorizationServices;
using Scv.Api.Models.AccessControlManagement;

namespace Scv.Api.Services
{
    public interface IPcssSyncService
    {
        public Task<bool> UpdateUserFromPcss(UserDto userDto, bool forceUpdateCache = false);
    }

    public class PcssSyncService(
        IPcssAuthorizationService authorizationService,
        IGroupService groupService,
        IJudgeService judgeService,
        ILogger<PcssSyncService> logger) : IPcssSyncService
    {
        private readonly IPcssAuthorizationService _authorizationService = authorizationService;
        private readonly IGroupService _groupService = groupService;
        private readonly IJudgeService _judgeService = judgeService;
        private readonly ILogger<PcssSyncService> _logger = logger;

        public async Task<bool> UpdateUserFromPcss(UserDto userDto, bool forceUpdateCache = false)
        {
            if (string.IsNullOrEmpty(userDto.NativeGuid))
            {
                _logger.LogInformation("User {Email} has no ProvJud GUID, cannot map to PCSS", userDto.Email);
                return false;
            }

            try
            {
                var matchingUser = await GetMatchingPcssUserAsync(userDto.NativeGuid, userDto.Email, forceUpdateCache);
                if (matchingUser == null)
                {
                    return false;
                }

                var groupIds = await GetGroupIdsForUserAsync(matchingUser.UserId.Value, userDto.Email);
                if (groupIds == null)
                {
                    return false;
                }

                var judgeId = await GetJudgeIdForUserAsync(matchingUser.UserId.Value, userDto.Email);

                return ApplyUserChanges(userDto, groupIds, judgeId, matchingUser);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to get user or groups from PCSS for {Email}.", userDto.Email);
                return false;
            }
        }

        private async Task<UserItem> GetMatchingPcssUserAsync(string nativeGuid, string email, bool forceUpdateCache = false)
        {
            _logger.LogInformation("Requesting user information from PCSS for {Email}.", email);
            var matchingUser = await _authorizationService.GetUserByGuid(nativeGuid, forceUpdateCache);

            _logger.LogDebug("PCSS user lookup by GUID {UserGuid} returned: {MatchingUser}",
                nativeGuid,
                matchingUser != null ? JsonConvert.SerializeObject(matchingUser) : "No match");

            if (matchingUser == null || !matchingUser.UserId.HasValue)
            {
                _logger.LogWarning("No matching PCSS user found for {Email} with GUID {UserGuid}", email, nativeGuid);
                return null;
            }

            return matchingUser;
        }

        private async Task<List<string>> GetGroupIdsForUserAsync(int pcssUserId, string email)
        {
            var roleNameResult = await _authorizationService.GetPcssUserRoleNames(pcssUserId);
            if (roleNameResult == null || roleNameResult.Errors.Any())
            {
                _logger.LogWarning("Failed to get PCSS roles for user {Email}: {Errors}",
                    email,
                    roleNameResult?.Errors != null ? string.Join(", ", roleNameResult.Errors) : "No result");
                return null;
            }

            var groupResult = await _groupService.GetGroupsByAliases(roleNameResult.Payload);
            if (groupResult == null || groupResult.Errors.Any())
            {
                _logger.LogWarning("Failed to get groups for user {Email}: {Errors}",
                    email,
                    groupResult?.Errors != null ? string.Join(", ", groupResult.Errors) : "No result");
                return null;
            }

            return groupResult.Payload.Select(g => g.Id).ToList();
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

        private bool ApplyUserChanges(UserDto userDto, List<string> groupIds, int? judgeId, UserItem user)
        {
            var isActive = groupIds.Count > 0;
            var hasChanges = false;

            if (userDto.JudgeId != judgeId)
            {
                userDto.JudgeId = judgeId;
                hasChanges = true;
            }

            if (userDto.FirstName != user.GivenName)
            {
                userDto.FirstName = user.GivenName;
                hasChanges = true;
            }

            if (userDto.LastName != user.Surname)
            {
                userDto.LastName = user.Surname;
                hasChanges = true;
            }

            if (userDto.Email != null && userDto.Email != user.Email)
            {
                userDto.Email = user.Email;
                hasChanges = true;
            }

            if (userDto.IsActive != isActive)
            {
                userDto.IsActive = isActive;
                hasChanges = true;
            }

            var currentGroupIds = userDto.GroupIds ?? new List<string>();
            if (!new HashSet<string>(currentGroupIds).SetEquals(groupIds))
            {
                userDto.GroupIds = groupIds;
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
    }
}
