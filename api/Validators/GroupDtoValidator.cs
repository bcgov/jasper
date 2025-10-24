using FluentValidation;
using MongoDB.Bson;
using Scv.Models.AccessControlManagement;

namespace Scv.Api.Validators;

public class GroupDtoValidator : BaseDtoValidator<GroupDto>
{
    public GroupDtoValidator() : base()
    {
        RuleFor(r => r.Name)
            .NotEmpty().WithMessage("Group name is required.");
        RuleFor(r => r.Description)
            .NotEmpty().WithMessage("Description is required.");
        RuleForEach(r => r.RoleIds)
            .Must(id => id != null && ObjectId.TryParse(id.ToString(), out _)).WithMessage("Found one or more invalid role IDs.");
    }
}
