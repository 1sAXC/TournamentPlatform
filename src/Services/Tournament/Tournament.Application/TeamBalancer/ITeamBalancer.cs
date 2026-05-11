namespace Tournament.Application.TeamBalancer;

public interface ITeamBalancer
{
    IReadOnlyList<BalancedTeam> BuildTeams(
        IReadOnlyCollection<PlayerForBalancing> players,
        int teamSize,
        string disciplineCode,
        Guid tournamentId);
}
