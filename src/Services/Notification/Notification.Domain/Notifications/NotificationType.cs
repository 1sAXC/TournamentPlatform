namespace Notification.Domain.Notifications;

/// <summary>
/// Kind of notification produced by the system. Currently only MatchCreated
/// is fanned out (see RoundCreatedEvent consumer in Notification.Api). The
/// enum is kept open for future kinds (TournamentStarted, MatchResultEntered, etc.).
/// </summary>
public enum NotificationType
{
    MatchCreated = 1,
}
