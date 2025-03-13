using FluentValidation;
using Scv.Api.Models.UserManagement;

namespace Scv.Api.Validators;

public class PermissionDtoValidator : UserManagementDtoValidator<PermissionDto>
{
    public PermissionDtoValidator() : base()
    {
        // "Code" is not validated intentionally.Changes to it are explicitly ignored in UserManagementProfile.
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Permission name is required.");
        RuleFor(r => r.Description)
            .NotEmpty().WithMessage("Description is required.");
        RuleFor(r => r.IsActive)
            .NotNull().WithMessage("IsActive is required.");
    }
}
