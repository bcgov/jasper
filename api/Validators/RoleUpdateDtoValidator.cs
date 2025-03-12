using FluentValidation;
using MongoDB.Bson;
using Scv.Api.Models.UserManagement;

namespace Scv.Api.Validators;

public class RoleUpdateValidator : AbstractValidator<RoleUpdateDto>
{
    public RoleUpdateValidator()
    {
        RuleFor(r => r.Id)
            .NotEmpty().WithMessage("ID is required.")
            .Must(id => ObjectId.TryParse(id.ToString(), out _)).WithMessage("Invalid ID.")
            .Must((dto, id, context) => BeTheSameAsRouteId(id, context)).WithMessage("Route ID should match the Role ID.");
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Role name is required.");
        RuleFor(r => r.Description)
            .NotEmpty().WithMessage("Description is required.");
        RuleForEach(r => r.PermissionIds)
            .Must(id => ObjectId.TryParse(id.ToString(), out _)).WithMessage("Found one or more invalid permission IDs.");
    }

    private static bool BeTheSameAsRouteId(string id, ValidationContext<RoleUpdateDto> context)
    {
        var routeId = context.RootContextData["RouteId"] as string;
        return routeId == id;
    }
}
