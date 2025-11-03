using Scv.Api.Services;
using Scv.Models.AccessControlManagement;
using System.Threading.Tasks;

public interface IUserService : ICrudService<UserDto>
{
    Task<UserDto> GetWithPermissionsAsync(string email);
    Task<UserDto> GetByIdWithPermissionsAsync(string userId);
}