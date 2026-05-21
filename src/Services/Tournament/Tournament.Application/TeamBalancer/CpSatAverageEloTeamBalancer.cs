using Google.OrTools.Sat;
using Microsoft.Extensions.Options;

namespace Tournament.Application.TeamBalancer;

public sealed class CpSatAverageEloTeamBalancer(
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
        TeamBalancerBuilder.Validate(players, teamSize);
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
        var minPossibleTeamTotal = playerArray
            .OrderBy(player => player.Elo)
            .Take(teamSize)
            .Sum(player => (long)player.Elo);
        var maxPossibleTeamTotal = playerArray
            .OrderByDescending(player => player.Elo)
            .Take(teamSize)
            .Sum(player => (long)player.Elo);

        try
        {
            var model = new CpModel();
            var assignments = CreateAssignmentVariables(model, playerArray.Length, teamsCount);

            AddPlayerConstraints(model, assignments, playerArray.Length, teamsCount);
            AddTeamSizeConstraints(model, assignments, playerArray.Length, teamsCount, teamSize);

            var teamTotals = CreateTeamTotalVariables(
                model,
                assignments,
                playerArray,
                teamsCount,
                minPossibleTeamTotal,
                maxPossibleTeamTotal);
            var maxTeamTotal = model.NewIntVar(minPossibleTeamTotal, maxPossibleTeamTotal, "max_team_total");
            var minTeamTotal = model.NewIntVar(minPossibleTeamTotal, maxPossibleTeamTotal, "min_team_total");
            var teamTotalGap = model.NewIntVar(0, totalElo, "team_total_gap");

            model.AddMaxEquality(maxTeamTotal, teamTotals);
            model.AddMinEquality(minTeamTotal, teamTotals);
            model.Add(teamTotalGap == maxTeamTotal - minTeamTotal);
            model.Minimize(teamTotalGap);

            var solver = new CpSolver();
            solver.StringParameters = BuildSolverParameters(options.Value);

            var status = solver.Solve(model);
            LastDiagnostics = new CpSatTeamBalancingDiagnostics(
                status.ToString(),
                status is CpSolverStatus.Optimal or CpSolverStatus.Feasible
                    ? (long)solver.ObjectiveValue
                    : null,
                solver.BestObjectiveBound);

            if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
            {
                return fallbackTeams;
            }

            var solvedTeams = ReadSolvedTeams(solver, assignments, playerArray, teamsCount);
            TeamBalancerBuilder.EnsureValidTeams(solvedTeams, teamSize);
            if (CalculateTeamTotalGap(solvedTeams) > CalculateTeamTotalGap(fallbackTeams))
            {
                return fallbackTeams;
            }

            return TeamBalancerBuilder.CreateBalancedTeams(solvedTeams, teamSize, randomProvider);
        }
        catch (Exception)
        {
            return fallbackTeams;
        }
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

    private static void AddPlayerConstraints(
        CpModel model,
        BoolVar[,] assignments,
        int playersCount,
        int teamsCount)
    {
        for (var playerIndex = 0; playerIndex < playersCount; playerIndex++)
        {
            var playerAssignments = Enumerable
                .Range(0, teamsCount)
                .Select(teamIndex => assignments[playerIndex, teamIndex]);

            model.AddExactlyOne(playerAssignments);
        }
    }

    private static void AddTeamSizeConstraints(
        CpModel model,
        BoolVar[,] assignments,
        int playersCount,
        int teamsCount,
        int teamSize)
    {
        for (var teamIndex = 0; teamIndex < teamsCount; teamIndex++)
        {
            var teamAssignments = Enumerable
                .Range(0, playersCount)
                .Select(playerIndex => assignments[playerIndex, teamIndex]);

            model.Add(LinearExpr.Sum(teamAssignments) == teamSize);
        }
    }

    private static IntVar[] CreateTeamTotalVariables(
        CpModel model,
        BoolVar[,] assignments,
        IReadOnlyList<PlayerForBalancing> players,
        int teamsCount,
        long minPossibleTeamTotal,
        long maxPossibleTeamTotal)
    {
        var teamTotals = new IntVar[teamsCount];
        var weights = players
            .Select(player => (long)player.Elo)
            .ToArray();

        for (var teamIndex = 0; teamIndex < teamsCount; teamIndex++)
        {
            var teamAssignments = Enumerable
                .Range(0, players.Count)
                .Select(playerIndex => assignments[playerIndex, teamIndex]);

            teamTotals[teamIndex] = model.NewIntVar(minPossibleTeamTotal, maxPossibleTeamTotal, $"team_total_{teamIndex}");
            model.Add(teamTotals[teamIndex] == LinearExpr.WeightedSum(teamAssignments, weights));
        }

        return teamTotals;
    }

    private static string BuildSolverParameters(TeamBalancingOptions options)
    {
        var timeLimitSeconds = Math.Max(1, options.CpSatTimeLimitSeconds);
        var searchWorkers = Math.Max(1, Environment.ProcessorCount);

        return $"max_time_in_seconds:{timeLimitSeconds} num_search_workers:{searchWorkers}";
    }

    private static IReadOnlyList<IReadOnlyCollection<PlayerForBalancing>> ReadSolvedTeams(
        CpSolver solver,
        BoolVar[,] assignments,
        IReadOnlyList<PlayerForBalancing> players,
        int teamsCount)
    {
        var teams = Enumerable
            .Range(0, teamsCount)
            .Select(_ => new List<PlayerForBalancing>())
            .ToArray();

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

    private static int CalculateTeamTotalGap(IReadOnlyCollection<BalancedTeam> teams)
    {
        var totals = teams
            .Select(team => team.Members.Sum(member => member.Elo))
            .ToArray();

        return totals.Max() - totals.Min();
    }

    private static int CalculateTeamTotalGap(IReadOnlyCollection<IReadOnlyCollection<PlayerForBalancing>> teams)
    {
        var totals = teams
            .Select(team => team.Sum(player => player.Elo))
            .ToArray();

        return totals.Max() - totals.Min();
    }
}
