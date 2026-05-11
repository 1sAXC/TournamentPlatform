namespace TournamentPlatform.Shared.Pagination;

public sealed record PageRequest(int PageNumber = 1, int PageSize = 20)
{
    public int PageNumber { get; init; } = Math.Max(1, PageNumber);
    public int PageSize { get; init; } = Math.Clamp(PageSize, 1, 100);
}
