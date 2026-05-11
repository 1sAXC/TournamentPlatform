using TournamentPlatform.Shared.Common;

namespace Rating.Application.Ratings;

public static class RatingErrors
{
    public static readonly Error RatingNotFound = new("Rating.NotFound", "Rating was not found.");
}
