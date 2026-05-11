namespace Rating.Application.Ratings.Services;

public interface IEloCalculator
{
    int CalculateDelta(int playerElo, double opponentAverageElo, double actualScore, double scoreCoefficient);
}
