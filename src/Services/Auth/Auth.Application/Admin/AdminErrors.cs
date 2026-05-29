using TournamentPlatform.Shared.Common;

namespace Auth.Application.Admin;

public static class AdminErrors
{
    public static readonly Error InvalidRole = new("Admin.InvalidRole", "Invalid user role.");
    public static readonly Error InvalidStatus = new("Admin.InvalidStatus", "Invalid account status.");
    public static readonly Error UserNotFound = new("Admin.UserNotFound", "User was not found.");
    public static readonly Error DuplicateEmail = new("Admin.DuplicateEmail", "User with this email already exists.");
    public static readonly Error DuplicateNickname = new("Admin.DuplicateNickname", "Player with this nickname already exists.");
    public static readonly Error OrganizerApprovalNotAllowed = new("Admin.OrganizerApprovalNotAllowed", "Only pending organizers can be approved.");
    public static readonly Error OrganizerRejectNotAllowed = new("Admin.OrganizerRejectNotAllowed", "Only pending organizers can be rejected.");
    public static readonly Error LastAdminDeleteNotAllowed = new("Admin.LastAdminDeleteNotAllowed", "The last active admin cannot be deleted.");
    public static readonly Error PlayerNicknameRequired = new("Admin.PlayerNicknameRequired", "Player nickname is required.");
    public static readonly Error OrganizerNameRequired = new("Admin.OrganizerNameRequired", "Organizer name is required.");
    public static readonly Error ContactHandleRequired = new("Admin.ContactHandleRequired", "Contact handle is required for player and organizer accounts.");
}
