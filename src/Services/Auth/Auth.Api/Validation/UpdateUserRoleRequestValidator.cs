using Auth.Application.Admin.Dto;
using FluentValidation;

namespace Auth.Api.Validation;

public sealed class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(request => request.Role)
            .NotEmpty();

        RuleFor(request => request.Nickname)
            .Length(3, 30)
            .When(request => !string.IsNullOrWhiteSpace(request.Nickname));

        RuleFor(request => request.OrganizerName)
            .Length(3, 100)
            .When(request => !string.IsNullOrWhiteSpace(request.OrganizerName));
    }
}
