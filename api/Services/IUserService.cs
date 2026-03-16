using Scv.Models.AccessControlManagement;
using System.Threading.Tasks;

namespace Scv.Api.Services
{
    public interface IUserService : ICrudService<UserDto>
    {
        Task<UserDto> GetWithPermissionsAsync(string email);
        Task<UserDto> GetByGuidWithPermissionsAsync(string guid);
        Task<UserDto> GetByIdWithPermissionsAsync(string userId);
        Task<UserDto> GetByJudgeIdAsync(int judgeId);
    }
}