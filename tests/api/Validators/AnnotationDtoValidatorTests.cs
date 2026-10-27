using System.Collections.Generic;
using System.Linq;
using FluentValidation.TestHelper;
using Scv.Api.Validators.UserArtifact;
using Scv.Db.Contants;
using Scv.Models.UserArtifacts;
using Xunit;

namespace tests.api.Validators;

public class AnnotationDtoValidatorTests
{
    private readonly AnnotationDtoValidator _validator = new();

    private static AnnotationDto GetMockDto(string annotationData = """{"page":1}""") => new()
    {
        DocumentId = "doc-1",
        DocumentHash = "hash-1",
        AnnotationData = annotationData
    };

    [Theory]
    [InlineData("""{"page":1}""")]
    [InlineData("""[{"page":1},{"page":2}]""")]
    [InlineData("{}")]
    [InlineData("[]")]
    public void Validate_WhenAnnotationDataIsValidJson_ShouldPass(string json)
    {
        var result = _validator.TestValidate(GetMockDto(json));

        result.ShouldNotHaveValidationErrorFor(r => r.AnnotationData);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"page":1""")]
    [InlineData("""{page:1}""")]
    [InlineData("""{'page':1}""")]
    [InlineData("""{"page":1,}""")]
    [InlineData("<xml/>")]
    public void Validate_WhenAnnotationDataIsInvalidJson_ShouldHaveError(string json)
    {
        var result = _validator.TestValidate(GetMockDto(json));

        result.ShouldHaveValidationErrorFor(r => r.AnnotationData)
            .WithErrorMessage("Annotation data must be valid JSON.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenAnnotationDataIsEmpty_ShouldOnlyHaveRequiredError(string json)
    {
        var messages = _validator.TestValidate(GetMockDto(json)).Errors
            .Where(e => e.PropertyName == nameof(AnnotationDto.AnnotationData))
            .Select(e => e.ErrorMessage);

        Assert.Equal(["Annotation data is required."], messages);
    }

    [Fact]
    public void Validate_WhenDocumentIdIsMissing_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.DocumentId = null;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.DocumentId)
            .WithErrorMessage("Document ID is required.");
    }

    [Fact]
    public void Validate_WhenLabelKeyIsInvalid_ShouldHaveError()
    {
        var dto = GetMockDto();
        dto.Labels = new Dictionary<string, string> { ["a.b"] = "value" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Labels);
    }

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var dto = GetMockDto();
        dto.Labels = new Dictionary<string, string>
        {
            [LabelConstants.PHYSICAL_FILE_ID] = "1",
            [LabelConstants.DIVISION] = "R",
            [LabelConstants.COURT_CLASS_CD] = "R",
        };
        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
