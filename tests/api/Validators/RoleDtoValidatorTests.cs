using Bogus;
using FluentValidation;
using FluentValidation.TestHelper;
using MongoDB.Bson;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Validators;
using Xunit;

namespace tests.api.Validators;

public class RoleDtoValidatorTests
{
    private readonly RoleDtoValidator _validator = new();
    private readonly static Faker _faker = new();

    private static RoleDto GetMockDto()
    {
        return new RoleDto
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = _faker.Random.AlphaNumeric(10),
            Description = _faker.Lorem.Paragraph(),
        };
    }

    [Fact]
    public void Validate_WhenPermissionIdsContainInvalidId_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.PermissionIds = [_faker.Random.AlphaNumeric(5), ObjectId.GenerateNewId().ToString()];

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.PermissionIds)
            .WithErrorMessage("Found one or more invalid permission IDs.");
    }

    [Fact]
    public void Validate_WhenDtoHasNullPermissionId_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.PermissionIds = [null, ObjectId.GenerateNewId().ToString()];

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.PermissionIds)
            .WithErrorMessage("Found one or more invalid permission IDs.");
    }

    [Fact]
    public void Validate_WhenAddingRoleDtoIsValid_ShouldPass()
    {
        var dto = GetMockDto();
        dto.Id = null;
        dto.PermissionIds = [ObjectId.GenerateNewId().ToString(), ObjectId.GenerateNewId().ToString()];

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenEditingRoleDtoIsValid_ShouldPass()
    {
        var dto = GetMockDto();
        dto.PermissionIds = [ObjectId.GenerateNewId().ToString(), ObjectId.GenerateNewId().ToString()];
        var context = new ValidationContext<RoleDto>(dto);
        context.RootContextData["IsEdit"] = true;
        context.RootContextData["RouteId"] = dto.Id;

        var result = _validator.Validate(context);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WhenNameIsNull_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.Name = null;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Name)
            .WithErrorMessage("Role name is required.");
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.Name = "";

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Name)
            .WithErrorMessage("Role name is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.Description = null;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Description)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsEmpty_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.Description = "";

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Description)
            .WithErrorMessage("Description is required.");
    }
}
