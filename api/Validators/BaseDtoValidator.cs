using FluentValidation;
using MongoDB.Bson;
using Scv.Models;

namespace Scv.Api.Validators;

public class BaseDtoValidator<TDto> : AbstractValidator<TDto> where TDto : BaseDto
{
    public BaseDtoValidator()
    {
        RuleFor(r => r.Id)
            .Cascade(CascadeMode.Stop)
            .Must((dto, id, context) => IsEdit(context) || string.IsNullOrWhiteSpace(id)).WithMessage("ID must be null when creating new data.")
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
