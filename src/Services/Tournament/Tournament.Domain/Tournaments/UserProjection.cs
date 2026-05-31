namespace Tournament.Domain.Tournaments;

public sealed class UserProjection
{
    private UserProjection()
    {
    }

    private UserProjection(
        Guid userId,
        string role,
        string? contactHandle,
        string? organizerName,
        DateTime createdAtUtc)
    {
        UserId = userId;
        Role = role;
        ContactHandle = contactHandle;
        OrganizerName = organizerName;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public string Role { get; private set; } = default!;
    /// <summary>
    /// Latest known contact handle (Telegram/Discord/etc.) for the user.
    /// Live-projected from Auth via UserCreatedEvent / UserContactHandleChangedEvent
    /// so the Match details endpoint can return the current value
    /// without calling Auth.Api over HTTP. Null for Admin accounts and
    /// for legacy rows created before this field was introduced.
    /// </summary>
    public string? ContactHandle { get; private set; }
    /// <summary>
    /// Latest known display name for an Organizer. Mirrored from
    /// UserCreatedEvent.OrganizerName / UserRoleChangedEvent.OrganizerName
    /// so the Match details endpoint can return the real organizer name
    /// without calling Auth.Api over HTTP. Null for Player and Admin
    /// accounts and for legacy rows created before this field was added.
    /// </summary>
    public string? OrganizerName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? BlockedAtUtc { get; private set; }

    public bool IsBlocked => BlockedAtUtc is not null;

    public static UserProjection Create(
        Guid userId,
        string role,
        string? contactHandle,
        string? organizerName,
        DateTime createdAtUtc)
    {
        return new UserProjection(userId, role, contactHandle, organizerName, createdAtUtc);
    }

    public void Restore(string role, string? organizerName)
    {
        Role = role;
        OrganizerName = organizerName;
        BlockedAtUtc = null;
    }

    public void ChangeRole(string role, string? organizerName)
    {
        Role = role;
        OrganizerName = organizerName;
    }

    public void UpdateContactHandle(string? contactHandle)
    {
        ContactHandle = contactHandle;
    }

    public void MarkBlocked(DateTime blockedAtUtc)
    {
        BlockedAtUtc = blockedAtUtc;
    }
}
