using Microsoft.Extensions.Logging;
using Tournament.Application.Tournaments.Services;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Abstractions;

namespace Tournament.Infrastructure.Messaging;

public sealed class TournamentUserCreatedConsumer(
    IPlayerRatingProjectionService playerRatingProjectionService,
    IUserProjectionService userProjectionService,
    ILogger<TournamentUserCreatedConsumer> logger)
    : IIntegrationEventConsumer<UserCreatedEvent>
{
    public async Task ConsumeAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await userProjectionService.HandleUserCreatedAsync(integrationEvent, cancellationToken);
        await playerRatingProjectionService.HandleUserCreatedAsync(integrationEvent, cancellationToken);
        logger.LogInformation("TournamentService projected UserCreated for user {UserId}", integrationEvent.UserId);
    }
}

public sealed class TournamentRatingUpdatedConsumer(
    IPlayerRatingProjectionService playerRatingProjectionService,
    ILogger<TournamentRatingUpdatedConsumer> logger)
    : IIntegrationEventConsumer<RatingUpdatedEvent>
{
    public async Task ConsumeAsync(RatingUpdatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await playerRatingProjectionService.HandleRatingUpdatedAsync(integrationEvent, cancellationToken);
        logger.LogInformation("TournamentService projected RatingUpdated for user {UserId}", integrationEvent.UserId);
    }
}

public sealed class TournamentUserDeletedConsumer(
    IPlayerRatingProjectionService playerRatingProjectionService,
    IUserProjectionService userProjectionService,
    ILogger<TournamentUserDeletedConsumer> logger)
    : IIntegrationEventConsumer<UserDeletedEvent>
{
    public async Task ConsumeAsync(UserDeletedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await userProjectionService.HandleUserDeletedAsync(integrationEvent, cancellationToken);
        await playerRatingProjectionService.HandleUserDeletedAsync(integrationEvent, cancellationToken);
        logger.LogInformation("TournamentService projected UserDeleted for user {UserId}", integrationEvent.UserId);
    }
}
