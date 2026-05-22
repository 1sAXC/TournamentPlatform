using FluentValidation;
using Tournament.Application.Tournaments.Dto;

namespace Tournament.Api.Validation;

public sealed class AdminCreateTournamentRequestValidator : AbstractValidator<AdminCreateTournamentRequest>
{
    public AdminCreateTournamentRequestValidator()
    {
        RuleFor(request => request.OrganizerId)
            .NotEmpty();

        RuleFor(request => request.ToCreateTournamentRequest())
            .SetValidator(new CreateTournamentRequestValidator());
    }
}
