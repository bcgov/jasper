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
    public class PcssSyncService(
        IAuthorizationServicesClient pcssAuthServiceClient,
        AuthorizationService authorizationService,
        IGroupService groupService,
        IDashboardService dashboardService,
        ILogger<PcssSyncService> logger)
    {
        private readonly IAuthorizationServicesClient _pcssAuthServiceClient = pcssAuthServiceClient;
        private readonly AuthorizationService _authorizationService = authorizationService;
        private readonly IGroupService _groupService = groupService;
        private readonly IDashboardService _dashboardService = dashboardService;
        private readonly ILogger<PcssSyncService> _logger = logger;

        public async Task<bool> UpdateUserFromPcss(UserDto userDto)
        {
            if (string.IsNullOrEmpty(userDto.NativeGuid))
            {
                _logger.LogInformation("User {Email} has no ProvJud GUID, cannot map to PCSS", userDto.Email);
                return false;
            }

            try
            {
                var matchingUser = await GetMatchingPcssUserAsync(userDto.NativeGuid, userDto.Email);
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

                return ApplyUserChanges(userDto, groupIds, judgeId, matchingUser.Email);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to get user or groups from PCSS for {Email}.", userDto.Email);
                return false;
            }
        }

        private async Task<UserItem> GetMatchingPcssUserAsync(string nativeGuid, string email)
        {
            _logger.LogInformation("Requesting user information from PCSS for {Email}.", email);
            var pcssUsers = await _pcssAuthServiceClient.GetUsersAsync();
            var matchingUser = pcssUsers?.FirstOrDefault(u => u.GUID == nativeGuid);

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
            var judges = await _dashboardService.GetJudges();
            var judge = judges.FirstOrDefault(j => j.UserId == pcssUserId);

            if (judge != null)
            {
                _logger.LogInformation("Mapped user {Email} to judge PersonId {PersonId}", email, judge.PersonId);
                return judge.PersonId;
            }

            _logger.LogInformation("No judge mapping found for user {Email} with PCSS UserId {UserId}", email, pcssUserId);
            return null;
        }

        private bool ApplyUserChanges(UserDto userDto, List<string> groupIds, int? judgeId, string email)
        {
            var isActive = groupIds.Count > 0;
            var hasChanges = false;

            if (userDto.JudgeId != judgeId)
            {
                userDto.JudgeId = judgeId;
                hasChanges = true;
            }

            if (email != null && userDto.Email != email)
            {
                userDto.Email = email;
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
