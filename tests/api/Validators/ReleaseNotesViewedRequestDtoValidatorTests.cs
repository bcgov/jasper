using FluentValidation.TestHelper;
using Scv.Api.Models.AccessControlManagement;
using Scv.Api.Validators;
using Xunit;

namespace tests.api.Validators;

public class ReleaseNotesViewedRequestDtoValidatorTests
{
    private readonly ReleaseNotesViewedRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenVersionIsNull_ShouldFail()
    {
        var request = new ReleaseNotesViewedRequestDto
        {
            Version = null,
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Version)
            .WithErrorMessage("Version is required.");
    }

    [Fact]
    public void Validate_WhenVersionIsEmpty_ShouldFail()
    {
        var request = new ReleaseNotesViewedRequestDto
        {
            Version = string.Empty,
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Version)
            .WithErrorMessage("Version is required.");
    }

    [Fact]
    public void Validate_WhenVersionIsWhitespace_ShouldFail()
    {
        var request = new ReleaseNotesViewedRequestDto
        {
            Version = "   ",
        };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Version)
            .WithErrorMessage("Version is required.");
    }

    [Fact]
    public void Validate_WhenVersionIsProvided_ShouldPass()
    {
        var request = new ReleaseNotesViewedRequestDto
        {
            Version = "1.0.0",
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.Version);
    }
}