namespace TournamentPlatform.Contracts.Common;

// The platform supports only round-based shooter disciplines. Each match's
// score is therefore expressible as both maps-won-in-series (display) and
// total-rounds (ELO weighting). Adding a non-round discipline would require
// changing the match score model — see Match.WinnerScore/WinnerMaps.
public static class DisciplineCodes
{
    public const string CS2 = "CS2";
    public const string Valorant = "Valorant";
    public const string Standoff2 = "Standoff2";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>
    {
        CS2,
        Valorant,
        Standoff2
    };
}
