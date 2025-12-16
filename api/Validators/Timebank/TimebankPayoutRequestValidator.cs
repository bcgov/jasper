using FluentValidation;
using Scv.Models.Timebank;

namespace Scv.Api.Validators.Timebank;

public class TimebankPayoutRequestValidator : AbstractValidator<TimebankPayoutRequest>
{
    public TimebankPayoutRequestValidator()
    {
        RuleFor(x => x.Period)
            .GreaterThan(1900).WithMessage("Period must be a valid year.");

        RuleFor(x => x.JudgeId)
            .GreaterThan(0).WithMessage("Judge ID must be a positive integer.")
            .When(x => x.JudgeId.HasValue);

        RuleFor(x => x.Rate)
            .GreaterThan(0).WithMessage("Rate must be a positive number.");
    }
}