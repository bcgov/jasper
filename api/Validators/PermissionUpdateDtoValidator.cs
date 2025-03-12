using FluentValidation;
using MongoDB.Bson;
using Scv.Api.Models.UserManagement;

namespace Scv.Api.Validators;

public class PermissionUpdateDtoValidator : AbstractValidator<PermissionUpdateDto>
{
    public PermissionUpdateDtoValidator()
    {
        RuleFor(r => r.Id)
           .NotEmpty().WithMessage("ID is required.")
           .Must(id => ObjectId.TryParse(id.ToString(), out _)).WithMessage("Invalid ID.")
           .Must((dto, id, context) => BeTheSameAsRouteId(id, context)).WithMessage("Route ID should match the Role ID.");
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Role name is required.");
        RuleFor(r => r.Description)
            .NotEmpty().WithMessage("Description is required.");
    }

    private static bool BeTheSameAsRouteId(string id, ValidationContext<PermissionUpdateDto> context)
    {
        var routeId = context.RootContextData["RouteId"] as string;
        return routeId == id;
    }
}
