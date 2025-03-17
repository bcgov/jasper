using FluentValidation;
using MongoDB.Bson;
using Scv.Api.Models.AccessControlManagement;

namespace Scv.Api.Validators;

public class UserDtoValidator : AccessControlManagementDtoValidator<UserDto>
{
    public UserDtoValidator() : base()
    {
        RuleFor(r => r.FirstName)
            .NotEmpty().WithMessage("First name is required.");
        RuleFor(r => r.LastName)
            .NotEmpty().WithMessage("Last name is required.");
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");
        RuleFor(r => r.IsActive)
            .NotNull().WithMessage("IsActive is required.");
        RuleForEach(r => r.GroupIds)
            .Must(id => id != null && ObjectId.TryParse(id.ToString(), out _)).WithMessage("Found one or more invalid group IDs.");
    }
}
