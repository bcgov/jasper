using FluentValidation;
using MongoDB.Bson;
using Scv.Api.Models.AccessControlManagement;

namespace Scv.Api.Validators;

public class AccessControlManagementDtoValidator<TDto> : AbstractValidator<TDto> where TDto : AccessControlManagementDto
{
    public AccessControlManagementDtoValidator()
    {
        RuleFor(r => r.Id)
            .Cascade(CascadeMode.Stop)
            .Must((dto, id, context) => !IsEdit(context) || !string.IsNullOrWhiteSpace(id)).WithMessage("ID is required.")
            .Must((dto, id, context) => !IsEdit(context) || ObjectId.TryParse(id.ToString(), out _)).WithMessage("Invalid ID.")
            .Must((dto, id, context) =>
            {
                if (!IsEdit(context))
                {
                    return true;
                }

                var routeId = context.RootContextData["RouteId"] as string;
                return id == routeId;

            }).WithMessage("Route ID should match the ID.");
    }

    protected static bool IsEdit(ValidationContext<TDto> context)
    {
        return context.RootContextData.TryGetValue("IsEdit", out var value) && (bool)value;
    }
}
