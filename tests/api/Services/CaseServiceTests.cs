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
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
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

        _mockRepo = new Mock<IRepositoryBase<Case>>();
        _caseService = new CaseService(cachingService, mapper, logger.Object, _mockRepo.Object);

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
            CreateCase(null, CaseService.SEIZED_RESTRICTION_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Payload.ReservedJudgments.Count); // 1 DEC (S) + 1 reserved
        Assert.Equal(2, result.Payload.ScheduledContinuations.Count); // DEC (S), CNT (S)
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
}