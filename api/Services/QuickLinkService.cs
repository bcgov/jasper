using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Core.Infrastructure;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models;

namespace Scv.Api.Services;

public interface IQuickLinkService : ICrudService<QuickLinkDto>
{
    Task<IEnumerable<QuickLinkDto>> GetJudgeQuickLinks();
}

public class QuickLinkService(
    IAppCache cache,
    IMapper mapper,
    ILogger<QuickLinkService> logger,
    IRepositoryBase<QuickLink> quickLinkRepo
) : CrudServiceBase<IRepositoryBase<QuickLink>, QuickLink, QuickLinkDto>(
        cache,
        mapper,
        logger,
        quickLinkRepo), IQuickLinkService
{
    public override string CacheName => nameof(QuickLinkService);
    
    public async Task<IEnumerable<QuickLinkDto>> GetJudgeQuickLinks()
    {
        // For now we are only retrieving default quick links (JudgeId == null)
        var quickLinks = await GetDataFromCache(
            $"{CacheName}", async () => await Repo.FindAsync(c => c.JudgeId == null)
        );

        var quickLinkDtos = Mapper.Map<IEnumerable<QuickLinkDto>>(quickLinks);
        return quickLinkDtos.OrderBy(ql => ql.Order);
    }

    public override Task<OperationResult<QuickLinkDto>> ValidateAsync(QuickLinkDto dto, bool isEdit = false)
    {
        return Task.FromResult(OperationResult<QuickLinkDto>.Success(dto));
    }

    public override Task<OperationResult> DeleteAsync(string id)
    {
        throw new System.NotImplementedException();
    }
}