using FluentValidation;
using MongoDB.Bson;
using Scv.Api.Models.UserManagement;

namespace Scv.Api.Validators;

public class RoleCreateDtoValidator : AbstractValidator<RoleCreateDto>
{
    public RoleCreateDtoValidator()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Role name is required.");
        RuleFor(r => r.Description)
            .NotEmpty().WithMessage("Description is required.");
        RuleForEach(r => r.PermissionIds)
            .Must(id => ObjectId.TryParse(id.ToString(), out _)).WithMessage("Found one or more invalid permission IDs.");
    }
}
