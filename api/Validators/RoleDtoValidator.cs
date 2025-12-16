using FluentValidation;
using MongoDB.Bson;
using Scv.Models.AccessControlManagement;

namespace Scv.Api.Validators;

public class RoleDtoValidator : BaseDtoValidator<RoleDto>
{
    public RoleDtoValidator() : base()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Role name is required.");
        RuleFor(r => r.Description)
            .NotEmpty().WithMessage("Description is required.");
        RuleForEach(r => r.PermissionIds)
            .Must(id => id != null && ObjectId.TryParse(id.ToString(), out _)).WithMessage("Found one or more invalid permission IDs.");
    }
}
