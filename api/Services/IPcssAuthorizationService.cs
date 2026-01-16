using System.Collections.Generic;
using System.Threading.Tasks;
using Scv.Api.Infrastructure;
using PCSSAuthServices = PCSSCommon.Clients.AuthorizationServices;

namespace Scv.Api.Services
{
    public interface IPcssAuthorizationService
    {
        Task<ICollection<PCSSAuthServices.UserItem>> GetUsers();
        Task<PCSSAuthServices.UserItem> GetUserByGuid(string userGuid, bool forceRefresh = false);
        Task<OperationResult<IEnumerable<string>>> GetPcssUserRoleNames(int userId);
        Task<OperationResult<PCSSAuthServices.UserItem>> GetPcssUserById(int userId);
    }
}
