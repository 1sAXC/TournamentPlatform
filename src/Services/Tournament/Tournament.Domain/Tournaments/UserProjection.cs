namespace Tournament.Domain.Tournaments;

public sealed class UserProjection
{
    private UserProjection()
    {
    }

    private UserProjection(Guid userId, string role, DateTime createdAtUtc)
    {
        UserId = userId;
        Role = role;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public string Role { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    public bool IsDeleted => DeletedAtUtc is not null;

    public static UserProjection Create(Guid userId, string role, DateTime createdAtUtc)
    {
        return new UserProjection(userId, role, createdAtUtc);
    }

    public void Restore(string role)
    {
        Role = role;
        DeletedAtUtc = null;
    }

    public void MarkDeleted(DateTime deletedAtUtc)
    {
        DeletedAtUtc = deletedAtUtc;
    }
}
