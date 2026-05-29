using System.Text.Json;
using Notification.Application.Notifications.Abstractions;
using Notification.Domain.Notifications;
using TournamentPlatform.Contracts.Events;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Application.Notifications.Services;

public sealed class RoundCreatedFanout(INotificationRepository notifications) : IRoundCreatedFanout
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<int> FanoutAsync(
        RoundCreatedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        // Index team rosters by id so we can resolve TeamAId/TeamBId on each match.
        var teamsById = integrationEvent.Teams.ToDictionary(team => team.TeamId);
        var createdAt = DateTime.UtcNow;
        var created = 0;

        foreach (var match in integrationEvent.Matches)
        {
            if (match.TeamAId is null || match.TeamBId is null)
            {
                // Bye matches or unfilled brackets — no opponent, no notification.
                continue;
            }

            if (!teamsById.TryGetValue(match.TeamAId.Value, out var teamA)
                || !teamsById.TryGetValue(match.TeamBId.Value, out var teamB))
            {
                // Defensive: event payload is malformed; skip the match rather
                // than crash the consumer (which would dead-letter the event).
                continue;
            }

            created += await NotifyTeamAsync(integrationEvent, match, teamA, teamB, createdAt, cancellationToken);
            created += await NotifyTeamAsync(integrationEvent, match, teamB, teamA, createdAt, cancellationToken);
        }

        if (created > 0)
        {
            await notifications.SaveChangesAsync(cancellationToken);
        }

        return created;
    }

    private async Task<int> NotifyTeamAsync(
        RoundCreatedEvent integrationEvent,
        EventMatchDto match,
        EventTeamDto team,
        EventTeamDto opponent,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var title = $"Сформирован матч в турнире «{integrationEvent.TournamentTitle}»";
        var body = $"Ваша команда «{team.Name}» играет против «{opponent.Name}» в раунде {integrationEvent.RoundNumber}.";
        var linkUrl = $"/tournaments/{integrationEvent.TournamentId}/matches/{match.MatchId}";
        var payload = JsonSerializer.Serialize(new
        {
            tournamentId = integrationEvent.TournamentId,
            tournamentTitle = integrationEvent.TournamentTitle,
            roundNumber = integrationEvent.RoundNumber,
            matchId = match.MatchId,
            matchNumber = match.MatchNumber,
            teamId = team.TeamId,
            teamName = team.Name,
            opponentTeamId = opponent.TeamId,
            opponentTeamName = opponent.Name
        }, JsonOptions);

        var created = 0;
        foreach (var member in team.Members)
        {
            // Two-step dedup: inbox at the consumer level already prevents
            // double processing of the same EventId, but we also guard at the
            // entity level so that a partial fan-out (e.g. crash after some
            // rows persisted) becomes idempotent on retry.
            if (await notifications.ExistsForSourceEventAsync(integrationEvent.EventId, member.UserId, cancellationToken))
            {
                continue;
            }

            notifications.Add(NotificationEntity.Create(
                member.UserId,
                NotificationType.MatchCreated,
                title,
                body,
                linkUrl,
                payload,
                integrationEvent.EventId,
                createdAt));
            created++;
        }

        return created;
    }
}
