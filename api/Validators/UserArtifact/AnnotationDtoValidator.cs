using System.Text.Json;
using FluentValidation;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Validators.UserArtifact;

public class AnnotationDtoValidator : UserArtifactDtoValidator<AnnotationDto>
{
    public AnnotationDtoValidator() : base(ArtifactType.Annotation)
    {
        RuleFor(r => r.DocumentId)
            .NotEmpty()
            .WithMessage("Document ID is required.");

        RuleFor(r => r.DocumentHash)
            .NotEmpty()
            .WithMessage("Document hash is required.");

        RuleFor(r => r.AnnotationData)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Annotation data is required.")
            .Must(BeValidJson)
            .WithMessage("Annotation data must be valid JSON.");
    }

    private static bool BeValidJson(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
