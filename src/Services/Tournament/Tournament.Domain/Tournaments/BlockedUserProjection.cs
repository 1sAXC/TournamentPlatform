namespace Tournament.Domain.Tournaments;

public sealed class BlockedUserProjection
{
    private BlockedUserProjection()
    {
    }

    private BlockedUserProjection(Guid userId, DateTime blockedAtUtc)
    {
        UserId = userId;
        BlockedAtUtc = blockedAtUtc;
    }

    public Guid UserId { get; private set; }
    public DateTime BlockedAtUtc { get; private set; }

    public static BlockedUserProjection Create(Guid userId, DateTime blockedAtUtc)
    {
        return new BlockedUserProjection(userId, blockedAtUtc);
    }
}
