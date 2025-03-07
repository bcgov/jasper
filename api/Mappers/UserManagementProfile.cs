using AutoMapper;
using Scv.Api.Models.UserManagement;
using Scv.Db.Models;

namespace Scv.Api.Mappers;

public class UserManagementProfile : Profile
{
    public UserManagementProfile()
    {
        CreateMap<Permission, PermissionDto>().ReverseMap();
    }
}
