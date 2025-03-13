using System.Linq;
using AutoMapper;
using Scv.Api.Models.UserManagement;
using Scv.Db.Models;

namespace Scv.Api.Mappers;

public class UserManagementProfile : Profile
{
    public UserManagementProfile()
    {
        // Permision
        CreateMap<Permission, PermissionDto>();
        CreateMap<PermissionDto, Permission>()
            .ForMember(dest => dest.Code, opt => opt.Ignore())
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Role
        CreateMap<Role, RoleDto>();
        CreateMap<RoleDto, Role>()
            .ForMember(dest => dest.PermissionIds, opt => opt.MapFrom(src => src.PermissionIds.Distinct().ToList()))
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
