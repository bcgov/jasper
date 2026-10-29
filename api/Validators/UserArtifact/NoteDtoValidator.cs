using FluentValidation;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Validators.UserArtifact;

public class NoteDtoValidator : UserArtifactDtoValidator<NoteDto>
{
    public NoteDtoValidator() : base(ArtifactType.Note)
    {
        RuleFor(r => r.Content)
            .NotEmpty()
            .WithMessage("Note content is required.");
    }
}
