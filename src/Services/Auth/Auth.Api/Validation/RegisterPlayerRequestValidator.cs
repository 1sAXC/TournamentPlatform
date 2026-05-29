using Auth.Application.Auth.Dto;
using FluentValidation;

namespace Auth.Api.Validation;

public sealed class RegisterPlayerRequestValidator : AbstractValidator<RegisterPlayerRequest>
{
    public RegisterPlayerRequestValidator()
    {
        RuleFor(request => request.Nickname)
            .NotEmpty()
            .Length(3, 30);

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

        RuleFor(request => request.ContactHandle)
            .NotEmpty()
            .Length(1, 64)
            .WithMessage("Contact handle must be 1-64 characters.");
    }
}
