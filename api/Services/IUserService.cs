using System.Threading.Tasks;
using Scv.Models.AccessControlManagement;

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