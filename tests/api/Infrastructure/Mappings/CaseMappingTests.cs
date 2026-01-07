using System;
using System.Globalization;
using Bogus;
using Mapster;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models;
using Scv.Models;
using Xunit;
using PCSSCommonConstants = PCSSCommon.Common.Constants;

namespace tests.api.Infrastructure.Mappings;

public class CaseMappingTests
{
    private readonly TypeAdapterConfig _config;
    private readonly Faker _faker;

    public CaseMappingTests()
    {
        Randomizer.Seed = new Random(42);
        _faker = new Faker();

        _config = new TypeAdapterConfig();
        new CaseMapping().Register(_config);
    }

    [Fact]
    public void CsvReservedJudgement_To_Case_MapsSuccessfully()
    {
        var csv = new CsvReservedJudgement
        {
            CourtFileNumber = _faker.Random.AlphaNumeric(10),
            AdjudicatorLastNameFirstName = $"{_faker.Name.LastName()}, {_faker.Name.FirstName()}",
            AppearanceDate = _faker.Date.Future(),
            FacilityCode = _faker.Address.CountryCode()
        };

        var result = csv.Adapt<Scv.Db.Models.Case>(_config);

        Assert.NotNull(result);
    }

    [Fact]
    public void Case_To_CaseDto_MapsUpdatedDate()
    {
        var now = _faker.Date.Recent();
        var caseEntity = new Scv.Db.Models.Case
        {
            Id = _faker.Random.Int(1, 1000).ToString(),
            Upd_Dtm = now,
            CourtFileNumber = _faker.Random.AlphaNumeric(10)
        };

        var result = caseEntity.Adapt<CaseDto>(_config);

        Assert.Equal(now, result.UpdatedDate);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_IgnoresId()
    {
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = _faker.Random.Int(1, 999),
            LastApprDt = "24-Oct-2025",
            NextApprDt = "25-Oct-2025",
            CourtClassCd = "S",
            FileNumberTxt = _faker.Random.AlphaNumeric(8),
            NextApprReason = "DEC",
            ProfPartId = _faker.Random.Double()
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        Assert.Null(result.Id);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_MapsAppearanceId()
    {
        var appearanceId = _faker.Random.Int(100, 999);
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = appearanceId,
            LastApprDt = "24-Oct-2025",
            NextApprDt = "25-Oct-2025",
            CourtClassCd = "S",
            FileNumberTxt = _faker.Random.AlphaNumeric(8)
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        Assert.Equal(appearanceId.ToString(), result.AppearanceId);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_ParsesAppearanceDate()
    {
        var dateString = "24-Oct-2025";
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = _faker.Random.Int(1, 999),
            LastApprDt = dateString,
            NextApprDt = "25-Oct-2025",
            CourtClassCd = "S",
            FileNumberTxt = _faker.Random.AlphaNumeric(8)
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        var expectedDate = DateTime.ParseExact(dateString, PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture);
        Assert.Equal(expectedDate, result.AppearanceDate);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_MapsCourtClass()
    {
        var courtClass = _faker.PickRandom("A", "S", "P", "F");
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = _faker.Random.Int(1, 999),
            LastApprDt = "24-Oct-2025",
            NextApprDt = "25-Oct-2025",
            CourtClassCd = courtClass,
            FileNumberTxt = _faker.Random.AlphaNumeric(8)
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        Assert.Equal(courtClass, result.CourtClass);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_MapsCourtFileNumber()
    {
        var fileNumber = _faker.Random.AlphaNumeric(10);
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = _faker.Random.Int(1, 999),
            LastApprDt = "24-Oct-2025",
            NextApprDt = "25-Oct-2025",
            CourtClassCd = "S",
            FileNumberTxt = fileNumber
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        Assert.Equal(fileNumber, result.CourtFileNumber);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_ConcatenatesFileNumber()
    {
        var courtClass = "S";
        var fileNumber = "12345";
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = _faker.Random.Int(1, 999),
            LastApprDt = "24-Oct-2025",
            NextApprDt = "25-Oct-2025",
            CourtClassCd = courtClass,
            FileNumberTxt = fileNumber
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        Assert.Equal($"{courtClass}-{fileNumber}", result.FileNumber);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_MapsReason()
    {
        var reason = _faker.PickRandom("DEC", "CNT", "ACT");
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = _faker.Random.Int(1, 999),
            LastApprDt = "24-Oct-2025",
            NextApprDt = "25-Oct-2025",
            CourtClassCd = "S",
            FileNumberTxt = _faker.Random.AlphaNumeric(8),
            NextApprReason = reason
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        Assert.Equal(reason, result.Reason);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_MapsPartId()
    {
        var partId = _faker.Random.Double();
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = _faker.Random.Int(1, 999),
            LastApprDt = "24-Oct-2025",
            NextApprDt = "25-Oct-2025",
            CourtClassCd = "S",
            FileNumberTxt = _faker.Random.AlphaNumeric(8),
            ProfPartId = partId
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        Assert.Equal(partId.ToString(), result.PartId);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_ParsesDueDate()
    {
        var dueDateString = "30-Nov-2025";
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = _faker.Random.Int(1, 999),
            LastApprDt = "24-Oct-2025",
            NextApprDt = dueDateString,
            CourtClassCd = "S",
            FileNumberTxt = _faker.Random.AlphaNumeric(8)
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        Assert.Equal(dueDateString, result.DueDate);
    }

    [Fact]
    public void CaseDto_To_Case_IgnoresId()
    {
        var dto = new CaseDto
        {
            Id = _faker.Random.Int(1, 999).ToString(),
            CourtFileNumber = _faker.Random.AlphaNumeric(10),
            FileNumber = $"S-{_faker.Random.AlphaNumeric(8)}"
        };

        var result = dto.Adapt<Scv.Db.Models.Case>(_config);

        Assert.NotEqual(dto.Id, result.Id);
        Assert.Equal(dto.CourtFileNumber, result.CourtFileNumber);
        Assert.Equal(dto.FileNumber, result.FileNumber);
    }

    [Fact]
    public void PCSSCase_To_CaseDto_CompleteMapping()
    {
        var appearanceId = 789;
        var lastApprDt = "15-Dec-2025";
        var nextApprDt = "20-Dec-2025";
        var courtClass = "P";
        var fileNumber = "98765";
        var reason = "ACT";
        var partId = _faker.Random.Double();

        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = appearanceId,
            LastApprDt = lastApprDt,
            NextApprDt = nextApprDt,
            CourtClassCd = courtClass,
            FileNumberTxt = fileNumber,
            NextApprReason = reason,
            ProfPartId = partId
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        Assert.Null(result.Id);
        Assert.Equal(appearanceId.ToString(), result.AppearanceId);
        Assert.Equal(DateTime.ParseExact(lastApprDt, PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture), result.AppearanceDate);
        Assert.Equal(courtClass, result.CourtClass);
        Assert.Equal(fileNumber, result.CourtFileNumber);
        Assert.Equal($"{courtClass}-{fileNumber}", result.FileNumber);
        Assert.Equal(reason, result.Reason);
        Assert.Equal(partId.ToString(), result.PartId);
        Assert.Equal(nextApprDt, result.DueDate);
    }

    [Theory]
    [InlineData("01-Jan-2025")]
    [InlineData("15-Jun-2025")]
    [InlineData("31-Dec-2025")]
    public void PCSSCase_To_CaseDto_ParsesVariousDates(string dateString)
    {
        var pcssCase = new PCSSCommon.Models.Case
        {
            NextApprId = _faker.Random.Int(1, 999),
            LastApprDt = dateString,
            NextApprDt = dateString,
            CourtClassCd = "S",
            FileNumberTxt = _faker.Random.AlphaNumeric(8)
        };

        var result = pcssCase.Adapt<CaseDto>(_config);

        var expectedDate = DateTime.ParseExact(dateString, PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture);
        Assert.Equal(expectedDate, result.AppearanceDate);
        Assert.Equal(dateString, result.DueDate);
    }

    [Fact]
    public void CaseDto_To_Case_MapsOtherProperties()
    {
        var dto = new CaseDto
        {
            Id = 999.ToString(),
            CourtFileNumber = "CV-2025-001",
            FileNumber = "S-12345",
            Reason = "DEC",
            PartId = "P123"
        };

        var result = dto.Adapt<Scv.Db.Models.Case>(_config);

        Assert.NotEqual(dto.Id, result.Id);
        Assert.Equal(dto.CourtFileNumber, result.CourtFileNumber);
        Assert.Equal(dto.FileNumber, result.FileNumber);
        Assert.Equal(dto.Reason, result.Reason);
        Assert.Equal(dto.PartId, result.PartId);
    }
}