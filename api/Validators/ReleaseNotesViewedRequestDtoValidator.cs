using FluentValidation;
using Scv.Models.AccessControlManagement;

namespace Scv.Api.Validators;

public class ReleaseNotesViewedRequestDtoValidator : AbstractValidator<ReleaseNotesViewedRequestDto>
{
    public ReleaseNotesViewedRequestDtoValidator()
    {
        RuleFor(r => r.Version)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Version is required.")
            .Must(version => !string.IsNullOrWhiteSpace(version)).WithMessage("Version is required.");
    }
}