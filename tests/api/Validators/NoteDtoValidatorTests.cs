using System.Collections.Generic;
using FluentValidation;
using FluentValidation.TestHelper;
using MongoDB.Bson;
using Scv.Api.Validators.UserArtifact;
using Scv.Db.Contants;
using Scv.Models.UserArtifacts;
using Xunit;

namespace tests.api.Validators;

public class NoteDtoValidatorTests
{
    private const string RequiredKeysMessage = "Labels must contain physicalFileId, division, courtClassCd.";
    private const string InvalidKeyMessage = "Label keys cannot be empty, contain '.', or start with '$'.";

    private readonly NoteDtoValidator _validator = new();

    private static Dictionary<string, string> GetValidLabels() => new()
    {
        [LabelConstants.PHYSICAL_FILE_ID] = "123",
        [LabelConstants.DIVISION] = "R",
        [LabelConstants.COURT_CLASS_CD] = "C"
    };

    private static NoteDto GetMockDto() => new() { Content = "note content", Labels = GetValidLabels() };

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        var result = _validator.TestValidate(GetMockDto());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenContentIsEmpty_ShouldHaveError(string content)
    {
        var dto = GetMockDto();
        dto.Content = content;

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Content)
            .WithErrorMessage("Note content is required.");
    }

    private static ValidationContext<NoteDto> GetEditContext(NoteDto dto)
    {
        dto.Id = ObjectId.GenerateNewId().ToString();
        var context = new ValidationContext<NoteDto>(dto);
        context.RootContextData["IsEdit"] = true;
        context.RootContextData["RouteId"] = dto.Id;
        return context;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_WhenAddingWithNullOrEmptyLabels_ShouldRequireKeys(bool useNull)
    {
        var dto = GetMockDto();
        dto.Labels = useNull ? null : [];

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Labels)
            .WithErrorMessage(RequiredKeysMessage);
    }

    [Theory]
    [InlineData(LabelConstants.PHYSICAL_FILE_ID)]
    [InlineData(LabelConstants.COURT_CLASS_CD)]
    public void Validate_WhenARequiredLabelKeyIsMissing_ShouldHaveError(string missingKey)
    {
        var dto = GetMockDto();
        dto.Labels.Remove(missingKey);

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Labels)
            .WithErrorMessage(RequiredKeysMessage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_WhenEditingWithNullOrEmptyLabels_ShouldHaveError(bool useNull)
    {
        var dto = GetMockDto();
        dto.Labels = useNull ? null : [];

        var result = _validator.TestValidate(GetEditContext(dto));

        result.ShouldHaveValidationErrorFor(r => r.Labels)
            .WithErrorMessage("Labels cannot be null or empty.");
    }

    [Fact]
    public void Validate_WhenEditingWithValidLabels_ShouldPass()
    {
        var result = _validator.TestValidate(GetEditContext(GetMockDto()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a.b")]
    [InlineData("$set")]
    public void Validate_WhenLabelKeyIsInvalid_ShouldHaveError(string key)
    {
        var dto = GetMockDto();
        dto.Labels[key] = "value";

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(r => r.Labels)
            .WithErrorMessage(InvalidKeyMessage);
    }

    [Fact]
    public void Validate_WhenEditingWithInvalidLabelKey_ShouldHaveKeyError()
    {
        var dto = GetMockDto();
        dto.Labels["a.b"] = "value";

        var result = _validator.TestValidate(GetEditContext(dto));

        result.ShouldHaveValidationErrorFor(r => r.Labels)
            .WithErrorMessage(InvalidKeyMessage);
    }

    [Fact]
    public void Validate_WhenLabelsContainRequiredAndExtraKeys_ShouldPass()
    {
        var dto = GetMockDto();
        dto.Labels["case-id"] = "123";
        dto.Labels["a$b"] = "x";

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(r => r.Labels);
    }
}
