using Tournament.Api.Validation;
using Tournament.Application.Tournaments.Dto;
using TournamentPlatform.Contracts.Common;

namespace Tournament.Tests;

public sealed class AdminCreateTournamentRequestValidatorTests
{
    [Fact]
    public void Validate_ShouldRejectEmptyOrganizerId()
    {
        var validator = new AdminCreateTournamentRequestValidator();

        var result = validator.Validate(ValidRequest(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AdminCreateTournamentRequest.OrganizerId));
    }

    [Fact]
    public void Validate_ShouldReuseCreateTournamentRules()
    {
        var validator = new AdminCreateTournamentRequestValidator();

        var result = validator.Validate(ValidRequest(Guid.NewGuid()) with { MaxPlayers = 121 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith(nameof(AdminCreateTournamentRequest.MaxPlayers), StringComparison.Ordinal));
    }

    private static AdminCreateTournamentRequest ValidRequest(Guid organizerId)
    {
        return new AdminCreateTournamentRequest(
            organizerId,
            "Admin Cup",
            "Description",
            DisciplineCodes.CS2,
            "SingleElimination",
            null,
            1,
            16);
    }
}
