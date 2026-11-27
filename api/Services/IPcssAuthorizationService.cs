using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Scv.Core.Infrastructure;
using PCSSAuthServices = PCSSCommon.Clients.AuthorizationServices;

namespace Scv.Api.Services
{
    public interface IPcssAuthorizationService
    {
        Task<ICollection<PCSSAuthServices.UserItem>> GetUsers();
        Task<ICollection<PCSSAuthServices.UserItem>> RefreshUsers(CancellationToken cancellationToken = default);
        Task<PcssUserMatch> GetUserByGuid(string userGuid, bool forceRefresh = false);
        Task<OperationResult<IEnumerable<string>>> GetPcssUserRoleNames(int userId, string expectedGuid = null);
        Task<OperationResult<PCSSAuthServices.UserItem>> GetPcssUserById(int userId);
    }

    public sealed record PcssUserMatch(PCSSAuthServices.UserItem User, bool UsedFallback = false);

    internal sealed class PcssFallbackIdentityMismatchException()
        : Exception("The live PCSS user identity contradicts the cached mapping.")
    {
    }
}
