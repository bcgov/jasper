using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Scv.Core.Infrastructure;
using Scv.Models.AccessControlManagement;
using Scv.Models.Location;

namespace Scv.Api.Services
{
    public interface IUserService : ICrudService<UserDto>
    {
        Task<UserDto> GetWithPermissionsAsync(string email);
        Task<UserDto> GetByGuidWithPermissionsAsync(string guid);
        Task<UserDto> GetByIdWithPermissionsAsync(string userId);
        Task<UserDto> GetByJudgeIdAsync(int judgeId);
        Task<List<Location>> GetCourtCalendarLocations(ClaimsPrincipal user);
        Task<List<Location>> GetJudicialListingLocations(ClaimsPrincipal user);
        Task<List<Location>> GetRotaAdminLocations(ClaimsPrincipal user);
        Task<OperationResult<UserDto>> MarkReleaseNotesViewedAsync(string userId, string version, DateTime viewedAtUtc);
    }
}