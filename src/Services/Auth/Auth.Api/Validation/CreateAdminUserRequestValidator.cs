using Auth.Application.Admin.Dto;
using FluentValidation;

namespace Auth.Api.Validation;

public sealed class CreateAdminUserRequestValidator : AbstractValidator<CreateAdminUserRequest>
{
    public CreateAdminUserRequestValidator()
    {
        RuleFor(request => request.Role)
            .NotEmpty();

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Za-z]")
            .WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one digit.");

        RuleFor(request => request.Nickname)
            .Length(3, 30)
            .When(request => !string.IsNullOrWhiteSpace(request.Nickname));

        RuleFor(request => request.OrganizerName)
            .Length(3, 100)
            .When(request => !string.IsNullOrWhiteSpace(request.OrganizerName));
    }
}
