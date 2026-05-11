namespace Tournament.Application.Tournaments;

public sealed record CurrentTournamentUser(Guid Id, string Role, string AccountStatus, string? Nickname = null);
