using System.Linq;
using FluentValidation;
using Scv.Db.Contants;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Validators.UserArtifact;

public abstract class UserArtifactDtoValidator<TDto> : BaseDtoValidator<TDto> where TDto : UserArtifactDto
{
    protected UserArtifactDtoValidator(ArtifactType expectedType) : base()
    {
        RuleFor(r => r.ArtifactType)
            .Equal(expectedType)
            .WithMessage($"Artifact type must be '{expectedType}'.");

        RuleFor(r => r.Labels)
            .Cascade(CascadeMode.Stop)
            .Must((dto, labels, context) => !IsEdit(context) || labels is { Count: > 0 })
            .WithMessage("Labels cannot be null or empty.")
            .Must(labels => labels is null || labels.Keys.All(IsValidLabelKey))
            .WithMessage("Label keys cannot be empty, contain '.', or start with '$'.")
            .Must(labels => labels is not null && RequiredLabelKeys.All(labels.ContainsKey))
            .WithMessage($"Labels must contain {string.Join(" and ", RequiredLabelKeys)}.");
    }

    private static readonly string[] RequiredLabelKeys = [LabelConstants.PHYSICAL_FILE_ID, LabelConstants.DIVISION, LabelConstants.COURT_CLASS_CD];

    private static bool IsValidLabelKey(string key) =>
        !string.IsNullOrWhiteSpace(key) && !key.Contains('.') && !key.StartsWith('$');
}
