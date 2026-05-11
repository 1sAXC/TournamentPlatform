using System.Text.Json;
using Auth.Application.Auth.Abstractions;
using Auth.Infrastructure.Persistence;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Outbox;
using TournamentPlatform.Shared.Correlation;

namespace Auth.Infrastructure.Auth;

public sealed class OutboxWriter(AuthDbContext dbContext, ICorrelationContext correlationContext) : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public void Add(IntegrationEvent integrationEvent)
    {
        var eventToPersist = string.IsNullOrWhiteSpace(integrationEvent.CorrelationId)
            && !string.IsNullOrWhiteSpace(correlationContext.CorrelationId)
            ? integrationEvent with { CorrelationId = correlationContext.CorrelationId }
            : integrationEvent;

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventToPersist.EventId,
            EventType = eventToPersist.EventType,
            Payload = JsonSerializer.Serialize(eventToPersist, eventToPersist.GetType(), JsonSerializerOptions),
            OccurredAtUtc = eventToPersist.OccurredAtUtc
        });
    }
}
