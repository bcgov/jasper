using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public class ReservedJudgementService(
    IAppCache cache,
    IMapper mapper,
    ILogger<ReservedJudgementService> logger,
    IRepositoryBase<ReservedJudgement> judgementRepo) : CrudServiceBase<IRepositoryBase<ReservedJudgement>, ReservedJudgement, ReservedJudgementDto>(
        cache,
        mapper,
        logger,
        judgementRepo)
{
    public override string CacheName => "GetReservedJudgementsAsync";

    public override Task<OperationResult<ReservedJudgementDto>> ValidateAsync(ReservedJudgementDto dto, bool isEdit = false)
        => Task.FromResult(OperationResult<ReservedJudgementDto>.Success(dto));

    public override async Task<List<ReservedJudgementDto>> GetAllAsync()
    {
        var rjs = await base.GetAllAsync();

        // RJs with a Reason should be listed first as they are Scheduled Decisions
        return [.. rjs
            .OrderByDescending(rj => !string.IsNullOrWhiteSpace(rj.Reason))
            .ThenBy(rj => rj.DueDate)
            .ThenBy(rj => rj.FileNumber)
        ];
    }
}