namespace Tournament.Domain.Tournaments;

public sealed class DeletedUserProjection
{
    private DeletedUserProjection()
    {
    }

    private DeletedUserProjection(Guid userId, DateTime deletedAtUtc)
    {
        UserId = userId;
        DeletedAtUtc = deletedAtUtc;
    }

    public Guid UserId { get; private set; }
    public DateTime DeletedAtUtc { get; private set; }

    public static DeletedUserProjection Create(Guid userId, DateTime deletedAtUtc)
    {
        return new DeletedUserProjection(userId, deletedAtUtc);
    }
}
