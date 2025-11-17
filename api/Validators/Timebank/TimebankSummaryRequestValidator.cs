using FluentValidation;
using Scv.Api.Models.Timebank;

namespace Scv.Api.Validators.Timebank;

public class TimebankSummaryRequestValidator : AbstractValidator<TimebankSummaryRequest>
{
    public TimebankSummaryRequestValidator()
    {
        RuleFor(x => x.Period)
            .GreaterThan(0).WithMessage("Period must be a positive integer.");

        RuleFor(x => x.JudgeId)
            .GreaterThan(0).WithMessage("Judge ID must be a positive integer.")
            .When(x => x.JudgeId.HasValue);
    }
}