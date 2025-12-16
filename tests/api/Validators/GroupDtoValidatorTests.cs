using Bogus;
using FluentValidation;
using FluentValidation.TestHelper;
using MongoDB.Bson;
using Scv.Models.AccessControlManagement;
using Scv.Api.Validators;
using Xunit;

namespace tests.api.Validators;

public class GroupDtoValidatorTests
{
    private readonly GroupDtoValidator _validator = new();
    private readonly static Faker _faker = new();

    private static GroupDto GetMockDto()
    {
        return new GroupDto
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph(),
        };
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveAnError()
    {
        var dto = GetMockDto();
        dto.Name = "";

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage("Group name is required.");
    }

    [Fact]
    public void Validate_WhenNameIsNull_ShouldHaveAnError()
    {
        var dto = GetMockDto();
        dto.Name = null;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage("Group name is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsEmpty_ShouldHaveAnError()
    {
        var dto = GetMockDto();
        dto.Description = "";

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Description).WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_ShouldHaveAnError()
    {
        var dto = GetMockDto();
        dto.Description = null;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Description).WithErrorMessage("Description is required.");
    }


    [Fact]
    public void Validate_WhenRoleIdsContainInvalidId_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.RoleIds = [_faker.Random.AlphaNumeric(5), ObjectId.GenerateNewId().ToString()];

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.RoleIds)
            .WithErrorMessage("Found one or more invalid role IDs.");
    }

    [Fact]
    public void Validate_WhenDtoHasNullPermissionId_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.RoleIds = [null, ObjectId.GenerateNewId().ToString()];

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.RoleIds)
            .WithErrorMessage("Found one or more invalid role IDs.");
    }

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var dto = GetMockDto();
        dto.Id = null;
        dto.RoleIds = [ObjectId.GenerateNewId().ToString(), ObjectId.GenerateNewId().ToString()];

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ValidateIsAdd_WhenIdIsMissing_ShouldPass()
    {
        var dto = new GroupDto
        {
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph(),
        };
        var context = new ValidationContext<GroupDto>(dto);

        var result = _validator.Validate(context);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
