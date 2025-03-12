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
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Role
        CreateMap<Role, RoleDto>();
        CreateMap<Role, RoleUpdateDto>();
        CreateMap<RoleDto, Role>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<RoleCreateDto, Role>()
            .ForMember(dest => dest.PermissionIds, opt => opt.MapFrom(src => src.PermissionIds.Distinct().ToList()))
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<RoleUpdateDto, Role>()
            .ForMember(dest => dest.PermissionIds, opt => opt.MapFrom(src => src.PermissionIds.Distinct().ToList()))
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}
