using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Db.Contexts;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models;

namespace Scv.Api.Services;

public interface ICaseService : ICrudService<CaseDto>
{
    Task<OperationResult<CaseResponse>> GetAssignedCasesAsync(int judgeId);
    Task<OperationResult> ReplaceAllAssignedCasesAsync(List<CaseDto> replacementCases);
}

public class CaseService(
    IAppCache cache,
    IMapper mapper,
    ILogger<CaseService> logger,
    IRepositoryBase<Case> judgementRepo,
    JasperDbContext dbContext) : CrudServiceBase<IRepositoryBase<Case>, Case, CaseDto>(
        cache,
        mapper,
        logger,
        judgementRepo), ICaseService
{
    private const int ReplaceDeleteBatchSize = 500;

    private readonly JasperDbContext _dbContext = dbContext;

    public override string CacheName => "GetCasesAsync";

    public const string DECISION_APPR_REASON_CD = "DEC";
    public const string CONTINUATION_APPR_REASON_CD = "CNT";
    public const string ADDTL_CNT_TIME_APPR_REASON_CD = "ACT";
    public const string SENTENCE_HEARING_APPR_REASON_CD = "SNT";

    public const string SEIZED_RESTRICTION_CD = "S";
    public const string ASSIGNED_RESTRICTION_CD = "G";

    public static readonly ImmutableArray<string> ContinuationReasonCodes = [
        DECISION_APPR_REASON_CD,
        CONTINUATION_APPR_REASON_CD,
        ADDTL_CNT_TIME_APPR_REASON_CD,
        SENTENCE_HEARING_APPR_REASON_CD
    ];

    public override Task<OperationResult<CaseDto>> ValidateAsync(CaseDto dto, bool isEdit = false)
        => Task.FromResult(OperationResult<CaseDto>.Success(dto));

    public async Task<OperationResult> ReplaceAllAssignedCasesAsync(List<CaseDto> replacementCases)
    {
        try
        {
            var existingIds = await _dbContext.Cases
                .Select(@case => @case.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToListAsync();

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            if (existingIds.Count > 0)
            {
                foreach (var batch in existingIds.Chunk(ReplaceDeleteBatchSize))
                {
                    _dbContext.Cases.RemoveRange(batch.Select(id => new Case { Id = id }));
                    await _dbContext.SaveChangesAsync();
                }
            }

            if (replacementCases.Count > 0)
            {
                var replacementEntities = this.Mapper.Map<List<Case>>(replacementCases);
                await _dbContext.Cases.AddRangeAsync(replacementEntities);
                await _dbContext.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            this.InvalidateCache(this.CacheName);

            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error replacing cases: {Message}", ex.Message);
            return OperationResult.Failure("Error replacing cases.");
        }
    }

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
