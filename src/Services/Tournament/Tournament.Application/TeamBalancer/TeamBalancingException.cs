namespace Tournament.Application.TeamBalancer;

public sealed class TeamBalancingException : Exception
{
    public TeamBalancingException(string message)
        : base(message)
    {
    }
}
