using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Db.Models;
using Scv.Db.Repositories;

namespace Scv.Api.Services;

public interface ICaseService : ICrudService<CaseDto>
{
    Task<OperationResult<CaseResponse>> GetAssignedCasesAsync(int judgeId);
}

public class CaseService(
    IAppCache cache,
    IMapper mapper,
    ILogger<CaseService> logger,
    IRepositoryBase<Case> judgementRepo) : CrudServiceBase<IRepositoryBase<Case>, Case, CaseDto>(
        cache,
        mapper,
        logger,
        judgementRepo), ICaseService
{
    public override string CacheName => "GetCasesAsync";

    public const string DECISION_APPR_REASON_CD = "DEC";
    public const string CONTINUATION_APPR_REASON_CD = "CNT";
    public const string ADDTL_CNT_TIME_APPR_REASON_CD = "ACT";

    public const string SEIZED_RESTRICTION_CD = "S";
    public const string ASSIGNED_RESTRICTION_CD = "G";

    public static readonly ImmutableArray<string> ContinuationReasonCodes = [
        DECISION_APPR_REASON_CD,
        CONTINUATION_APPR_REASON_CD,
        ADDTL_CNT_TIME_APPR_REASON_CD
    ];

    public override Task<OperationResult<CaseDto>> ValidateAsync(CaseDto dto, bool isEdit = false)
        => Task.FromResult(OperationResult<CaseDto>.Success(dto));

    public async Task<OperationResult<CaseResponse>> GetAssignedCasesAsync(int judgeId)
    {
        try
        {
            var judgeCases = await this.GetDataFromCache(
                $"{this.CacheName}-{judgeId}",
                async () => await this.Repo.FindAsync(c => c.JudgeId == judgeId)
            );

            var reservedJudgments = judgeCases
                .Where(c => string.IsNullOrWhiteSpace(c.Reason))
                .OrderBy(c => c.DueDate);

            var scheduledDecisions = judgeCases
                .Where(c => !string.IsNullOrWhiteSpace(c.Reason)
                    && c.Reason.Equals(DECISION_APPR_REASON_CD, StringComparison.OrdinalIgnoreCase)
                    && c.RestrictionCode.Equals(SEIZED_RESTRICTION_CD, StringComparison.OrdinalIgnoreCase))
                .OrderByDateString(c => c.DueDate);

            var scheduledContinuations = judgeCases
                .Where(c => ContinuationReasonCodes
                    .Any(code => code.Equals(c.Reason, StringComparison.OrdinalIgnoreCase))
                        && c.RestrictionCode.Equals(SEIZED_RESTRICTION_CD, StringComparison.OrdinalIgnoreCase))
                .OrderByDateString(c => c.DueDate);

            var others = judgeCases
                .Where(c => !string.IsNullOrWhiteSpace(c.Reason)
                    && !ContinuationReasonCodes
                        .Any(code => code.Equals(c.Reason, StringComparison.OrdinalIgnoreCase))
                    && c.RestrictionCode.Equals(SEIZED_RESTRICTION_CD, StringComparison.OrdinalIgnoreCase))
                .OrderByDateString(c => c.DueDate);

            var futureAssigned = judgeCases
                .Where(c => c.RestrictionCode.Equals(ASSIGNED_RESTRICTION_CD, StringComparison.OrdinalIgnoreCase))
                .OrderByDateString(c => c.DueDate);

            var response = new CaseResponse
            {
                // Scheduled decisions should be listed first followed by reserved judgments
                ReservedJudgments = this.Mapper.Map<List<CaseDto>>(scheduledDecisions.Concat(reservedJudgments)),
                ScheduledContinuations = this.Mapper.Map<List<CaseDto>>(scheduledContinuations),
                Others = this.Mapper.Map<List<CaseDto>>(others),
                FutureAssigned = this.Mapper.Map<List<CaseDto>>(futureAssigned)
            };

            return OperationResult<CaseResponse>.Success(response);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error retrieving assigned cases for judge {JudgeId}: {Message}", judgeId, ex.Message);
            return OperationResult<CaseResponse>.Failure("Error retrieving assigned cases.");
        }
    }
}