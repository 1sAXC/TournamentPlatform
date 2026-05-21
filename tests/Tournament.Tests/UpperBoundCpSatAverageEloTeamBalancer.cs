using Google.OrTools.Sat;
using Microsoft.Extensions.Options;
using Tournament.Application.TeamBalancer;

namespace Tournament.Tests;

internal sealed class UpperBoundCpSatAverageEloTeamBalancer(
    GreedyTeamBalancer fallbackBalancer,
    IRandomProvider randomProvider,
    IOptions<TeamBalancingOptions> options) : ITeamBalancer
{
    public CpSatTeamBalancingDiagnostics? LastDiagnostics { get; private set; }

    public IReadOnlyList<BalancedTeam> BuildTeams(
        IReadOnlyCollection<PlayerForBalancing> players,
        int teamSize,
        string disciplineCode,
        Guid tournamentId)
    {
        LastDiagnostics = null;
        var fallbackTeams = fallbackBalancer.BuildTeams(players, teamSize, disciplineCode, tournamentId);

        if (teamSize == 1)
        {
            return fallbackTeams;
        }

        var playerArray = players
            .OrderByDescending(player => player.Elo)
            .ThenBy(player => player.Nickname, StringComparer.Ordinal)
            .ThenBy(player => player.PlayerId)
            .ToArray();
        var teamsCount = playerArray.Length / teamSize;
        var totalElo = playerArray.Sum(player => (long)player.Elo);
        var fallbackGap = CalculateTeamTotalGap(fallbackTeams);
        var minPossibleTeamTotal = playerArray
            .OrderBy(player => player.Elo)
            .Take(teamSize)
            .Sum(player => (long)player.Elo);
        var maxPossibleTeamTotal = playerArray
            .OrderByDescending(player => player.Elo)
            .Take(teamSize)
            .Sum(player => (long)player.Elo);

        var model = new CpModel();
        var assignments = CreateAssignmentVariables(model, playerArray.Length, teamsCount);

        for (var playerIndex = 0; playerIndex < playerArray.Length; playerIndex++)
        {
            model.AddExactlyOne(Enumerable
                .Range(0, teamsCount)
                .Select(teamIndex => assignments[playerIndex, teamIndex]));
        }

        for (var teamIndex = 0; teamIndex < teamsCount; teamIndex++)
        {
            model.Add(LinearExpr.Sum(Enumerable
                .Range(0, playerArray.Length)
                .Select(playerIndex => assignments[playerIndex, teamIndex])) == teamSize);
        }

        var weights = playerArray.Select(player => (long)player.Elo).ToArray();
        var teamTotals = new IntVar[teamsCount];

        for (var teamIndex = 0; teamIndex < teamsCount; teamIndex++)
        {
            teamTotals[teamIndex] = model.NewIntVar(minPossibleTeamTotal, maxPossibleTeamTotal, $"team_total_{teamIndex}");
            model.Add(teamTotals[teamIndex] == LinearExpr.WeightedSum(Enumerable
                .Range(0, playerArray.Length)
                .Select(playerIndex => assignments[playerIndex, teamIndex]), weights));
        }

        var maxTeamTotal = model.NewIntVar(minPossibleTeamTotal, maxPossibleTeamTotal, "max_team_total");
        var minTeamTotal = model.NewIntVar(minPossibleTeamTotal, maxPossibleTeamTotal, "min_team_total");
        var teamTotalGap = model.NewIntVar(0, fallbackGap, "team_total_gap");

        model.AddMaxEquality(maxTeamTotal, teamTotals);
        model.AddMinEquality(minTeamTotal, teamTotals);
        model.Add(teamTotalGap == maxTeamTotal - minTeamTotal);
        model.Add(teamTotalGap <= fallbackGap);
        model.Minimize(teamTotalGap);

        var solver = new CpSolver();
        solver.StringParameters = $"max_time_in_seconds:{Math.Max(1, options.Value.CpSatTimeLimitSeconds)} num_search_workers:{Math.Max(1, Environment.ProcessorCount)}";

        var status = solver.Solve(model);
        LastDiagnostics = new CpSatTeamBalancingDiagnostics(
            status.ToString(),
            status is CpSolverStatus.Optimal or CpSolverStatus.Feasible ? (long)solver.ObjectiveValue : null,
            solver.BestObjectiveBound);

        if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
        {
            return fallbackTeams;
        }

        var solvedTeams = ReadSolvedTeams(solver, assignments, playerArray, teamsCount);
        var solvedBalancedTeams = CreateBalancedTeams(solvedTeams);

        return CalculateTeamTotalGap(solvedBalancedTeams) <= fallbackGap
            ? solvedBalancedTeams
            : fallbackTeams;
    }

    private static BoolVar[,] CreateAssignmentVariables(CpModel model, int playersCount, int teamsCount)
    {
        var assignments = new BoolVar[playersCount, teamsCount];

        for (var playerIndex = 0; playerIndex < playersCount; playerIndex++)
        {
            for (var teamIndex = 0; teamIndex < teamsCount; teamIndex++)
            {
                assignments[playerIndex, teamIndex] = model.NewBoolVar($"x_{playerIndex}_{teamIndex}");
            }
        }

        return assignments;
    }

    private static IReadOnlyList<IReadOnlyCollection<PlayerForBalancing>> ReadSolvedTeams(
        CpSolver solver,
        BoolVar[,] assignments,
        IReadOnlyList<PlayerForBalancing> players,
        int teamsCount)
    {
        var teams = Enumerable.Range(0, teamsCount).Select(_ => new List<PlayerForBalancing>()).ToArray();

        for (var playerIndex = 0; playerIndex < players.Count; playerIndex++)
        {
            for (var teamIndex = 0; teamIndex < teamsCount; teamIndex++)
            {
                if (solver.BooleanValue(assignments[playerIndex, teamIndex]))
                {
                    teams[teamIndex].Add(players[playerIndex]);
                }
            }
        }

        return teams;
    }

    private IReadOnlyList<BalancedTeam> CreateBalancedTeams(IReadOnlyList<IReadOnlyCollection<PlayerForBalancing>> teams)
    {
        return teams
            .Select((team, index) => new { Team = team, Total = team.Sum(player => player.Elo), Index = index })
            .OrderBy(team => team.Total)
            .ThenBy(team => team.Index)
            .Select(team => CreateBalancedTeam(team.Team))
            .ToArray();
    }

    private BalancedTeam CreateBalancedTeam(IReadOnlyCollection<PlayerForBalancing> team)
    {
        var orderedPlayers = team
            .OrderByDescending(player => player.Elo)
            .ThenBy(player => player.Nickname, StringComparer.Ordinal)
            .ThenBy(player => player.PlayerId)
            .ToArray();
        var maxElo = orderedPlayers[0].Elo;
        var captainCandidates = orderedPlayers.Where(player => player.Elo == maxElo).ToArray();
        var captain = captainCandidates[randomProvider.Next(captainCandidates.Length)];

        return new BalancedTeam(
            captain.Nickname,
            captain.PlayerId,
            orderedPlayers.Select(player => new BalancedTeamMember(player.PlayerId, player.Nickname, player.Elo)).ToArray(),
            orderedPlayers.Average(player => player.Elo));
    }

    private static int CalculateTeamTotalGap(IReadOnlyCollection<BalancedTeam> teams)
    {
        var totals = teams.Select(team => team.Members.Sum(member => member.Elo)).ToArray();
        return totals.Max() - totals.Min();
    }
}
