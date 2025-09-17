using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using Moq;
using Scv.Api.Models;
using Scv.Api.Processors;
using Scv.Db.Contants;
using Xunit;

namespace tests.api.Processors;

public class KeyDocumentsBinderProcessorTests
{
    private readonly ClaimsPrincipal _mockUser;
    private readonly Mock<IValidator<BinderDto>> _mockValidator;

    public KeyDocumentsBinderProcessorTests()
    {
        var identity = new ClaimsIdentity([], "HELLO");
        _mockUser = new ClaimsPrincipal(identity);
        _mockValidator = new Mock<IValidator<BinderDto>>();

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<BinderDto>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
    }

    [Fact(Skip = "Will fix after the core implementation has been approved.")]
    public void PreProcessAsync_Should_Not_Clear_Labels()
    {
        var processor = new KeyDocumentsBinderProcessor(
            null,
            _mockUser,
            _mockValidator.Object,
            new BinderDto
            {
                Labels = new Dictionary<string, string>
                {
                    { "TEST_KEY", "TEST_VALUE" }
                }
            },
            null, null, null, null, null);

        Assert.Single(processor.Binder.Labels);
    }

    [Fact(Skip = "Will fix after the core implementation has been approved.")]
    public async Task ValidateAsync_Should_Return_Error_When_Required_Labels_Are_Missing()
    {

        var processor = new KeyDocumentsBinderProcessor(
            null,
            _mockUser,
            _mockValidator.Object,
            new BinderDto
            {
                Labels = new Dictionary<string, string>
                {
                    { "TEST_KEY", "TEST_VALUE" }
                }
            },
            null, null, null, null, null);
        var result = await processor.ValidateAsync();

        Assert.False(result.Succeeded);
        Assert.Contains($"Missing label: {LabelConstants.PARTICIPANT_ID}", result.Errors);
        Assert.Contains($"Missing label: {LabelConstants.COURT_CLASS_CD}", result.Errors);
        Assert.Contains($"Missing label: {LabelConstants.APPEARANCE_ID}", result.Errors);
        Assert.Contains($"Missing label: {LabelConstants.PHYSICAL_FILE_ID}", result.Errors);
    }

    [Fact(Skip = "Will fix after the core implementation has been approved.")]
    public async Task ValidateAsync_Should_Return_Successful_When_Required_Fields_Are_There()
    {
        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<BinderDto>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var processor = new KeyDocumentsBinderProcessor(
            null,
            _mockUser,
            _mockValidator.Object,
            new BinderDto
            {
                Labels = new Dictionary<string, string>
                {
                    { LabelConstants.PARTICIPANT_ID, "" },
                    { LabelConstants.COURT_CLASS_CD, "" },
                    { LabelConstants.APPEARANCE_ID, "" },
                    { LabelConstants.PHYSICAL_FILE_ID, "" }
                }
            },
            null, null, null, null, null);
        var result = await processor.ValidateAsync();

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }
}
