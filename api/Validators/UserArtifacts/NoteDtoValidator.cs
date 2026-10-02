using FluentValidation;

namespace Scv.Api.Validators.UserArtifacts;

public class NoteDtoValidator : BaseDtoValidator<NoteDto>
{
    public NoteDtoValidator() : base()
    {
        RuleFor(r => r.Content)
            .NotEmpty()
            .WithMessage("Note content is required.");
    }
}
