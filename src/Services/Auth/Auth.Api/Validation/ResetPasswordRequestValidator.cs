using Auth.Application.Admin.Dto;
using FluentValidation;

namespace Auth.Api.Validation;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.TemporaryPassword)
            .MinimumLength(8)
            .Matches("[A-Za-z]")
            .WithMessage("Temporary password must contain at least one letter.")
            .Matches("[0-9]")
            .WithMessage("Temporary password must contain at least one digit.")
            .When(request => !string.IsNullOrWhiteSpace(request.TemporaryPassword));
    }
}
