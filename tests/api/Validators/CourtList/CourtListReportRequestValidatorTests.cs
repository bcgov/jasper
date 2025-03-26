using System;
using System.Linq;
using Bogus;
using FluentValidation.TestHelper;
using Scv.Api.Models.CourtList;
using Scv.Api.Validators.CourtList;
using Xunit;

namespace tests.api.Validators.CourtList;

public class CourtListReportRequestValidatorTests
{
    private readonly CourtListReportRequestValidator _validator = new();
    private readonly static Faker _faker = new();

    private static CourtListReportRequest GetMockData()
    {
        return new CourtListReportRequest
        {
            CourtDivision = _faker.Random.AlphaNumeric(1),
            CourtClass = _faker.Random.AlphaNumeric(1),
            Date = DateTime.Now,
            LocationId = _faker.Random.Int(1),
        };
    }

    [Fact]
    public void Validate_WhenCourtDivisionIsMissing_ShouldFail()
    {
        var data = GetMockData();
        data.CourtDivision = "";

        var result = _validator.TestValidate(data);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Division is required.", result.Errors.First().ErrorMessage);
    }

    [Fact]
    public void Validate_WhenLocationIdIsNull_ShouldFail()
    {
        var data = GetMockData();
        data.LocationId = null;

        var result = _validator.TestValidate(data);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Location ID is required.", result.Errors.First().ErrorMessage);
    }

    [Fact]
    public void Validate_WhenLocationIdIsZeroOrBelow_ShouldFail()
    {
        var data = GetMockData();
        data.LocationId = _faker.Random.Int(max: 0);

        var result = _validator.TestValidate(data);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Location ID is invalid.", result.Errors.First().ErrorMessage);
    }

    [Fact]
    public void Validate_WhenCourtClassIsMissing_ShouldFail()
    {
        var data = GetMockData();
        data.CourtClass = "";

        var result = _validator.TestValidate(data);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Class is required.", result.Errors.First().ErrorMessage);
    }

    [Fact]
    public void Validate_WhenDateIsNull_ShouldFail()
    {
        var data = GetMockData();
        data.Date = null;

        var result = _validator.TestValidate(data);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Date is required.", result.Errors.First().ErrorMessage);
    }
}
