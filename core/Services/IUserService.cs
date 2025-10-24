using Scv.Core.Services;
using Scv.Models.AccessControlManagement;

public interface IUserService : ICrudService<UserDto>
{
    Task<UserDto> GetWithPermissionsAsync(string email);
    Task<UserDto> GetByIdWithPermissionsAsync(string userId);
}