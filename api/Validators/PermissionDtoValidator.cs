using FluentValidation;
using Scv.Api.Models.AccessControlManagement;

namespace Scv.Api.Validators;

public class PermissionDtoValidator : AccessControlManagementDtoValidator<PermissionDto>
{
    public PermissionDtoValidator() : base()
    {
        // "Code" is not validated intentionally. Changes to it are explicitly ignored in AccessControlManagement Profile.
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Permission name is required.");
        RuleFor(r => r.Description)
            .NotEmpty().WithMessage("Description is required.");
        RuleFor(r => r.IsActive)
            .NotNull().WithMessage("IsActive is required.");
    }
}
