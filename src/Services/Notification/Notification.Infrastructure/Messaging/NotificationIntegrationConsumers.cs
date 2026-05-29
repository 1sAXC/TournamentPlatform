using Microsoft.Extensions.Logging;
using Notification.Application.Notifications.Services;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Abstractions;

namespace Notification.Infrastructure.Messaging;

public sealed class NotificationRoundCreatedConsumer(
    IRoundCreatedFanout fanout,
    ILogger<NotificationRoundCreatedConsumer> logger)
    : IIntegrationEventConsumer<RoundCreatedEvent>
{
    public async Task ConsumeAsync(RoundCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "NotificationService received RoundCreated for tournament {TournamentId} round {RoundNumber} ({Matches} matches)",
            integrationEvent.TournamentId,
            integrationEvent.RoundNumber,
            integrationEvent.Matches.Count);

        var created = await fanout.FanoutAsync(integrationEvent, cancellationToken);

        logger.LogInformation(
            "NotificationService fanned out {Count} match-created notifications for event {EventId}",
            created,
            integrationEvent.EventId);
    }
}
