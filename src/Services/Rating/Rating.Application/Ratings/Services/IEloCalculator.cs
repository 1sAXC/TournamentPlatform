namespace Rating.Application.Ratings.Services;

public interface IEloCalculator
{
    double CalculateExpectedScore(double rating, double opponentRating);

    int CalculateTeamDelta(
        double teamAverageElo,
        double opponentAverageElo,
        int teamSize,
        double actualScore,
        double scoreMultiplier);
}
