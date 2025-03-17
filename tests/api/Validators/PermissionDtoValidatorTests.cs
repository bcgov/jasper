using Bogus;
using FluentValidation;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Validators;
using Xunit;

namespace tests.api.Validators;

public class PermissionDtoValidatorTests
{
    private readonly PermissionDtoValidator _validator = new();
    private readonly Faker _faker;

    public PermissionDtoValidatorTests()
    {
        _faker = new Faker();
    }

    [Fact]
    public void Validate_WhenIsActiveIsMissing_ShouldHaveAnError()
    {
        var dto = new PermissionDto
        {
            Name = _faker.Random.Word(),
            Description = _faker.Lorem.Paragraph()
        };
        var context = new ValidationContext<PermissionDto>(dto);

        var result = _validator.Validate(context);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("IsActive is required.", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Validate_WhenIsActiveIsPresent_ShouldPass()
    {
        var dto = new PermissionDto
        {
            Name = _faker.Random.Word(),
            Description = _faker.Lorem.Paragraph(),
            IsActive = _faker.Random.Bool()
        };
        var context = new ValidationContext<PermissionDto>(dto);

        var result = _validator.Validate(context);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
