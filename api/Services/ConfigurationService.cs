using System.Collections.Generic;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure;
using Scv.Api.Models.Configuration;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public interface IConfigurationService : ICrudService<ConstantDto>
{
    Task<IEnumerable<ConstantDto>> GetConfigurationAsync();
}

public class ConfigurationService(
    IAppCache cache,
    IMapper mapper,
    ILogger<ConfigurationService> logger,
    IRepositoryBase<Constant> constantRepo
) : CrudServiceBase<IRepositoryBase<Constant>, Constant, ConstantDto>(
        cache,
        mapper,
        logger,
        constantRepo), IConfigurationService
{
    public override string CacheName => nameof(ConfigurationService);

    public async Task<IEnumerable<ConstantDto>> GetConfigurationAsync()
    {
        Logger.LogInformation("Getting application configuration constants.");

        var constants = await GetDataFromCache(
            CacheName,
            async () => await Repo.GetAllAsync());

        return Mapper.Map<IEnumerable<ConstantDto>>(constants);
    }

    public override Task<OperationResult<ConstantDto>> ValidateAsync(ConstantDto dto, bool isEdit = false)
    {
        return Task.FromResult(OperationResult<ConstantDto>.Success(dto));
    }
}
