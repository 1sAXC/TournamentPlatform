using Auth.Application.Auth.Dto;
using FluentValidation;

namespace Auth.Api.Validation;

public sealed class RegisterOrganizerRequestValidator : AbstractValidator<RegisterOrganizerRequest>
{
    public RegisterOrganizerRequestValidator()
    {
        RuleFor(request => request.OrganizerName)
            .NotEmpty()
            .Length(3, 100);

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}
