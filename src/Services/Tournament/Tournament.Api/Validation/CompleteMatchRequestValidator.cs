using FluentValidation;
using Tournament.Application.Matches;

namespace Tournament.Api.Validation;

public sealed class CompleteMatchRequestValidator : AbstractValidator<CompleteMatchRequest>
{
    public CompleteMatchRequestValidator()
    {
        RuleFor(request => request.WinnerTeamId)
            .NotEmpty();

        // ----- Rounds (WinnerScore / LoserScore) -----
        RuleFor(request => request.WinnerScore)
            .NotNull()
            .When(request => !request.IsTechnicalDefeat)
            .WithMessage("WinnerScore is required unless the match is a technical defeat.");

        RuleFor(request => request.LoserScore)
            .NotNull()
            .When(request => !request.IsTechnicalDefeat)
            .WithMessage("LoserScore is required unless the match is a technical defeat.");

        RuleFor(request => request)
            .Must(request => request.WinnerScore is null || request.LoserScore is null || request.WinnerScore > request.LoserScore)
            .WithMessage("WinnerScore must be greater than LoserScore.");

        RuleFor(request => request.WinnerScore)
            .GreaterThanOrEqualTo(0)
            .When(request => request.WinnerScore is not null);

        RuleFor(request => request.LoserScore)
            .GreaterThanOrEqualTo(0)
            .When(request => request.LoserScore is not null);

        // ----- Maps (WinnerMaps / LoserMaps) -----
        RuleFor(request => request.WinnerMaps)
            .NotNull()
            .When(request => !request.IsTechnicalDefeat)
            .WithMessage("WinnerMaps is required unless the match is a technical defeat.");

        RuleFor(request => request.LoserMaps)
            .NotNull()
            .When(request => !request.IsTechnicalDefeat)
            .WithMessage("LoserMaps is required unless the match is a technical defeat.");

        RuleFor(request => request)
            .Must(request => request.WinnerMaps is null || request.LoserMaps is null || request.WinnerMaps > request.LoserMaps)
            .WithMessage("WinnerMaps must be greater than LoserMaps.");

        RuleFor(request => request.WinnerMaps)
            .GreaterThanOrEqualTo(0)
            .When(request => request.WinnerMaps is not null);

        RuleFor(request => request.LoserMaps)
            .GreaterThanOrEqualTo(0)
            .When(request => request.LoserMaps is not null);

        // ----- Consistency between maps and rounds -----
        // The team that won more maps must not have lost the round count.
        // Otherwise the result is internally inconsistent — a series winner
        // cannot have fewer total rounds than the loser of the series.
        RuleFor(request => request)
            .Must(request =>
                request.WinnerMaps is null
                || request.LoserMaps is null
                || request.WinnerScore is null
                || request.LoserScore is null
                || request.WinnerScore >= request.LoserScore)
            .WithMessage("Rounds total must not contradict the map score (series winner cannot lose by rounds).");
    }
}
