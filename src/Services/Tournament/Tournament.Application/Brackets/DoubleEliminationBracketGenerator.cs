using Tournament.Domain.Tournaments;
using Tournament.Application.Tournaments.Abstractions;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Brackets;

// Canonical Double Elimination: Upper and Lower brackets progress with
// independent round numbering. UB has R = ceil(log2(N)) rounds, LB has
// 2·(R-1) rounds (alternating consolidation/absorption), then a Grand Final
// (with optional reset if the LB champion wins the first GF).
//
// LB feeder map:
//   LB R1: UB R1 losers
//   LB R(2j-1) (odd, j>=2): consolidation among LB R(2j-2) winners
//   LB R(2j)   (even, j>=1, j>=2 except k=2 case): LB R(2j-1) winners + UB R(j+1) losers
//     equivalently: LB Rk (k even) absorbs UB R(k/2 + 1) losers.
public sealed class DoubleEliminationBracketGenerator(IOutboxWriter outboxWriter)
    : BracketGeneratorBase(outboxWriter)
{
    protected override BracketType BracketType => BracketType.Upper;

    public override Task HandleMatchCompletedAsync(
        Domain.Tournaments.Tournament tournament,
        Match completedMatch,
        CancellationToken ct)
    {
        if (completedMatch.LoserTeamId is not null)
        {
            tournament.DoubleEliminationStandings
                .Single(standing => standing.TeamId == completedMatch.LoserTeamId)
                .AddLoss();
        }

        var round = FindRound(tournament, completedMatch);
        if (round is null || round.Matches.Any(match => match.Status != MatchStatus.Completed))
        {
            return Task.CompletedTask;
        }

        round.Complete(DateTime.UtcNow);

        if (round.BracketType == BracketType.GrandFinal)
        {
            HandleGrandFinalCompleted(tournament, round);
            return Task.CompletedTask;
        }

        TryAdvance(tournament);
        return Task.CompletedTask;
    }

    protected override void AddInitialStandings(Domain.Tournaments.Tournament tournament, IReadOnlyList<Team> teams)
    {
        if (tournament.DoubleEliminationStandings.Count > 0)
        {
            return;
        }

        foreach (var team in teams)
        {
            tournament.AddDoubleEliminationStanding(DoubleEliminationStanding.Create(tournament.Id, team.Id));
        }
    }

    private void TryAdvance(Domain.Tournaments.Tournament tournament)
    {
        var upperRoundCount = UpperRoundCount(tournament);
        var lowerRoundCount = LowerRoundCount(upperRoundCount);

        TryAdvanceUpper(tournament, upperRoundCount);
        TryAdvanceLower(tournament, lowerRoundCount);
        TrySpawnGrandFinal(tournament, upperRoundCount, lowerRoundCount);
    }

    private void TryAdvanceUpper(Domain.Tournaments.Tournament tournament, int upperRoundCount)
    {
        var lastUpper = tournament.Rounds
            .Where(r => r.BracketType == BracketType.Upper)
            .OrderByDescending(r => r.Number)
            .FirstOrDefault();
        if (lastUpper is null || lastUpper.Status != RoundStatus.Completed) return;
        if (lastUpper.Number >= upperRoundCount) return;

        var winners = lastUpper.Matches
            .Where(m => m.WinnerTeamId.HasValue)
            .Select(m => tournament.Teams.Single(t => t.Id == m.WinnerTeamId!.Value))
            .OrderByDescending(t => t.AverageElo)
            .ThenBy(t => t.Id)
            .ToArray();
        if (winners.Length < 2) return;

        CreateRound(tournament, lastUpper.Number + 1, BracketType.Upper, winners);
    }

    private void TryAdvanceLower(Domain.Tournaments.Tournament tournament, int lowerRoundCount)
    {
        for (var k = 1; k <= lowerRoundCount; k++)
        {
            if (tournament.Rounds.Any(r => r.BracketType == BracketType.Lower && r.Number == k))
            {
                continue;
            }

            var teams = TryBuildLowerRoundTeams(tournament, k);
            if (teams is null) return;
            if (teams.Length == 0) continue;

            CreateRound(tournament, k, BracketType.Lower, teams);
        }
    }

    // null  -> prerequisites not yet met, stop spawning subsequent rounds.
    // empty -> no teams (shouldn't happen in practice).
    private static Team[]? TryBuildLowerRoundTeams(Domain.Tournaments.Tournament tournament, int k)
    {
        IEnumerable<Guid>? teamIds = null;

        if (k == 1)
        {
            var ub1 = tournament.Rounds.SingleOrDefault(r =>
                r.BracketType == BracketType.Upper && r.Number == 1);
            if (ub1?.Status != RoundStatus.Completed) return null;
            teamIds = ub1.Matches.Where(m => m.LoserTeamId.HasValue).Select(m => m.LoserTeamId!.Value);
        }
        else if (k % 2 == 1)
        {
            // Consolidation: winners of the previous LB round.
            var prev = tournament.Rounds.SingleOrDefault(r =>
                r.BracketType == BracketType.Lower && r.Number == k - 1);
            if (prev?.Status != RoundStatus.Completed) return null;
            teamIds = prev.Matches.Where(m => m.WinnerTeamId.HasValue).Select(m => m.WinnerTeamId!.Value);
        }
        else
        {
            // Absorption: LB(k-1) winners + UB(k/2+1) losers.
            var prevLb = tournament.Rounds.SingleOrDefault(r =>
                r.BracketType == BracketType.Lower && r.Number == k - 1);
            var ubFeeder = tournament.Rounds.SingleOrDefault(r =>
                r.BracketType == BracketType.Upper && r.Number == k / 2 + 1);
            if (prevLb?.Status != RoundStatus.Completed || ubFeeder?.Status != RoundStatus.Completed)
            {
                return null;
            }

            var lbWinners = prevLb.Matches.Where(m => m.WinnerTeamId.HasValue).Select(m => m.WinnerTeamId!.Value);
            var ubLosers = ubFeeder.Matches.Where(m => m.LoserTeamId.HasValue).Select(m => m.LoserTeamId!.Value);
            teamIds = lbWinners.Concat(ubLosers);
        }

        return teamIds
            .Select(id => tournament.Teams.Single(t => t.Id == id))
            .OrderByDescending(t => t.AverageElo)
            .ThenBy(t => t.Id)
            .ToArray();
    }

    private void TrySpawnGrandFinal(Domain.Tournaments.Tournament tournament, int upperRoundCount, int lowerRoundCount)
    {
        if (tournament.Rounds.Any(r => r.BracketType == BracketType.GrandFinal)) return;
        if (upperRoundCount == 0) return;

        var ubFinal = tournament.Rounds.SingleOrDefault(r =>
            r.BracketType == BracketType.Upper && r.Number == upperRoundCount);
        if (ubFinal?.Status != RoundStatus.Completed) return;
        var ubFinalMatch = ubFinal.Matches.Single();
        var ubChampion = ubFinalMatch.WinnerTeamId;

        Guid? lbChampion;
        if (lowerRoundCount == 0)
        {
            // N=2: there is no LB structure, so the UB R1 loser advances to GF
            // straight away as the LB champion.
            lbChampion = ubFinalMatch.LoserTeamId;
        }
        else
        {
            var lbFinal = tournament.Rounds.SingleOrDefault(r =>
                r.BracketType == BracketType.Lower && r.Number == lowerRoundCount);
            if (lbFinal?.Status != RoundStatus.Completed) return;
            lbChampion = lbFinal.Matches.Single().WinnerTeamId;
        }

        if (ubChampion is null || lbChampion is null) return;

        var ubTeam = tournament.Teams.Single(t => t.Id == ubChampion.Value);
        var lbTeam = tournament.Teams.Single(t => t.Id == lbChampion.Value);
        // Seed the UB champion first — their unbeaten path earns the top
        // bracket slot in the GF.
        CreateRound(tournament, 1, BracketType.GrandFinal, new[] { ubTeam, lbTeam });
    }

    private void HandleGrandFinalCompleted(Domain.Tournaments.Tournament tournament, Round gfRound)
    {
        var gfMatch = gfRound.Matches.Single();
        var winner = gfMatch.WinnerTeamId!.Value;
        var loser = gfMatch.LoserTeamId!.Value;
        var upperRoundCount = UpperRoundCount(tournament);
        var ubFinal = tournament.Rounds.Single(r =>
            r.BracketType == BracketType.Upper && r.Number == upperRoundCount);
        var ubChampion = ubFinal.Matches.Single().WinnerTeamId!.Value;

        if (gfRound.Number == 1 && winner != ubChampion)
        {
            // LB champion won the first GF. UB champion now has their first
            // loss → both teams have one loss → bracket reset.
            var winnerTeam = tournament.Teams.Single(t => t.Id == winner);
            var loserTeam = tournament.Teams.Single(t => t.Id == loser);
            CreateRound(tournament, 2, BracketType.GrandFinal, new[] { winnerTeam, loserTeam });
            return;
        }

        CompleteWithStandings(tournament, winner, loser);
    }

    private void CompleteWithStandings(Domain.Tournaments.Tournament tournament, Guid champion, Guid runnerUp)
    {
        var standings = new List<Guid> { champion, runnerUp };
        // 3rd onward by LB elimination round, latest first (loser of LB Final
        // is 3rd, loser of preceding LB round is 4th, etc.).
        var lbRoundsReversed = tournament.Rounds
            .Where(r => r.BracketType == BracketType.Lower)
            .OrderByDescending(r => r.Number);
        foreach (var round in lbRoundsReversed)
        {
            foreach (var match in round.Matches.Where(m => m.LoserTeamId.HasValue))
            {
                standings.Add(match.LoserTeamId!.Value);
            }
        }
        CompleteTournament(tournament, standings);
    }

    private static int UpperRoundCount(Domain.Tournaments.Tournament tournament)
    {
        var teamCount = tournament.Teams.Count;
        return teamCount <= 1 ? 0 : (int)Math.Ceiling(Math.Log2(teamCount));
    }

    private static int LowerRoundCount(int upperRoundCount)
    {
        return upperRoundCount <= 1 ? 0 : 2 * (upperRoundCount - 1);
    }
}
