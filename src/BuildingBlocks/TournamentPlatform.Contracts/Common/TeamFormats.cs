namespace TournamentPlatform.Contracts.Common;

public static class TeamFormats
{
    public const int OneVsOne = 1;
    public const int TwoVsTwo = 2;
    public const int FiveVsFive = 5;

    public static readonly IReadOnlySet<int> Supported = new HashSet<int>
    {
        OneVsOne,
        TwoVsTwo,
        FiveVsFive
    };
}
