namespace Notification.Domain.Notifications;

/// <summary>
/// A delivered notification record for a specific recipient. Created by
/// the fan-out logic in Notification.Api when integration events arrive
/// from RabbitMQ. Idempotency is enforced by a unique index on
/// (SourceEventId, RecipientUserId).
/// </summary>
public sealed class Notification
{
    private Notification()
    {
    }

    private Notification(
        Guid id,
        Guid recipientUserId,
        NotificationType type,
        string title,
        string body,
        string linkUrl,
        string payloadJson,
        Guid sourceEventId,
        DateTime createdAtUtc)
    {
        Id = id;
        RecipientUserId = recipientUserId;
        Type = type;
        Title = title;
        Body = body;
        LinkUrl = linkUrl;
        PayloadJson = payloadJson;
        SourceEventId = sourceEventId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public string LinkUrl { get; private set; } = default!;
    public string PayloadJson { get; private set; } = default!;
    public Guid SourceEventId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    public bool IsRead => ReadAtUtc.HasValue;

    public void MarkRead(DateTime atUtc)
    {
        if (ReadAtUtc.HasValue)
        {
            return;
        }

        ReadAtUtc = atUtc;
    }

    public static Notification Create(
        Guid recipientUserId,
        NotificationType type,
        string title,
        string body,
        string linkUrl,
        string payloadJson,
        Guid sourceEventId,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ArgumentException("Body is required.", nameof(body));
        }

        if (string.IsNullOrWhiteSpace(linkUrl))
        {
            throw new ArgumentException("LinkUrl is required.", nameof(linkUrl));
        }

        return new Notification(
            Guid.NewGuid(),
            recipientUserId,
            type,
            title.Trim(),
            body.Trim(),
            linkUrl.Trim(),
            payloadJson ?? string.Empty,
            sourceEventId,
            createdAtUtc);
    }
}
