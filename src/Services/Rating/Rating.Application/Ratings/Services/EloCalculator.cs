namespace Rating.Application.Ratings.Services;

public sealed class EloCalculator : IEloCalculator
{
    public double CalculateExpectedScore(double rating, double opponentRating)
    {
        return 1 / (1 + Math.Pow(10, (opponentRating - rating) / 400.0));
    }

    public int CalculateTeamDelta(
        double teamAverageElo,
        double opponentAverageElo,
        int teamSize,
        double actualScore,
        double scoreMultiplier)
    {
        var expectedScore = CalculateExpectedScore(teamAverageElo, opponentAverageElo);
        var kFactor = GetKFactor(teamSize);

        return (int)Math.Round(kFactor * scoreMultiplier * (actualScore - expectedScore));
    }

    private static int GetKFactor(int teamSize)
    {
        return teamSize switch
        {
            1 => 32,
            2 => 28,
            5 => 24,
            _ => 24
        };
    }
}
