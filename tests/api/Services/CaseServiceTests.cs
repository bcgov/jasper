using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Scv.Db.Contexts;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Xunit;

namespace tests.api.Services;

public class CaseServiceTests
{
    private readonly Faker _faker;
    private readonly Mock<IRepositoryBase<Case>> _mockRepo;
    private readonly CaseService _caseService;
    private readonly int _testJudgeId;

    public CaseServiceTests()
    {
        _faker = new Faker();

        var cachingService = new CachingService(new Lazy<ICacheProvider>(() => new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));
        var config = new TypeAdapterConfig();
        config.Apply(new AccessControlManagementMapping());
        var mapper = new Mapper(config);
        var logger = new Mock<ILogger<CaseService>>();
        var dbContext = new JasperDbContext(new DbContextOptionsBuilder<JasperDbContext>().Options);

        _mockRepo = new Mock<IRepositoryBase<Case>>();
        _caseService = new CaseService(cachingService, mapper, logger.Object, _mockRepo.Object, dbContext,
            new ConfigurationBuilder().Build());

        _testJudgeId = _faker.Random.Int();
    }

    [Fact]
    public async Task GetReservedJudgementsAsync_ShouldReturnMappedJudgements()
    {
        var fileNumber = _faker.Random.AlphaNumeric(10);
        var mockJudgement = new Case
        {
            FileNumber = fileNumber,
        };

        _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([mockJudgement]);

        var result = await _caseService.GetAllAsync();

        Assert.Single(result);
        Assert.Equal(fileNumber, result.FirstOrDefault().FileNumber);
        _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task AddReservedJudgementAsync_ShouldReturnSuccess()
    {
        var mockJudgement = new Scv.Models.CaseDto();
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Case>())).Returns(Task.CompletedTask);

