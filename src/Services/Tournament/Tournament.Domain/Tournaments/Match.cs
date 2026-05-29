using TournamentPlatform.Contracts.Enums;

namespace Tournament.Domain.Tournaments;

public sealed class Match
{
    private Match()
    {
    }

    private Match(
        Guid tournamentId,
        int matchNumber,
        Guid? teamAId,
        Guid? teamBId,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        TournamentId = tournamentId;
        MatchNumber = matchNumber;
        TeamAId = teamAId;
        TeamBId = teamBId;
        Status = MatchStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TournamentId { get; private set; }
    public Guid RoundId { get; private set; }
    public int MatchNumber { get; private set; }
    public Guid? TeamAId { get; private set; }
    public Guid? TeamBId { get; private set; }
    public Guid? WinnerTeamId { get; private set; }
    public Guid? LoserTeamId { get; private set; }
    public MatchStatus Status { get; private set; }
    // Score by rounds (sum across all maps of the series). This is the
    // granular signal the Rating service uses to weight the ELO delta.
    public int? WinnerScore { get; private set; }
    public int? LoserScore { get; private set; }

    // Score by maps in the series (e.g. 2-1 for Bo3). Display-only; the
    // bracket logic uses WinnerTeamId, the rating uses round score above.
    public int? WinnerMaps { get; private set; }
    public int? LoserMaps { get; private set; }
    public bool IsTechnicalDefeat { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Match Create(
        Guid tournamentId,
        int matchNumber,
        Guid? teamAId,
        Guid? teamBId,
        DateTime createdAtUtc)
    {
        return new Match(tournamentId, matchNumber, teamAId, teamBId, createdAtUtc);
    }

    public void Complete(
        Guid winnerTeamId,
        int? winnerScore,
        int? loserScore,
        int? winnerMaps,
        int? loserMaps,
        bool isTechnicalDefeat,
        DateTime completedAtUtc)
    {
        if (TeamAId != winnerTeamId && TeamBId != winnerTeamId)
        {
            throw new InvalidOperationException("Winner must be one of match teams.");
        }

        WinnerTeamId = winnerTeamId;
        LoserTeamId = TeamAId == winnerTeamId ? TeamBId : TeamAId;
        WinnerScore = winnerScore;
        LoserScore = loserScore;
        WinnerMaps = winnerMaps;
        LoserMaps = loserMaps;
        IsTechnicalDefeat = isTechnicalDefeat;
        CompletedAtUtc = completedAtUtc;
        Status = MatchStatus.Completed;
    }

    public void CompleteBye(Guid winnerTeamId, DateTime completedAtUtc)
    {
        WinnerTeamId = winnerTeamId;
        LoserTeamId = null;
        WinnerScore = null;
        LoserScore = null;
        WinnerMaps = null;
        LoserMaps = null;
        IsTechnicalDefeat = false;
        CompletedAtUtc = completedAtUtc;
        Status = MatchStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == MatchStatus.Completed)
        {
            return;
        }

        Status = MatchStatus.Cancelled;
    }
}
