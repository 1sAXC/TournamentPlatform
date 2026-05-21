using Google.OrTools.Sat;
using Microsoft.Extensions.Options;
using Tournament.Application.TeamBalancer;

namespace Tournament.Tests;

internal sealed class HintedCpSatAverageEloTeamBalancer(
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
        var playerIndexes = playerArray
            .Select((player, index) => new { player.PlayerId, Index = index })
            .ToDictionary(player => player.PlayerId, player => player.Index);
        var fallbackTeamsForHint = fallbackTeams
            .OrderBy(team => team.Members.Sum(member => member.Elo))
            .ThenBy(team => team.CaptainPlayerId)
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

        var model = new CpModel();
        var assignments = CreateAssignmentVariables(model, playerArray.Length, teamsCount);

        AddPlayerConstraints(model, assignments, playerArray.Length, teamsCount);
        AddTeamSizeConstraints(model, assignments, playerArray.Length, teamsCount, teamSize);
        AddHints(model, assignments, fallbackTeamsForHint, playerIndexes);

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
        var solvedBalancedTeams = CreateBalancedTeams(solvedTeams);

        return CalculateTeamTotalGap(solvedBalancedTeams) <= CalculateTeamTotalGap(fallbackTeams)
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

    private static void AddPlayerConstraints(CpModel model, BoolVar[,] assignments, int playersCount, int teamsCount)
    {
        for (var playerIndex = 0; playerIndex < playersCount; playerIndex++)
        {
            model.AddExactlyOne(Enumerable
                .Range(0, teamsCount)
                .Select(teamIndex => assignments[playerIndex, teamIndex]));
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
            model.Add(LinearExpr.Sum(Enumerable
                .Range(0, playersCount)
                .Select(playerIndex => assignments[playerIndex, teamIndex])) == teamSize);
        }
    }

    private static void AddHints(
        CpModel model,
        BoolVar[,] assignments,
        IReadOnlyList<BalancedTeam> fallbackTeamsForHint,
        IReadOnlyDictionary<Guid, int> playerIndexes)
    {
        var hintedAssignments = new HashSet<(int PlayerIndex, int TeamIndex)>();

        for (var teamIndex = 0; teamIndex < fallbackTeamsForHint.Count; teamIndex++)
        {
            foreach (var member in fallbackTeamsForHint[teamIndex].Members)
            {
                hintedAssignments.Add((playerIndexes[member.PlayerId], teamIndex));
            }
        }

        for (var playerIndex = 0; playerIndex < assignments.GetLength(0); playerIndex++)
        {
            for (var teamIndex = 0; teamIndex < assignments.GetLength(1); teamIndex++)
            {
                model.AddHint(assignments[playerIndex, teamIndex], hintedAssignments.Contains((playerIndex, teamIndex)));
            }
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
        var weights = players.Select(player => (long)player.Elo).ToArray();

        for (var teamIndex = 0; teamIndex < teamsCount; teamIndex++)
        {
            teamTotals[teamIndex] = model.NewIntVar(minPossibleTeamTotal, maxPossibleTeamTotal, $"team_total_{teamIndex}");
            model.Add(teamTotals[teamIndex] == LinearExpr.WeightedSum(Enumerable
                .Range(0, players.Count)
                .Select(playerIndex => assignments[playerIndex, teamIndex]), weights));
        }

        return teamTotals;
    }

    private static string BuildSolverParameters(TeamBalancingOptions options)
    {
        return $"max_time_in_seconds:{Math.Max(1, options.CpSatTimeLimitSeconds)} num_search_workers:{Math.Max(1, Environment.ProcessorCount)}";
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
