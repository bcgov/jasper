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
        var mockJudgement = new Scv.Api.Models.CaseDto();
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Case>())).Returns(Task.CompletedTask);

        var result = await _caseService.AddAsync(mockJudgement);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Case>()), Times.Once);
    }

    [Fact]
    public async Task AddRangeReservedJudgementAsync_ShouldReturnSuccess()
    {
        var mockJudgements = new List<Scv.Api.Models.CaseDto>();
        _mockRepo.Setup(r => r.AddRangeAsync(It.IsAny<List<Case>>())).Returns(Task.CompletedTask);

        var result = await _caseService.AddRangeAsync(mockJudgements);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _mockRepo.Verify(r => r.AddRangeAsync(It.IsAny<List<Case>>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRangeReservedJudgementAsync_ShouldInvokeWhenEntitiesFound()
    {
        var mockJudgementDto = new Scv.Api.Models.CaseDto() { Id = "test-id" };
        var mockJudgements = new List<Scv.Api.Models.CaseDto>() { mockJudgementDto };
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
        var mockJudgementDto = new Scv.Api.Models.CaseDto() { Id = "test-id" };
        var mockJudgements = new List<Scv.Api.Models.CaseDto>() { mockJudgementDto };
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
    }

    [Fact]
    public async Task GetAssignedCasesAsync_WithReservedJudgmentsOnly_ReturnsOnlyReservedJudgments()
    {
        var cases = new List<Case>
        {
            CreateCase(null),
            CreateCase(""),
            CreateCase("")
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_faker.Random.Int());

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Payload.ReservedJudgments.Count);
        Assert.Empty(result.Payload.ScheduledContinuations);
        Assert.Equal(cases.OrderBy(c => c.StyleOfCause).First().StyleOfCause, result.Payload.ReservedJudgments[0].StyleOfCause);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_WithScheduledDecisionsOnly_ReturnsInBothLists()
    {
        var cases = new List<Case>
        {
            CreateCase(CaseService.DECISION_APPR_REASON_CD),
            CreateCase("dec"),
            CreateCase("Dec")
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Payload.ReservedJudgments.Count);
        Assert.Equal(3, result.Payload.ScheduledContinuations.Count);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_WithScheduledContinuationsOnly_ReturnsOnlyInScheduledContinuations()
    {
        var cases = new List<Case>
        {
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD),
            CreateCase(CaseService.ADDTL_CNT_TIME_APPR_REASON_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Payload.ReservedJudgments);
        Assert.Equal(2, result.Payload.ScheduledContinuations.Count);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_WithMixedCases_SeparatesCorrectly()
    {
        var cases = new List<Case>
        {
            CreateCase(null),
            CreateCase(CaseService.DECISION_APPR_REASON_CD),
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD),
            CreateCase(""),
            CreateCase(CaseService.ADDTL_CNT_TIME_APPR_REASON_CD)
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Payload.ReservedJudgments.Count); // 1 DEC + 2 reserved
        Assert.Equal(3, result.Payload.ScheduledContinuations.Count); // DEC, CNT, ACT
    }

    [Fact]
    public async Task GetAssignedCasesAsync_ScheduledDecisionsAppearFirstInReservedJudgments()
    {
        var cases = new List<Case>
        {
            CreateCase(null),
            CreateCase(CaseService.DECISION_APPR_REASON_CD),
            CreateCase(null)
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
            CreateCase("dec"),
            CreateCase(CaseService.DECISION_APPR_REASON_CD),
            CreateCase("Dec"),
            CreateCase("cnt"),
            CreateCase(CaseService.CONTINUATION_APPR_REASON_CD),
            CreateCase("act")
        };

        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Payload.ReservedJudgments.Count);
        Assert.Equal(6, result.Payload.ScheduledContinuations.Count);
    }

    [Fact]
    public async Task GetAssignedCasesAsync_UnrecognizedReasonCode_ExcludedFromBothLists()
    {
        var cases = new List<Case>
        {
            CreateCase("UNKNOWN"),
            CreateCase("XYZ"),
            CreateCase(CaseService.DECISION_APPR_REASON_CD)
        };
        _mockRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Case, bool>>>()))
            .ReturnsAsync(cases);

        var result = await _caseService.GetAssignedCasesAsync(_testJudgeId);

        Assert.True(result.Succeeded);
        Assert.Single(result.Payload.ReservedJudgments);
        Assert.Single(result.Payload.ScheduledContinuations);
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

    private Case CreateCase(string reason)
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
            PartId = _faker.Random.AlphaNumeric(10)
        };
    }

}