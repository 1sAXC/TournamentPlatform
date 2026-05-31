using TournamentPlatform.Shared.Common;

namespace Tournament.Application.Tournaments;

public static class TournamentErrors
{
    public static readonly Error AccessDenied = new("Tournaments.AccessDenied", "Only active organizers can create tournaments.");
    public static readonly Error TournamentNotFound = new("Tournaments.NotFound", "Tournament was not found.");
    public static readonly Error DuplicateTitle = new("Tournaments.DuplicateTitle", "Tournament title already exists.");
    public static readonly Error InvalidTitle = new("Tournaments.InvalidTitle", "Tournament title has invalid format.");
    public static readonly Error InvalidFormat = new("Tournaments.InvalidFormat", "Tournament format is invalid.");
    public static readonly Error InvalidMaxPlayers = new("Tournaments.InvalidMaxPlayers", "MaxPlayers must be less than or equal to 120.");
    public static readonly Error InvalidTeamSize = new("Tournaments.InvalidTeamSize", "TeamSize is not allowed for this discipline.");
    public static readonly Error InvalidWinnerTeam = new("Tournaments.InvalidWinnerTeam", "WinnerTeamId must reference one of the match teams.");
    public static readonly Error InvalidMatchScore = new("Tournaments.InvalidMatchScore", "WinnerScore must be greater than LoserScore.");
    public static readonly Error MatchScoreRequired = new("Tournaments.MatchScoreRequired", "WinnerScore and LoserScore are required unless the match is a technical defeat.");
    public static readonly Error MaxPlayersNotMultipleOfTeamSize = new("Tournaments.MaxPlayersNotMultipleOfTeamSize", "MaxPlayers must be divisible by TeamSize.");
    public static readonly Error DisciplineNotFound = new("Tournaments.DisciplineNotFound", "Discipline does not exist or is inactive.");
    public static readonly Error InvalidSwissRounds = new("Tournaments.InvalidSwissRounds", "SwissRounds must be specified only for Swiss tournaments.");
    public static readonly Error PlayerAccessDenied = new("Tournaments.PlayerAccessDenied", "Only players can register to tournaments.");
    public static readonly Error MissingNickname = new("Tournaments.MissingNickname", "Player nickname claim is required.");
    public static readonly Error TournamentRegistrationClosed = new("Tournaments.RegistrationClosed", "Tournament is not open for registration.");
    public static readonly Error DuplicateRegistration = new("Tournaments.DuplicateRegistration", "Player is already registered to this tournament.");
    public static readonly Error TournamentFull = new("Tournaments.Full", "Tournament player limit has been reached.");
    public static readonly Error ParticipantNotFound = new("Tournaments.ParticipantNotFound", "Player is not registered to this tournament.");
    public static readonly Error TournamentAlreadyStarted = new("Tournaments.AlreadyStarted", "Tournament registration can be changed only before start.");
    public static readonly Error RegistrationConflict = new("Tournaments.RegistrationConflict", "Tournament registration was changed concurrently. Try again.");
    public static readonly Error CannotCancelCompleted = new("Tournaments.CannotCancelCompleted", "Completed tournament cannot be cancelled.");
    public static readonly Error AdminAccessDenied = new("Tournaments.AdminAccessDenied", "Only admins can perform this action.");
    public static readonly Error MatchAlreadyCompleted = new("Tournaments.MatchAlreadyCompleted", "Match is already completed.");
    public static readonly Error TournamentEditNotAllowed = new("Tournaments.EditNotAllowed", "Tournament can be edited only before it starts.");
    public static readonly Error OrganizerNotFound = new("Tournaments.OrganizerNotFound", "Organizer was not found.");
    public static readonly Error OrganizerRoleRequired = new("Tournaments.OrganizerRoleRequired", "User must have Organizer role.");
    public static readonly Error OrganizerInactive = new("Tournaments.OrganizerInactive", "Organizer must be active.");
    public static readonly Error CurrentRoundNotCompleted = new("Tournaments.CurrentRoundNotCompleted", "Current round has not been completed yet.");
    public static readonly Error TournamentNotInProgress = new("Tournaments.NotInProgress", "Action requires the tournament to be in progress.");
    public static readonly Error NotSwissTournament = new("Tournaments.NotSwissTournament", "Action is only available for Swiss tournaments.");
    public static readonly Error SwissRoundsExhausted = new("Tournaments.SwissRoundsExhausted", "All Swiss rounds have already been played.");
}
