using FluentValidation;
using Tournament.Application.Tournaments.Dto;

namespace Tournament.Api.Validation;

public sealed class CreateTournamentRequestValidator : AbstractValidator<CreateTournamentRequest>
{
    private const int MaxTournamentPlayers = 120;

    public CreateTournamentRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .Length(5, 50)
            .Matches(@"^(?=.{5,50}$)(?!.* {2,})(?!.*-{2,})[A-Za-z0-9Ѐ-ӿ][A-Za-z0-9Ѐ-ӿ -]*[A-Za-z0-9Ѐ-ӿ]$");

        RuleFor(request => request.Description)
            .MaximumLength(150);

        RuleFor(request => request.DisciplineCode)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(request => request.Format)
            .NotEmpty()
            .Must(format => !string.IsNullOrWhiteSpace(format)
                && !char.IsDigit(format.Trim()[0])
                && Enum.TryParse<TournamentPlatform.Contracts.Enums.TournamentFormat>(
                    format,
                    ignoreCase: true,
                    out var parsed)
                && Enum.IsDefined(parsed))
            .WithMessage("Format is invalid.");

        RuleFor(request => request.TeamSize)
            .Must(teamSize => teamSize is 1 or 2 or 5)
            .WithMessage("TeamSize must be 1, 2 or 5.");

        RuleFor(request => request.MaxPlayers)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaxTournamentPlayers);

        RuleFor(request => request)
            .Must(request => request.MaxPlayers % request.TeamSize == 0)
            .When(request => request.TeamSize is 1 or 2 or 5)
            .WithMessage("MaxPlayers must be divisible by TeamSize.");

        RuleFor(request => request.SwissRounds)
            .GreaterThan(0)
            .When(request => string.Equals(request.Format, "Swiss", StringComparison.OrdinalIgnoreCase));

        RuleFor(request => request.SwissRounds)
            .Null()
            .When(request => !string.Equals(request.Format, "Swiss", StringComparison.OrdinalIgnoreCase));
    }
}
