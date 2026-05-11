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
    public int? WinnerScore { get; private set; }
    public int? LoserScore { get; private set; }
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
        int winnerScore,
        int loserScore,
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
