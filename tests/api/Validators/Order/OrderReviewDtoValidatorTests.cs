using System;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Scv.Api.Validators.Order;
using Scv.Models.Order;
using Xunit;

namespace tests.api.Validators.Order;

public class OrderReviewDtoValidatorTests
{
    private readonly OrderReviewDtoValidator _validator;

    // %PDF file signature.
    private static readonly string ValidPdfBase64 = Convert.ToBase64String([0x25, 0x50, 0x44, 0x46]);
    // PNG file signature (unsupported type).
    private static readonly string InvalidTypeBase64 = Convert.ToBase64String([0x89, 0x50, 0x4E, 0x47]);

    public OrderReviewDtoValidatorTests()
    {
        _validator = new OrderReviewDtoValidator();
    }

    #region DocumentData Validation Tests

    [Fact]
    public async Task Validate_ShouldHaveError_WhenDocumentDataIsInvalidType()
    {
        var dto = new OrderReviewDto { DocumentData = InvalidTypeBase64 };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(o => o.DocumentData)
            .WithErrorMessage("Signed document must be a valid PDF, Word Document (.doc or .docx).");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenDocumentDataIsValidType()
    {
        var dto = new OrderReviewDto { DocumentData = ValidPdfBase64 };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldNotHaveValidationErrorFor(o => o.DocumentData);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenDocumentDataIsEmpty()
    {
        var dto = new OrderReviewDto { DocumentData = null };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldNotHaveValidationErrorFor(o => o.DocumentData);
    }

    #endregion

    #region SupportingDocumentData Validation Tests

    [Fact]
    public async Task Validate_ShouldHaveError_WhenSupportingDocumentDataIsInvalidType()
    {
        var dto = new OrderReviewDto { SupportingDocumentData = InvalidTypeBase64 };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldHaveValidationErrorFor(o => o.SupportingDocumentData)
            .WithErrorMessage("Supporting document must be a valid PDF, Word Document (.doc or .docx).");
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenSupportingDocumentDataIsValidType()
    {
        var dto = new OrderReviewDto { SupportingDocumentData = ValidPdfBase64 };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldNotHaveValidationErrorFor(o => o.SupportingDocumentData);
    }

    [Fact]
    public async Task Validate_ShouldNotHaveError_WhenSupportingDocumentDataIsEmpty()
    {
        var dto = new OrderReviewDto { SupportingDocumentData = null };

        var result = await _validator.TestValidateAsync(dto);

        result.ShouldNotHaveValidationErrorFor(o => o.SupportingDocumentData);
    }

    #endregion

    #region Null Payload Validation Tests

    [Fact]
    public async Task Validate_ShouldHaveError_WhenPayloadIsNull()
    {
        var result = await _validator.TestValidateAsync((OrderReviewDto)null);

        result.ShouldHaveValidationErrorFor(o => o)
            .WithErrorMessage("Order review payload is required.");
    }

    #endregion
}
