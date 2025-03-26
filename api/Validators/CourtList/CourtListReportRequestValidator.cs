using FluentValidation;
using Scv.Api.Models.CourtList;

namespace Scv.Api.Validators.CourtList;

public class CourtListReportRequestValidator : AbstractValidator<CourtListReportRequest>
{
    public CourtListReportRequestValidator()
    {
        RuleFor(x => x.CourtDivision)
           .NotEmpty().WithMessage("Division is required.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.");

        RuleFor(x => x.LocationId)
            .GreaterThan(0).WithMessage("Location ID is invalid.");

        RuleFor(x => x.CourtClass)
            .NotEmpty().WithMessage("Class is required.");
    }
}
