using Microsoft.Extensions.Logging;
using Rating.Application.Ratings.Services;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Abstractions;

namespace Rating.Infrastructure.Messaging;

public sealed class RatingUserCreatedConsumer(
    IRatingService ratingService,
    ILogger<RatingUserCreatedConsumer> logger)
    : IIntegrationEventConsumer<UserCreatedEvent>
{
    public async Task ConsumeAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("RatingService received UserCreated for user {UserId}", integrationEvent.UserId);
        await ratingService.HandleUserCreatedAsync(integrationEvent, cancellationToken);
    }
}

public sealed class RatingUserDeletedConsumer(
    IRatingService ratingService,
    ILogger<RatingUserDeletedConsumer> logger)
    : IIntegrationEventConsumer<UserDeletedEvent>
{
    public async Task ConsumeAsync(UserDeletedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("RatingService received UserDeleted for user {UserId}", integrationEvent.UserId);
        await ratingService.HandleUserDeletedAsync(integrationEvent, cancellationToken);
    }
}

public sealed class RatingMatchCompletedConsumer(
    IRatingService ratingService,
    ILogger<RatingMatchCompletedConsumer> logger)
    : IIntegrationEventConsumer<MatchCompletedEvent>
{
    public async Task ConsumeAsync(MatchCompletedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("RatingService received MatchCompleted for match {MatchId}", integrationEvent.MatchId);
        await ratingService.HandleMatchCompletedAsync(integrationEvent, cancellationToken);
    }
}

public sealed class RatingTournamentCompletedConsumer(ILogger<RatingTournamentCompletedConsumer> logger)
    : IIntegrationEventConsumer<TournamentCompletedEvent>
{
    public Task ConsumeAsync(TournamentCompletedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("RatingService received TournamentCompleted for tournament {TournamentId}", integrationEvent.TournamentId);
        return Task.CompletedTask;
    }
}
