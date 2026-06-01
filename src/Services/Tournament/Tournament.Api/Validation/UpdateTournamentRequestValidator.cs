using FluentValidation;
using Tournament.Application.Tournaments.Dto;

namespace Tournament.Api.Validation;

public sealed class UpdateTournamentRequestValidator : AbstractValidator<UpdateTournamentRequest>
{
    public UpdateTournamentRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .Length(5, 50)
            .Matches(@"^(?=.{5,50}$)(?!.* {2,})(?!.*-{2,})[A-Za-z0-9Ѐ-ӿ][A-Za-z0-9Ѐ-ӿ -]*[A-Za-z0-9Ѐ-ӿ]$");

        RuleFor(request => request.Description)
            .MaximumLength(150);
    }
}
