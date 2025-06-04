using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LazyCache;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure;
using Scv.Api.Models.AccessControlManagement;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;


public class PermissionService(
    IAppCache cache,
    IMapper mapper,
    ILogger<PermissionService> logger,
    IPermissionRepository permissionRepo
) : AccessControlManagementServiceBase<IPermissionRepository, Permission, PermissionDto>(
        cache,
        mapper,
        logger,
        permissionRepo)
{
    public override string CacheName => "GetPermissionsAsync";

    public override async Task<List<PermissionDto>> GetAllAsync() =>
        await this.GetDataFromCache(
            this.CacheName,
            async () =>
            {
                var permissions = await this.Repo.GetActivePermissionsAsync();
                return this.Mapper.Map<List<PermissionDto>>(permissions);
            });
    public override async Task<OperationResult<PermissionDto>> ValidateAsync(PermissionDto dto, bool isEdit = false)
    {
        var errors = new List<string>();

        if (await this.Repo.GetByIdAsync(dto.Id) == null)
        {
            errors.Add("Permission ID is not found.");
        }

        return errors.Count != 0
            ? OperationResult<PermissionDto>.Failure([.. errors])
            : OperationResult<PermissionDto>.Success(dto);
    }
}
