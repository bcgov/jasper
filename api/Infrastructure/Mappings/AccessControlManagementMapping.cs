using System.Collections.Generic;
using Mapster;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Models.AccessControlManagement;
using Scv.Models.Configuration;

namespace Scv.Api.Infrastructure.Mappings;

public class AccessControlManagementMapping : IRegister
{
    public static void Register(TypeAdapterConfig config)
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

        config.NewConfig<User, UserDto>()
            .Map(dest => dest.RoleIds, src => src.RoleIds ?? new List<string>());

        config.NewConfig<UserDto, User>()
            .Map(dest => dest.GroupIds, src => src.GroupIds.DistinctList())
            .Map(dest => dest.RoleIds, src => src.RoleIds.DistinctList())
            .AfterMapping((src, dest) =>
            {
                if (!string.IsNullOrEmpty(src.Email))
                    dest.Email = src.Email.ToLower();
            })
            .IgnoreNullValues(true);

        config.NewConfig<Constant, ConstantDto>();

        config.NewConfig<ConstantDto, Constant>()
            .Map(dest => dest.Values, src => src.Values.DistinctList())
            .IgnoreNullValues(true);
    }

    void IRegister.Register(TypeAdapterConfig config)
    {
        Register(config);
    }
}
