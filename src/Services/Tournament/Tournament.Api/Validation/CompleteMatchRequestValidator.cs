using FluentValidation;
using Tournament.Application.Matches;

namespace Tournament.Api.Validation;

public sealed class CompleteMatchRequestValidator : AbstractValidator<CompleteMatchRequest>
{
    public CompleteMatchRequestValidator()
    {
        RuleFor(request => request.WinnerTeamId)
            .NotEmpty();

        RuleFor(request => request)
            .Must(request => request.WinnerScore is null || request.LoserScore is null || request.WinnerScore > request.LoserScore)
            .WithMessage("WinnerScore must be greater than LoserScore.");

        RuleFor(request => request.WinnerScore)
            .GreaterThanOrEqualTo(0)
            .When(request => request.WinnerScore is not null);

        RuleFor(request => request.LoserScore)
            .GreaterThanOrEqualTo(0)
            .When(request => request.LoserScore is not null);
    }
}
