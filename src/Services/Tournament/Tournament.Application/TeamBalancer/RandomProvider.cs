namespace Tournament.Application.TeamBalancer;

public sealed class RandomProvider : IRandomProvider
{
    public int Next(int maxExclusive)
    {
        return Random.Shared.Next(maxExclusive);
    }
}
