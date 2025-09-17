using FluentValidation;
using MongoDB.Bson;
using Scv.Api.Models.AccessControlManagement;

namespace Scv.Api.Validators;

public class UserDtoValidator : BaseDtoValidator<UserDto>
{
    private readonly string VALID_EMAIL_ADDRESS_PATTERN = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    public UserDtoValidator() : base()
    {
        RuleFor(r => r.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .Matches(VALID_EMAIL_ADDRESS_PATTERN).WithMessage("Invalid email format.");
        RuleFor(r => r.IsActive)
            .NotNull().WithMessage("IsActive is required.");
        RuleForEach(r => r.GroupIds)
            .Must(id => id != null && ObjectId.TryParse(id.ToString(), out _)).WithMessage("Found one or more invalid group IDs.");
    }
}
