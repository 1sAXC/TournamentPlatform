namespace Tournament.Domain.Tournaments;

public sealed class UserProjection
{
    private UserProjection()
    {
    }

    private UserProjection(Guid userId, string role, string? contactHandle, DateTime createdAtUtc)
    {
        UserId = userId;
        Role = role;
        ContactHandle = contactHandle;
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
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public bool IsDeleted => DeletedAtUtc is not null;

    public static UserProjection Create(Guid userId, string role, string? contactHandle, DateTime createdAtUtc)
    {
        return new UserProjection(userId, role, contactHandle, createdAtUtc);
    }

    public void Restore(string role)
    {
        Role = role;
        DeletedAtUtc = null;
    }

    public void ChangeRole(string role)
    {
        Role = role;
    }

    public void UpdateContactHandle(string? contactHandle)
    {
        ContactHandle = contactHandle;
    }

    public void MarkDeleted(DateTime deletedAtUtc)
    {
        DeletedAtUtc = deletedAtUtc;
    }
}
