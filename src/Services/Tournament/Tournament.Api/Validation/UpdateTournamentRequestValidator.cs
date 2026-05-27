using FluentValidation;
using Tournament.Application.Tournaments.Dto;

namespace Tournament.Api.Validation;

public sealed class UpdateTournamentRequestValidator : AbstractValidator<UpdateTournamentRequest>
{
    public UpdateTournamentRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200)
            .Matches(@"^(?!.* {2,})(?!.*-{2,})[A-Za-z0-9][A-Za-z0-9 -]*[A-Za-z0-9]$");

        RuleFor(request => request.Description)
            .MaximumLength(4000);
    }
}
