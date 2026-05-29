using Auth.Application.Auth.Dto;
using FluentValidation;

namespace Auth.Api.Validation;

public sealed class UpdateContactHandleRequestValidator : AbstractValidator<UpdateContactHandleRequest>
{
    public UpdateContactHandleRequestValidator()
    {
        RuleFor(request => request.ContactHandle)
            .NotEmpty()
            .Length(1, 64)
            .WithMessage("Contact handle must be 1-64 characters.");
    }
}
