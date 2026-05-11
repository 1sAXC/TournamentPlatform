namespace Rating.Application.Ratings.Services;

public sealed class EloCalculator : IEloCalculator
{
    private const int BaseK = 32;

    public int CalculateDelta(
        int playerElo,
        double opponentAverageElo,
        double actualScore,
        double scoreCoefficient)
    {
        var expectedScore = 1 / (1 + Math.Pow(10, (opponentAverageElo - playerElo) / 400.0));
        return (int)Math.Round(BaseK * scoreCoefficient * (actualScore - expectedScore));
    }
}
