using System.Linq;
using Bogus;
using FluentValidation;
using FluentValidation.TestHelper;
using MongoDB.Bson;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Validators;
using Xunit;

namespace tests.api.Validators;

public class UserDtoValidatorTests
{
    private readonly UserDtoValidator _validator = new();
    private readonly static Faker _faker = new();

    private static UserDto GetMockDto()
    {
        return new UserDto
        {
            Id = ObjectId.GenerateNewId().ToString(),
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Email = _faker.Person.Email,
            IsActive = _faker.Random.Bool()
        };
    }

    [Fact]
    public void Validate_WhenAllRequiredFieldsAreEmpty_ShouldFail()
    {
        var dto = GetMockDto();
        dto.FirstName = "";
        dto.LastName = "";
        dto.Email = "";

        var result = _validator.TestValidate(dto);

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Validate_WhenGroupIdsContainInvalidId_ShouldFail()
    {
        var dto = GetMockDto();
        dto.GroupIds = [_faker.Random.AlphaNumeric(5), ObjectId.GenerateNewId().ToString()];

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.GroupIds)
            .WithErrorMessage("Found one or more invalid group IDs.");
    }

    [Fact]
    public void Validate_WhenDtoHasNullPermissionId_ShouldFail()
    {
        var dto = GetMockDto();
        dto.GroupIds = [null, ObjectId.GenerateNewId().ToString()];

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.GroupIds)
            .WithErrorMessage("Found one or more invalid group IDs.");
    }

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var dto = GetMockDto();
        dto.Id = null;
        dto.GroupIds = [ObjectId.GenerateNewId().ToString(), ObjectId.GenerateNewId().ToString()];

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidateIsAdd_WhenIdIsMissing_ShouldPass()
    {
        var dto = GetMockDto();
        dto.Id = null;
        var context = new ValidationContext<UserDto>(dto);

        var result = _validator.Validate(context);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenEditingUserDtoIsValid_ShouldPass()
    {
        var dto = GetMockDto();
        dto.GroupIds = [ObjectId.GenerateNewId().ToString(), ObjectId.GenerateNewId().ToString()];
        var context = new ValidationContext<UserDto>(dto);
        context.RootContextData["IsEdit"] = true;
        context.RootContextData["RouteId"] = dto.Id;

        var result = _validator.Validate(context);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldFail()
    {
        var dto = GetMockDto();
        dto.Id = null;
        dto.Email = "invalidemail";

        var result = _validator.TestValidate(dto);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Invalid email format.", result.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void Validate_WhenEmailHasNoTopLevelDomain_ShouldFail()
    {
        var dto = GetMockDto();
        dto.Id = null;
        dto.Email = "sample@email";

        var result = _validator.TestValidate(dto);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Invalid email format.", result.Errors.Single().ErrorMessage);
    }
}
