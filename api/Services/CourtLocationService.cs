using System;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.CourtLocation;

namespace Scv.Api.Services;

public interface ICourtLocationService : ICrudService<CourtLocationDto>
{
    Task<OperationResult<CourtLocationDto>> GetCourtLocationByCodeAsync(string code);
}

public class CourtLocationService(
    IAppCache cache,
    IMapper mapper,
    ILogger<CourtLocationService> logger,
    IRepositoryBase<CourtLocation> courtLocationRepo) : CrudServiceBase<IRepositoryBase<CourtLocation>, CourtLocation, CourtLocationDto>(
        cache,
        mapper,
        logger,
        courtLocationRepo), ICourtLocationService
{
    public override string CacheName => nameof(CourtLocationService);

    public override Task<OperationResult<CourtLocationDto>> ValidateAsync(CourtLocationDto dto, bool isEdit = false)
        => Task.FromResult(OperationResult<CourtLocationDto>.Success(dto));

    public async Task<OperationResult<CourtLocationDto>> GetCourtLocationByCodeAsync(string code)
    {
        try
        {
            var courtLocations = await this.GetDataFromCache(
                $"{this.CacheName}-{code}",
                () => this.Repo.FindAsync(cl => cl.Code == code));

            var courtLocation = courtLocations?.FirstOrDefault();

            return courtLocation == null
                ? OperationResult<CourtLocationDto>.Success(null)
                : OperationResult<CourtLocationDto>.Success(this.Mapper.Map<CourtLocationDto>(courtLocation));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "An error occurred while getting court location by code.");
            return OperationResult<CourtLocationDto>.Failure(ex.Message);
        }
    }
}
