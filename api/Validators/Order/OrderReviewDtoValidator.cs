using FluentValidation;
using FluentValidation.Results;
using Scv.Core.Helpers;
using Scv.Models.Order;

namespace Scv.Api.Validators.Order;

public class OrderReviewDtoValidator : AbstractValidator<OrderReviewDto>
{
    public OrderReviewDtoValidator()
    {
        RuleFor(x => x.DocumentData)
            .Must(documentData => DocumentHelper.IsPdfOrWordDocumentBase64(documentData))
            .When(x => !string.IsNullOrWhiteSpace(x.DocumentData))
            .WithMessage("Signed document must be a valid PDF, Word Document (.doc or .docx).");

        RuleFor(x => x.SupportingDocumentData)
            .Must(supportingDoc => DocumentHelper.IsPdfOrWordDocumentBase64(supportingDoc))
            .When(x => !string.IsNullOrWhiteSpace(x.SupportingDocumentData))
            .WithMessage("Supporting document must be a valid PDF, Word Document (.doc or .docx).");
    }

    protected override bool PreValidate(ValidationContext<OrderReviewDto> context, ValidationResult result)
    {
        if (context.InstanceToValidate == null)
        {
            result.Errors.Add(new ValidationFailure(string.Empty, "Order review payload is required."));
            return false;
        }

        return true;
    }
}
