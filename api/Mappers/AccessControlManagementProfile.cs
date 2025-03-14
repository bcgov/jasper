using System.Linq;
using AutoMapper;
using Scv.Api.Models.AccessControlManagement;
using Scv.Db.Models;

namespace Scv.Api.Mappers;

public class AccessControlManagementProfile : Profile
{
    public AccessControlManagementProfile()
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
