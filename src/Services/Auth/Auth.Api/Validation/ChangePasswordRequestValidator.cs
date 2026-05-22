using Auth.Application.Auth.Dto;
using FluentValidation;

namespace Auth.Api.Validation;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty();

        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Za-z]")
            .WithMessage("New password must contain at least one letter.")
            .Matches("[0-9]")
            .WithMessage("New password must contain at least one digit.");
    }
}
