using FluentValidation;
using Scv.Models.TransitoryDocuments;

namespace Scv.Api.Validators.TransitoryDocuments;

public class GetDocumentsRequestValidator : AbstractValidator<GetDocumentsRequest>
{
    public GetDocumentsRequestValidator()
    {
        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("locationId is required and must be non-empty.");

        RuleFor(x => x.RoomCd)
            .NotEmpty().WithMessage("roomCd is required and must be non-empty.");

        RuleFor(x => x.Date)
            .NotNull().WithMessage("Date is required.");
    }
}