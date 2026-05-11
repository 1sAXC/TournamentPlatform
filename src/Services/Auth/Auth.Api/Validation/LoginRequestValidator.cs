using Auth.Application.Auth.Dto;
using FluentValidation;

namespace Auth.Api.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Login)
            .NotEmpty();

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}