        var result = await _caseService.AddAsync(mockJudgement);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Case>()), Times.Once);
    }

    [Fact]
    public async Task AddRangeReservedJudgementAsync_ShouldReturnSuccess()
    {
        var mockJudgements = new List<Scv.Models.CaseDto>();
        _mockRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<Case>>())).Returns(Task.CompletedTask);

        var result = await _caseService.AddRangeAsync(mockJudgements);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<List<Case>>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRangeReservedJudgementAsync_ShouldInvokeWhenEntitiesFound()
    {
        var mockJudgementDto = new Scv.Models.CaseDto() { Id = "test-id" };
        var mockJudgements = new List<Scv.Models.CaseDto>() { mockJudgementDto };
        _mockRepo.Setup(r => r.DeleteRangeAsync(It.IsAny<List<Case>>())).Returns(Task.CompletedTask);
        var mockJudgementEntity = new Case();
        _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>())).Returns(Task.FromResult(mockJudgementEntity));

        var result = await _caseService.DeleteRangeAsync(["test-id"]);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRepo.Verify(r => r.DeleteRangeAsync(It.IsAny<List<Case>>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRangeReservedJudgementAsync_ShouldNotInvokeWhenEntitiesNotFound()
    {
        var mockJudgementDto = new Scv.Models.CaseDto() { Id = "test-id" };
        var mockJudgements = new List<Scv.Models.CaseDto>() { mockJudgementDto };
        _mockRepo.Setup(r => r.DeleteRangeAsync(It.IsAny<List<Case>>())).Returns(Task.CompletedTask);
        var mockJudgementEntity = new Case();

        var result = await _caseService.DeleteRangeAsync(["test-id"]);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        _mockRepo.Verify(r => r.DeleteRangeAsync(It.IsAny<List<Case>>()), Times.Never);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_WithNoCases_ReturnsEmptyLists()
    {
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync([]);

        var result = await _caseService.GetAssignedCasesAsync(_faker.Random.Int());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Payload);
        Assert.Empty(result.Payload.ReservedJudgments);
        Assert.Empty(result.Payload.ScheduledContinuations);
        Assert.Empty(result.Payload.Others);
        Assert.Empty(result.Payload.FutureAssigned);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_WithReservedJudgmentsOnly_ReturnsOnlyReservedJudgments()
    {
        var cases = new List<Case>
        {
            CreateCase(null, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase("", CaseService.SEIZED_RESTRICTION_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_faker.Random.Int());

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payload.ReservedJudgments.Count);
        Assert.Empty(result.Payload.ScheduledContinuations);
        Assert.Empty(result.Payload.Others);
        Assert.Empty(result.Payload.FutureAssigned);
        Assert.Equal(cases.OrderBy(c => c.DueDate).First().StyleOfCause, result.Payload.ReservedJudgments[0].StyleOfCause);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_WithScheduledDecisionsOnly_ReturnsInReservedJudgmentsAndScheduledContinuations()
    {
        var cases = new List<Case>
        {
            CreateCase(CaseService.DECISION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase("dec", CaseService.SEIZED_RESTRICTION_CD),
            CreateCase("Dec", CaseService.SEIZED_RESTRICTION_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Payload.ReservedJudgments.Count);
        Assert.Equal(3, result.Payload.ScheduledContinuations.Count);
        Assert.Empty(result.Payload.Others);
        Assert.Empty(result.Payload.FutureAssigned);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_WithScheduledContinuationsOnly_ReturnsOnlyInScheduledContinuations()
    {
        var cases = new List<Case>
        {
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.ADDTL_CNT_TIME_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Payload.ReservedJudgments);
        Assert.Equal(2, result.Payload.ScheduledContinuations.Count);
        Assert.Empty(result.Payload.Others);
        Assert.Empty(result.Payload.FutureAssigned);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_WithMixedCases_SeparatesCorrectly()
    {
        var cases = new List<Case>
        {
            CreateCase(null, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.DECISION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase("", CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.ADDTL_CNT_TIME_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Payload.ReservedJudgments.Count); // 1 DEC + 2 reserved
        Assert.Equal(3, result.Payload.ScheduledContinuations.Count); // DEC, CNT, ACT
        Assert.Empty(result.Payload.Others);
        Assert.Empty(result.Payload.FutureAssigned);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_ScheduledDecisionsAppearFirstInReservedJudgments()
    {
        var cases = new List<Case>
        {
            CreateCase(null, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.DECISION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(null, CaseService.SEIZED_RESTRICTION_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(cases[1].StyleOfCause, result.Payload.ReservedJudgments[0].StyleOfCause);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_CaseInsensitiveReasonMatching()
    {
        var cases = new List<Case>
        {
            CreateCase("dec", CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.DECISION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase("Dec", CaseService.SEIZED_RESTRICTION_CD),
            CreateCase("cnt", CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase("act", CaseService.SEIZED_RESTRICTION_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Payload.ReservedJudgments.Count);
        Assert.Equal(6, result.Payload.ScheduledContinuations.Count);
        Assert.Empty(result.Payload.Others);
        Assert.Empty(result.Payload.FutureAssigned);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_UnrecognizedReasonCode_IncludedInOthers()
    {
        var cases = new List<Case>
        {
            CreateCase("UNKNOWN", CaseService.SEIZED_RESTRICTION_CD),
            CreateCase("XYZ", CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.DECISION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD)
        };
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Single(result.Payload.ReservedJudgments); // Only DEC
        Assert.Single(result.Payload.ScheduledContinuations); // Only DEC
        Assert.Equal(2, result.Payload.Others.Count); // UNKNOWN and XYZ
        Assert.Empty(result.Payload.FutureAssigned);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_FutureAssignedCases_IncludedInFutureAssigned()
    {
        var cases = new List<Case>
        {
            CreateCase(CaseService.DECISION_APPR_REASON_CD, CaseService.ASSIGNED_RESTRICTION_CD),
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD, CaseService.ASSIGNED_RESTRICTION_CD),
            CreateCase("UNKNOWN", CaseService.ASSIGNED_RESTRICTION_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Payload.ReservedJudgments);
        Assert.Empty(result.Payload.ScheduledContinuations);
        Assert.Empty(result.Payload.Others);
        Assert.Equal(3, result.Payload.FutureAssigned.Count);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_MixedRestrictionCodes_CategorizedCorrectly()
    {
        var cases = new List<Case>
        {
            CreateCase(CaseService.DECISION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.DECISION_APPR_REASON_CD, CaseService.ASSIGNED_RESTRICTION_CD),
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD, CaseService.ASSIGNED_RESTRICTION_CD),
            CreateCase("UNKNOWN", CaseService.SEIZED_RESTRICTION_CD),
            CreateCase("UNKNOWN", CaseService.ASSIGNED_RESTRICTION_CD),
            CreateCase(null, CaseService.SEIZED_RESTRICTION_CD),
            CreateCase(CaseService.SENTENCE_HEARING_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payload.ReservedJudgments.Count); // 1 DEC (S) + 1 reserved
        Assert.Equal(3, result.Payload.ScheduledContinuations.Count); // DEC (S), CNT (S), SNT (S)
        Assert.Single(result.Payload.Others); // UNKNOWN (S)
        Assert.Equal(3, result.Payload.FutureAssigned.Count); // All with restriction G
    }

    [Fact]
    public async Task GetAssignedCasesAsync_OnlySeizedRestrictionInNonFutureAssigned()
    {
        var cases = new List<Case>
        {
            CreateCase(CaseService.DECISION_APPR_REASON_CD, "X"), // Should be excluded
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD, "Y"), // Should be excluded
            CreateCase(null, "Z"),
            CreateCase(CaseService.DECISION_APPR_REASON_CD, CaseService.SEIZED_RESTRICTION_CD) // Should be included
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payload.ReservedJudgments.Count);
        Assert.Single(result.Payload.ScheduledContinuations);
        Assert.Empty(result.Payload.Others);
        Assert.Empty(result.Payload.FutureAssigned);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_RepositoryThrowsException_ReturnsFailure()
    {
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.False(result.Succeeded);
        Assert.Contains("Error retrieving assigned cases.", result.Errors);
    }

    private Case CreateCase(string reason, string restrictionCode)
    {
        return new Case
        {
            Id = _faker.Random.AlphaNumeric(24),
            JudgeId = _testJudgeId,
            AppearanceId = _faker.Random.Int(1, 9999).ToString(),
            AppearanceDate = _faker.Date.Future(),
            CourtClass = _faker.PickRandom("S", "A", "P"),
            CourtFileNumber = _faker.Random.AlphaNumeric(10),
            FileNumber = $"{_faker.Random.AlphaNumeric(2)}-{_faker.Random.AlphaNumeric(8)}",
            StyleOfCause = $"{_faker.Name.LastName()} vs {_faker.Name.LastName()}",
            Reason = reason,
            PartId = _faker.Random.AlphaNumeric(10),
            RestrictionCode = restrictionCode
        };
    }

    [Fact]
    public void Reconcile_MatchesIdsUpdatesSourceAndPreservesOverride()
    {
        var old = ReconciliationCase("2026-12-01");
        var source = ReconciliationCase();
        source.DueDate = "25-Nov-2026";
        source.StyleOfCause = "Updated source";
        var result = CaseService.ReconcileCases([old], [source], DateTime.UtcNow, 30);
        Assert.Empty(result.Added);
        Assert.Empty(result.Removed);
        Assert.Equal("2026-12-01", old.OverrideDueDate);
        Assert.Equal(source.DueDate, old.DueDate);
        Assert.Equal(source.StyleOfCause, old.StyleOfCause);
        Assert.NotEqual(source.Id, old.Id);
    }

    [Fact]
    public void Reconcile_EmptyImportDeletesUneditedRowsAndRetainsOriginalDeletionTime()
    {
        var now = DateTime.UtcNow;
        var plain = ReconciliationCase();
        var overridden = ReconciliationCase("2026-12-01");
        var result = CaseService.ReconcileCases([plain, overridden], [], now, 30);
        // Eligible duplicate rows share the override before absence is processed.
        Assert.Empty(result.Removed);
        Assert.Equal(now, plain.DeletedAt);
        Assert.Equal(now, overridden.DeletedAt);
        CaseService.ReconcileCases([plain, overridden], [], now.AddDays(5), 30);
        Assert.Equal(now, overridden.DeletedAt);
    }

    [Fact]
    public void Reconcile_ExpiresOnlyMissingRowsOutsideRetentionAndRestoresReappearances()
    {
        var now = DateTime.UtcNow;
        var expired = ReconciliationCase("2026-12-01");
        expired.DeletedAt = now.AddDays(-31);
        var retained = ReconciliationCase("2026-12-01");
        retained.AppearanceId = "retained";
        retained.DeletedAt = now.AddDays(-30);
        var plain = ReconciliationCase();
        plain.AppearanceId = "plain";
        var result = CaseService.ReconcileCases([expired, retained, plain], [], now, 30);
        Assert.Equal([expired, plain], result.Removed);
        var source = ReconciliationCase();
        source.AppearanceId = "retained";
        CaseService.ReconcileCases([retained], [source], now, 30);
        Assert.Null(retained.DeletedAt);
        Assert.Equal("2026-12-01", retained.OverrideDueDate);
    }

    [Fact]
    public void Reconcile_PreservesEveryDuplicateAndPropagatesOnlyToEligibleRows()
    {
        var old = ReconciliationCase("2026-12-01");
        var unrelated = ReconciliationCase();
        unrelated.Reason = CaseService.CONTINUATION_APPR_REASON_CD;
        var sources = new List<Case> { ReconciliationCase(), ReconciliationCase(), ReconciliationCase() };
        sources[2].Reason = CaseService.CONTINUATION_APPR_REASON_CD;
        var result = CaseService.ReconcileCases([old, unrelated], sources, DateTime.UtcNow, 30);
        Assert.Single(result.Added);
        Assert.Equal("2026-12-01", result.Added[0].OverrideDueDate);
        Assert.Null(unrelated.OverrideDueDate);
        Assert.Empty(result.Removed);
        Assert.Equal(2, Assert.Single(result.Warnings).ImportedCount);
    }

    [Fact]
    public void Reconcile_ConflictsPreserveAllValuesWhileUnrelatedRowsUpdate()
    {
        var first = ReconciliationCase("2026-12-01");
        var second = ReconciliationCase("2026-12-02");
        var third = ReconciliationCase();
        third.AppearanceId = "unrelated";
        var source = ReconciliationCase();
        var otherSource = ReconciliationCase();
        otherSource.AppearanceId = third.AppearanceId;
        otherSource.AgeInDays = 123;
        var result = CaseService.ReconcileCases([first, second, third], [source, otherSource], DateTime.UtcNow, 30);
        Assert.Equal("2026-12-01", first.OverrideDueDate);
        Assert.Equal("2026-12-02", second.OverrideDueDate);
        Assert.Equal(123, third.AgeInDays);
        Assert.Empty(result.Removed);
        Assert.Equal(2, Assert.Single(result.Warnings).ConflictingOverrides.Length);
        Assert.Single(new[] { first, second }, c => c.DeletedAt != null);
    }

    [Fact]
    public void Reconcile_MissingKeysNeverAttachAnOverrideToAnUnidentifiedImport()
    {
        var old = ReconciliationCase("2026-12-01");
        old.AppearanceId = null;
        var source = ReconciliationCase();
        source.AppearanceId = null;
        var result = CaseService.ReconcileCases([old], [source], DateTime.UtcNow, 30);
        Assert.Same(source, Assert.Single(result.Added));
        Assert.Null(source.OverrideDueDate);
        Assert.NotNull(old.DeletedAt);
        Assert.True(Assert.Single(result.Warnings).MissingBusinessKey);
    }

    [Fact]
    public async Task GetAssignedCases_ExcludesSoftDeletedRows()
    {
        var active = ReconciliationCase();
        var deleted = ReconciliationCase("2026-12-01");
        deleted.DeletedAt = DateTime.UtcNow;
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync((Expression<Func<Case, bool>> filter) => new[] { active, deleted }.Where(filter.Compile()));
        var result = await _caseService.GetAssignedCasesAsync(active.JudgeId.Value);
        Assert.Equal(active.Id, Assert.Single(result.Payload.ReservedJudgments).Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-02-30")]
    [InlineData("2026-11-01T00:00:00Z")]
    [InlineData("11/01/2026")]
    public async Task Override_RejectsInvalidDateBeforeAccessingDatabase(string date)
    {
        var result = await _caseService.SetDueDateOverrideAsync(MongoDB.Bson.ObjectId.GenerateNewId().ToString(), 1,
            new Scv.Models.CaseDueDateOverrideRequest { OverrideDueDate = date, UpdatedDate = DateTime.UtcNow });
        Assert.Equal(400, result.Status);
    }

    [Theory]
    [InlineData("CNT", "S")]
    [InlineData("DEC", "G")]
    [InlineData("OTH", "S")]
    public void Reconcile_UnrelatedDuplicatesArePreservedWithoutOverrideWarnings(string reason, string restriction)
    {
        var old = ReconciliationCase();
        old.Reason = reason;
        old.RestrictionCode = restriction;
        var sources = new[] { ReconciliationCase(), ReconciliationCase() };
        foreach (var source in sources)
        {
            source.Reason = reason;
            source.RestrictionCode = restriction;
        }
        var result = CaseService.ReconcileCases([old], [.. sources], DateTime.UtcNow, 30);
        Assert.Empty(result.Warnings);
        Assert.Single(result.Added);
        Assert.Empty(result.Removed);
    }

    [Fact]
    public void Reconcile_MixedCategoriesDoNotInflateEligibleDuplicateCounts()
    {
        var eligible = ReconciliationCase("2026-12-01");
        var unrelated = ReconciliationCase();
        unrelated.Reason = "CNT";
        var eligibleImport = ReconciliationCase();
        var unrelatedImport = ReconciliationCase();
        unrelatedImport.Reason = "CNT";
        var result = CaseService.ReconcileCases([eligible, unrelated], [eligibleImport, unrelatedImport], DateTime.UtcNow, 30);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.Added);
        Assert.Empty(result.Removed);
        Assert.Equal("2026-12-01", eligible.OverrideDueDate);
        Assert.Null(unrelated.OverrideDueDate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reconcile_PreExistingConflictsRetainIdsAndDatesDespiteImportOrder(bool reversed)
    {
        var first = ReconciliationCase("2026-12-01");
        first.Id = "000000000000000000000001";
        first.PartId = "party-a";
        var second = ReconciliationCase("2026-12-02");
        second.Id = "000000000000000000000002";
        second.PartId = "party-b";
        var sourceA = ReconciliationCase();
        sourceA.PartId = "party-a";
        var sourceB = ReconciliationCase();
        sourceB.PartId = "party-b";
        var result = CaseService.ReconcileCases([first, second],
            reversed ? [sourceB, sourceA] : [sourceA, sourceB], DateTime.UtcNow, 30);
        Assert.Empty(result.Added);
        Assert.Empty(result.Removed);
        Assert.Equal("2026-12-01", first.OverrideDueDate);
        Assert.Equal("2026-12-02", second.OverrideDueDate);
        Assert.Equal(2, Assert.Single(result.Warnings).ConflictingOverrides.Length);
        // With pre-existing conflicts the shared key cannot identify an individual source row.
        Assert.Equal(reversed ? "party-b" : "party-a", first.PartId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reconcile_AbsentDuplicateJudgmentsDoNotGenerateRecurringWarnings(bool alreadyDeleted)
    {
        var now = DateTime.UtcNow;
        var first = ReconciliationCase("2026-12-01");
        var second = ReconciliationCase("2026-12-01");
        if (alreadyDeleted) first.DeletedAt = second.DeletedAt = now.AddDays(-1);
        var expectedDeletedAt = first.DeletedAt ?? now;
        var result = CaseService.ReconcileCases([first, second], [], now, 30);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.Removed);
        Assert.All(new[] { first, second }, c => Assert.Equal(expectedDeletedAt, c.DeletedAt));
        var repeated = CaseService.ReconcileCases([first, second], [], now.AddDays(1), 30);
        Assert.Empty(repeated.Warnings);
        Assert.Empty(repeated.Removed);
        Assert.All(new[] { first, second }, c => Assert.Equal("2026-12-01", c.OverrideDueDate));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reconcile_HistoricalDuplicatesAloneDoNotWarnWhenOneJudgmentReturns(bool hasActiveSibling)
    {
        var first = ReconciliationCase("2026-12-01");
        var second = ReconciliationCase("2026-12-01");
        first.DeletedAt = hasActiveSibling ? null : DateTime.UtcNow.AddDays(-1);
        second.DeletedAt = DateTime.UtcNow.AddDays(-1);
        var ids = new[] { first.Id, second.Id };
        var result = CaseService.ReconcileCases([first, second], [ReconciliationCase()], DateTime.UtcNow, 30);
        Assert.Empty(result.Warnings);
        Assert.Empty(result.Removed);
        Assert.Empty(result.Added);
        Assert.Equal(ids, new[] { first.Id, second.Id });
        Assert.All(new[] { first, second }, c => Assert.Equal("2026-12-01", c.OverrideDueDate));
    }

    [Fact]
    public void Reconcile_HistoricalConflictsWarnWhenRelevantJudgmentsReturn()
    {
        var first = ReconciliationCase("2026-12-01");
        var second = ReconciliationCase("2026-12-02");
        first.DeletedAt = second.DeletedAt = DateTime.UtcNow.AddDays(-1);
        var absent = CaseService.ReconcileCases([first, second], [], DateTime.UtcNow, 30);
        Assert.Empty(absent.Warnings);
        Assert.Empty(absent.Removed);
        var unrelated = ReconciliationCase();
        unrelated.Reason = "CNT";
        Assert.Empty(CaseService.ReconcileCases([first, second], [unrelated], DateTime.UtcNow, 30).Warnings);
        var returned = CaseService.ReconcileCases([first, second], [ReconciliationCase()], DateTime.UtcNow, 30);
        var warning = Assert.Single(returned.Warnings);
        Assert.Equal(0, warning.ExistingCount);
        Assert.Equal(1, warning.ImportedCount);
        Assert.Equal(new[] { "2026-12-01", "2026-12-02" }, warning.ConflictingOverrides);
        Assert.Empty(returned.Removed);
        Assert.Equal("2026-12-01", first.OverrideDueDate);
        Assert.Equal("2026-12-02", second.OverrideDueDate);
    }

    private static Case ReconciliationCase(string date = null) => new()
    {
        JudgeId = 1,
        PhysicalFileId = "file",
        AppearanceId = "appearance",
        OverrideDueDate = date,
        RestrictionCode = CaseService.SEIZED_RESTRICTION_CD
    };
}
