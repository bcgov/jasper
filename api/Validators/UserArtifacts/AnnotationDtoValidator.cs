using FluentValidation;

namespace Scv.Api.Validators.UserArtifacts;

public class AnnotationDtoValidator : BaseDtoValidator<AnnotationDto>
{
    public AnnotationDtoValidator() : base()
    {
        RuleFor(r => r.DocumentId)
            .NotEmpty()
            .WithMessage("Annotation document ID is required.");

        RuleFor(r => r.DocumentHash)
            .NotEmpty()
            .WithMessage("Annotation document hash is required.");
    }
}
