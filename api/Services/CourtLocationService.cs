using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Core.Infrastructure;
using Scv.Db.Contants;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.CourtLocation;

namespace Scv.Api.Services;

public interface ICourtLocationService : ICrudService<CourtLocationDto>
{
    Task<OperationResult<CourtLocationDto>> GetCourtLocationByCodeAsync(string code);
    Task<OperationResult> ReplaceCourtLocationsAsync(CourtLocationDto[] dtos);
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
        => throw new NotSupportedException();

    public override Task<OperationResult<CourtLocationDto>> AddAsync(CourtLocationDto dto)
        => throw new NotSupportedException();

    public override Task<OperationResult<CourtLocationDto>> AddRangeAsync(List<CourtLocationDto> dtos)
        => throw new NotSupportedException();

    public override Task<OperationResult<CourtLocationDto>> UpdateAsync(CourtLocationDto dto)
        => throw new NotSupportedException();

    public override Task<OperationResult> DeleteAsync(string id)
        => throw new NotSupportedException();

    public override Task<OperationResult> DeleteRangeAsync(List<string> ids)
        => throw new NotSupportedException();

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
            return OperationResult<CourtLocationDto>.Failure("Something went wrong when retrieving the court location.");
        }
    }

    public async Task<OperationResult> ReplaceCourtLocationsAsync(CourtLocationDto[] dtos)
    {
        try
        {
            var entities = this.Mapper.Map<CourtLocation[]>(dtos);
            await this.Repo.ReplaceAllAsync(CollectionNameConstants.COURT_LOCATIONS, entities);
            this.InvalidateCache(this.CacheName);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "An error occurred while replacing all court locations.");
            return OperationResult.Failure(ex.Message);
        }
    }
}
