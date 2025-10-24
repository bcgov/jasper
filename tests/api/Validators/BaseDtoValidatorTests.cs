using Bogus;
using FluentValidation;
using MongoDB.Bson;
using Scv.Models;
using Scv.Api.Validators;
using Xunit;

namespace tests.api.Validators;

public class BaseDtoValidatorTests
{
    private readonly BaseDtoValidator<BaseDto> _validator = new();
    private readonly static Faker _faker = new();

    private static BaseDto GetMockDto()
    {
        return new BaseDto
        {
            Id = ObjectId.GenerateNewId().ToString(),
        };
    }

    [Fact]
    public void ValidateIsEdit_WhenIdIsEmpty_ShouldHaveAnError()
    {
        var dto = GetMockDto();
        dto.Id = "";

        var context = new ValidationContext<BaseDto>(dto);
        context.RootContextData["IsEdit"] = true;

        var result = _validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("ID is required.", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void ValidateIsEdit_WhenIdIsNull_ShouldHaveAnError()
    {
        var dto = GetMockDto();
        dto.Id = null;

        var context = new ValidationContext<BaseDto>(dto);
        context.RootContextData["IsEdit"] = true;

        var result = _validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("ID is required.", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void ValidateIsEdit_WhenIdIsInvalid_ShouldHaveAnError()
    {
        var dto = GetMockDto();
        dto.Id = _faker.Random.AlphaNumeric(10);

        var context = new ValidationContext<BaseDto>(dto);
        context.RootContextData["IsEdit"] = true;

        var result = _validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Invalid ID.", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void ValidateIsEdit_WhenIdDoesNotMatchRouteId_ShouldHaveAnError()
    {
        var dto = GetMockDto();
        var context = new ValidationContext<BaseDto>(dto);
        context.RootContextData["IsEdit"] = true;
        context.RootContextData["RouteId"] = ObjectId.GenerateNewId().ToString();

        var result = _validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Route ID should match the ID.", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void ValidateUpdate_WhenDtoIsValid_ShouldPass()
    {
        var validId = ObjectId.GenerateNewId().ToString();
        var dto = GetMockDto();
        dto.Id = validId;

        var context = new ValidationContext<BaseDto>(dto);
        context.RootContextData["RouteId"] = validId;
        context.RootContextData["IsEdit"] = true;

        var result = _validator.Validate(context);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateIsAdd_WhenIdIsEmpty_ShouldHavePass()
    {
        var dto = GetMockDto();
        dto.Id = "";
        var context = new ValidationContext<BaseDto>(dto);

        var result = _validator.Validate(context);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateIsAdd_WhenIdIsNull_ShouldHavePass()
    {
        var dto = GetMockDto();
        dto.Id = null;
        var context = new ValidationContext<BaseDto>(dto);

        var result = _validator.Validate(context);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateIsAdd_WhenIdIsInvalid_ShouldFail()
    {
        var dto = GetMockDto();
        dto.Id = _faker.Random.AlphaNumeric(5);
        var context = new ValidationContext<BaseDto>(dto);

        var result = _validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("ID must be null when creating new data.", result.Errors[0].ErrorMessage);

    }
}
