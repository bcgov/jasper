using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.CourtLocation;

namespace Scv.Api.Services;

public class CourtLocationService(
    IAppCache cache,
    IMapper mapper,
    ILogger<CourtLocationService> logger,
    IRepositoryBase<CourtLocation> courtLocationRepo) : CrudServiceBase<IRepositoryBase<CourtLocation>, CourtLocation, CourtLocationDto>(
        cache,
        mapper,
        logger,
        courtLocationRepo)
{
    public override string CacheName => nameof(CourtLocationService);

    public override Task<OperationResult<CourtLocationDto>> ValidateAsync(CourtLocationDto dto, bool isEdit = false)
        => Task.FromResult(OperationResult<CourtLocationDto>.Success(dto));
}
