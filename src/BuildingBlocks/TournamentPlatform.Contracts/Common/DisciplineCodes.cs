namespace TournamentPlatform.Contracts.Common;

public static class DisciplineCodes
{
    public const string CS2 = "CS2";
    public const string PUBG = "PUBG";
    public const string Valorant = "Valorant";
    public const string Standoff2 = "Standoff2";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>
    {
        CS2,
        PUBG,
        Valorant,
        Standoff2
    };
}
