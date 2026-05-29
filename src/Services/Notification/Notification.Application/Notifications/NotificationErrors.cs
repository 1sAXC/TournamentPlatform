using TournamentPlatform.Shared.Common;

namespace Notification.Application.Notifications;

public static class NotificationErrors
{
    public static readonly Error NotFound = new("Notifications.NotFound", "Notification was not found.");
    public static readonly Error AccessDenied = new("Notifications.AccessDenied", "This notification belongs to another user.");
}
