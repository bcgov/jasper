using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
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
    Task<OperationResult<List<CaseSyncWarning>>> SyncAllAssignedCasesAsync(List<CaseDto> importedCases);
    Task<(int Status, OperationResult<List<CaseDto>> Result)> SetDueDateOverrideAsync(
        string id, int judgeId, CaseDueDateOverrideRequest request);
}

public class CaseService(
    IAppCache cache,
    IMapper mapper,
    ILogger<CaseService> logger,
    IRepositoryBase<Case> judgementRepo,
    JasperDbContext dbContext,
    IConfiguration configuration) : CrudServiceBase<IRepositoryBase<Case>, Case, CaseDto>(
        cache,
        mapper,
        logger,
        judgementRepo), ICaseService
{
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

    public async Task<OperationResult<List<CaseSyncWarning>>> SyncAllAssignedCasesAsync(List<CaseDto> importedCases)
    {
        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var existing = await _dbContext.Cases.ToListAsync();
            var incoming = this.Mapper.Map<List<Case>>(importedCases);
            var retentionDays = int.TryParse(configuration["JOBS:SyncAssignedCases:RetentionDays"], out var days)
                && days is > 0 and <= 36500 ? days : 30;
            var (added, removed, warnings) = ReconcileCases(existing, incoming, DateTime.UtcNow, retentionDays);
            _dbContext.Cases.RemoveRange(removed);
            await _dbContext.Cases.AddRangeAsync(added);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            this.InvalidateCache(this.CacheName);
            return OperationResult<List<CaseSyncWarning>>.Success(warnings);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error synchronizing assigned cases: {Message}", ex.Message);
            _dbContext.ChangeTracker.Clear();
            return OperationResult<List<CaseSyncWarning>>.Failure("Error synchronizing assigned cases.");
        }
    }

    private static bool HasBusinessKey(Case c) => c.JudgeId > 0
        && !string.IsNullOrWhiteSpace(c.PhysicalFileId) && !string.IsNullOrWhiteSpace(c.AppearanceId);

    private static bool IsEligible(Case c) => string.IsNullOrWhiteSpace(c.Reason)
        || (string.Equals(c.Reason, DECISION_APPR_REASON_CD, StringComparison.OrdinalIgnoreCase)
            && string.Equals(c.RestrictionCode, SEIZED_RESTRICTION_CD, StringComparison.OrdinalIgnoreCase));

    private static (int? JudgeId, string PhysicalFileId, string AppearanceId) BusinessKey(Case c)
        => (c.JudgeId, c.PhysicalFileId, c.AppearanceId);

    private static string Category(Case c) => IsEligible(c)
        ? "judgment" : $"{c.Reason?.ToUpperInvariant()}|{c.RestrictionCode?.ToUpperInvariant()}";

    // Mutates tracked source fields only, except explicit propagation of an unambiguous group override.
    internal static (List<Case> Added, List<Case> Removed, List<CaseSyncWarning> Warnings) ReconcileCases(
        List<Case> existing, List<Case> incoming, DateTime now, int retentionDays)
    {
        var added = new List<Case>();
        var removed = new List<Case>();
        var warnings = new List<CaseSyncWarning>();
        var matched = new HashSet<Case>();
        var existingGroups = existing.Where(HasBusinessKey).ToLookup(BusinessKey);
        var incomingGroups = incoming.Where(HasBusinessKey).ToLookup(BusinessKey);

        foreach (var key in existingGroups.Select(g => g.Key).Union(incomingGroups.Select(g => g.Key)))
        {
            var oldRows = existingGroups[key].ToList();
            var newRows = incomingGroups[key].ToList();
            var eligibleOldRows = oldRows.Where(IsEligible).ToList();
            var activeOldCount = eligibleOldRows.Count(c => c.DeletedAt == null);
            var eligibleNewCount = newRows.Count(IsEligible);
            var overrides = eligibleOldRows.Select(c => c.OverrideDueDate)
                .Where(d => d != null).Distinct().Order().ToArray();
            if (eligibleNewCount > 0 && (activeOldCount > 1 || eligibleNewCount > 1 || overrides.Length > 1))
            {
                warnings.Add(new CaseSyncWarning
                {
                    JudgeId = key.JudgeId,
                    PhysicalFileId = key.PhysicalFileId,
                    AppearanceId = key.AppearanceId,
                    ExistingCount = activeOldCount,
                    ImportedCount = eligibleNewCount,
                    ConflictingOverrides = overrides.Length > 1 ? overrides : []
                });
            }

            // Conflicts retain dates on their stored IDs; the shared key cannot identify individual source rows.
            if (overrides.Length == 1)
                foreach (var row in eligibleOldRows) row.OverrideDueDate = overrides[0];

            var available = oldRows.OrderBy(c => c.DeletedAt.HasValue).ThenBy(c => c.Id, StringComparer.Ordinal)
                .GroupBy(Category).ToDictionary(g => g.Key, g => new Queue<Case>(g));
            foreach (var source in newRows)
            {
                if (available.TryGetValue(Category(source), out var queue) && queue.TryDequeue(out var target))
                {
                    CopySourceFields(source, target);
                    target.DeletedAt = null;
                    // Also touches unchanged matches so concurrent group insertions/edits conflict atomically.
                    target.Upd_Dtm = now;
                    matched.Add(target);
                }
                else
                {
                    source.OverrideDueDate = IsEligible(source) && overrides.Length == 1 ? overrides[0] : null;
                    source.DeletedAt = null;
                    added.Add(source);
                }
            }
        }

        foreach (var source in incoming.Where(c => !HasBusinessKey(c)))
        {
            source.OverrideDueDate = null;
            source.DeletedAt = null;
            added.Add(source);
            warnings.Add(new CaseSyncWarning
            {
                JudgeId = source.JudgeId,
                PhysicalFileId = source.PhysicalFileId,
                AppearanceId = source.AppearanceId,
                ImportedCount = 1,
                MissingBusinessKey = true
            });
        }

        foreach (var missing in existing.Where(c => !matched.Contains(c)))
        {
            if (missing.OverrideDueDate == null || missing.DeletedAt < now.AddDays(-retentionDays))
                removed.Add(missing);
            else
                missing.DeletedAt ??= now;
        }
        return (added, removed, warnings);
    }

    private static void CopySourceFields(Case source, Case target)
    {
        target.AppearanceId = source.AppearanceId;
        target.AppearanceDate = source.AppearanceDate;
        target.JudgeId = source.JudgeId;
        target.CourtClass = source.CourtClass;
        target.PhysicalFileId = source.PhysicalFileId;
        target.CourtFileNumber = source.CourtFileNumber;
        target.FileNumber = source.FileNumber;
        target.AgeInDays = source.AgeInDays;
        target.StyleOfCause = source.StyleOfCause;
        target.Reason = source.Reason;
        target.DueDate = source.DueDate;
        target.PartId = source.PartId;
        target.RestrictionCode = source.RestrictionCode;
    }

    public async Task<(int Status, OperationResult<List<CaseDto>> Result)> SetDueDateOverrideAsync(
        string id, int judgeId, CaseDueDateOverrideRequest request)
    {
        if (judgeId <= 0) return (403, OperationResult<List<CaseDto>>.Failure("A judge identity is required."));
        if (!ObjectId.TryParse(id, out _)) return (404, OperationResult<List<CaseDto>>.Failure("Case not found."));
        if (request?.UpdatedDate == null || (request.OverrideDueDate != null
            && !DateOnly.TryParseExact(request.OverrideDueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _)))
            return (400, OperationResult<List<CaseDto>>.Failure("Supply a date as yyyy-MM-dd or null, and the displayed record's updatedDate."));
        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            var entity = await _dbContext.Cases.SingleOrDefaultAsync(c => c.Id == id);
            if (entity == null) return (404, OperationResult<List<CaseDto>>.Failure("Case not found."));
            if (entity.JudgeId != judgeId) return (403, OperationResult<List<CaseDto>>.Failure("This case belongs to another judge."));
            if (entity.DeletedAt != null || !HasBusinessKey(entity) || !IsEligible(entity)
                || entity.Upd_Dtm != request.UpdatedDate.Value.ToUniversalTime())
                return (409, OperationResult<List<CaseDto>>.Failure("This case has changed. Refresh before editing its due date."));

            var group = await _dbContext.Cases.Where(c => c.JudgeId == entity.JudgeId
                && c.PhysicalFileId == entity.PhysicalFileId && c.AppearanceId == entity.AppearanceId).ToListAsync();
            var eligible = group.Where(IsEligible).ToList();
            if (eligible.Select(c => c.OverrideDueDate).Where(d => d != null).Distinct().Skip(1).Any())
                return (409, OperationResult<List<CaseDto>>.Failure("Conflicting saved due dates require administrator review."));
            foreach (var row in eligible)
            {
                row.OverrideDueDate = request.OverrideDueDate;
                row.Upd_Dtm = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            this.InvalidateCache(this.CacheName);
            return (200, OperationResult<List<CaseDto>>.Success(Mapper.Map<List<CaseDto>>(eligible.Where(c => c.DeletedAt == null))));
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            return (409, OperationResult<List<CaseDto>>.Failure("This case changed while saving. Refresh and try again."));
        }
        catch (MongoException ex) when (ex.HasErrorLabel("TransientTransactionError"))
        {
            _dbContext.ChangeTracker.Clear();
            return (409, OperationResult<List<CaseDto>>.Failure("This case changed while saving. Refresh and try again."));
        }
        catch (Exception ex)
        {
            _dbContext.ChangeTracker.Clear();
            Logger.LogError(ex, "Error saving due date for case {CaseId}", id);
            return (500, OperationResult<List<CaseDto>>.Failure("Unable to save the due date. Your selection has not been saved."));
        }
    }

    public async Task<OperationResult<CaseResponse>> GetAssignedCasesAsync(int judgeId)
    {
        try
        {
            // Each refresh must see commits from other API/job instances as well.
            var judgeCases = await this.Repo.FindAsync(c => c.JudgeId == judgeId && c.DeletedAt == null);

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
