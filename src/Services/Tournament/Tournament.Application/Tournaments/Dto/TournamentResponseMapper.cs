using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Tournaments.Dto;

public static class TournamentResponseMapper
{
    public static TournamentDetailsResponse ToDetailsResponse(Domain.Tournaments.Tournament tournament)
    {
        return new TournamentDetailsResponse(
            tournament.Id,
            tournament.Title,
            tournament.Description,
            tournament.DisciplineCode,
            tournament.Format.ToString(),
            tournament.SwissRounds,
            tournament.TeamSize,
            tournament.MaxPlayers,
            tournament.OrganizerId,
            tournament.Status.ToString(),
            tournament.CurrentRoundNumber,
            tournament.ActiveParticipantsCount,
            tournament.CreatedAtUtc,
            tournament.StartedAtUtc,
            tournament.CompletedAtUtc,
            tournament.CancelledAtUtc,
            tournament.Participants.Select(participant => new TournamentParticipantResponse(
                participant.Id,
                participant.PlayerId,
                participant.PlayerNickname,
                participant.RegisteredAtUtc,
                participant.LeftAtUtc,
                participant.IsActive)).ToArray(),
            tournament.Teams
                .OrderBy(team => team.Seed)
                .Select(team => new TeamResponse(
                    team.Id,
                    team.Name,
                    team.CaptainPlayerId,
                    team.Seed,
                    team.AverageElo,
                    team.Members
                        .OrderByDescending(member => member.Elo)
                        .Select(member => new TeamMemberResponse(
                            member.PlayerId,
                            member.Nickname,
                            member.Elo))
                        .ToArray()))
                .ToArray(),
            tournament.Rounds
                .OrderBy(round => round.Number)
                .ThenBy(round => round.BracketType)
                .Select(round => new RoundResponse(
                    round.Id,
                    round.Number,
                    round.BracketType.ToString(),
                    round.Status.ToString(),
                    round.CreatedAtUtc,
                    round.CompletedAtUtc,
                    round.Matches
                        .OrderBy(match => match.MatchNumber)
                        .Select(match => new MatchResponse(
                            match.Id,
                            match.MatchNumber,
                            match.TeamAId,
                            match.TeamBId,
                            match.WinnerTeamId,
                            match.LoserTeamId,
                            match.Status.ToString(),
                            match.WinnerScore,
                            match.LoserScore,
                            match.IsTechnicalDefeat,
                            match.CreatedAtUtc,
                            match.CompletedAtUtc))
                        .ToArray()))
                .ToArray(),
            tournament.Status == TournamentStatus.Open && tournament.HasFreeSlots(),
            tournament.Status is TournamentStatus.Open or TournamentStatus.Full);
    }
}
