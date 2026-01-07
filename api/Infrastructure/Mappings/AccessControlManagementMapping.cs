using Mapster;
using Scv.Api.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Models.AccessControlManagement;

namespace Scv.Api.Infrastructure.Mappings;

public class AccessControlManagementMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Permission, PermissionDto>();

        config.NewConfig<PermissionDto, Permission>()
            .Ignore(dest => dest.Code)
            .IgnoreNullValues(true);

        config.NewConfig<Role, RoleDto>();

        config.NewConfig<RoleDto, Role>()
            .Map(dest => dest.PermissionIds, src => src.PermissionIds.DistinctList())
            .IgnoreNullValues(true);

        config.NewConfig<Group, GroupDto>();

        config.NewConfig<GroupDto, Group>()
            .Map(dest => dest.RoleIds, src => src.RoleIds.DistinctList())
            .IgnoreNullValues(true);

        config.NewConfig<User, UserDto>();

        config.NewConfig<UserDto, User>()
            .Map(dest => dest.GroupIds, src => src.GroupIds.DistinctList())
            .AfterMapping((src, dest) =>
            {
                if (!string.IsNullOrEmpty(src.Email))
                    dest.Email = src.Email.ToLower();
            })
            .IgnoreNullValues(true);
    }
}
