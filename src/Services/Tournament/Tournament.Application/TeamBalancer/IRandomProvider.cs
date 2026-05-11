namespace Tournament.Application.TeamBalancer;

public interface IRandomProvider
{
    int Next(int maxExclusive);
}
