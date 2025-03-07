using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LazyCache;
using Scv.Api.Models.UserManagement;
using Scv.Db.Repositories;


namespace Scv.Api.Services;

public interface IPermissionService
{
    Task<List<PermissionDto>> GetPermissionsAsync();
    Task<PermissionDto> GetPermissionByIdAsync(string id);
    Task<PermissionDto> UpdatePermissionAsync(string id, PermissionUpdateDto permissionDto);
}

public class PermissionService(
    IAppCache cache,
    IMapper mapper,
    IPermissionRepository permissionRepo
    ) : ServiceBase(cache), IPermissionService
{
    private readonly IMapper _mapper = mapper;
    private readonly IPermissionRepository _permissionRepo = permissionRepo;

    public async Task<List<PermissionDto>> GetPermissionsAsync() =>
        await this.GetDataFromCache(
            "GetPermissionsAsync",
            async () =>
            {
                var permissions = await _permissionRepo.GetActivePermissionsAsync();
                return _mapper.Map<List<PermissionDto>>(permissions);
            });

    public async Task<PermissionDto> GetPermissionByIdAsync(string id)
    {
        var permission = await _permissionRepo.GetByIdAsync(id);

        return _mapper.Map<PermissionDto>(permission);
    }

    public async Task<PermissionDto> UpdatePermissionAsync(string id, PermissionUpdateDto permissionDto)
    {
        var permission = await _permissionRepo.GetByIdAsync(id);

        permission.Description = permissionDto.Description;
        permission.IsActive = permissionDto.IsActive.GetValueOrDefault();

        await _permissionRepo.UpdateAsync(permission);

        return _mapper.Map<PermissionDto>(permission);
    }
}
