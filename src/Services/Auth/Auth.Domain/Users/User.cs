using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Auth.Domain.Users;

public sealed class User
{
    private readonly List<IntegrationEvent> _domainEvents = [];

    private User()
    {
    }

    private User(
        Guid id,
        string email,
        string normalizedEmail,
        string passwordHash,
        UserRole role,
        AccountStatus status,
        string? nickname,
        string? organizerName,
        string? contactHandle,
        Guid? createdByAdminId,
        DateTime createdAtUtc)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        Role = role;
        Status = status;
        Nickname = nickname;
        NormalizedNickname = NormalizeOptional(nickname);
        OrganizerName = organizerName;
        NormalizedOrganizerName = NormalizeOptional(organizerName);
        ContactHandle = contactHandle;
        CreatedByAdminId = createdByAdminId;
        CreatedAtUtc = createdAtUtc;

        if (status == AccountStatus.Active && role != UserRole.Admin)
        {
            AddUserCreatedEvent("Registration");
        }
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string NormalizedEmail { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public AccountStatus Status { get; private set; }
    public string? Nickname { get; private set; }
    public string? NormalizedNickname { get; private set; }
    public string? OrganizerName { get; private set; }
    public string? NormalizedOrganizerName { get; private set; }
    /// <summary>
    /// Contact handle (Telegram/Discord/etc) the user provides at registration.
    /// Required for Player and Organizer (so opponents can reach captains
    /// outside the platform); null for Admin accounts which don't need it.
    /// </summary>
    public string? ContactHandle { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }
    public Guid? CreatedByAdminId { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<IntegrationEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static User CreatePlayer(
        string email,
        string passwordHash,
        string nickname,
        string contactHandle,
        DateTime createdAtUtc)
    {
        return new User(
            Guid.NewGuid(),
            email,
            NormalizeRequired(email),
            passwordHash,
            UserRole.Player,
            AccountStatus.Active,
            nickname,
            organizerName: null,
            contactHandle: NormalizeContactHandle(contactHandle),
            createdByAdminId: null,
            createdAtUtc);
    }

    public static User CreateOrganizerSelfRegistration(
        string email,
        string passwordHash,
        string organizerName,
        string contactHandle,
        DateTime createdAtUtc)
    {
        return new User(
            Guid.NewGuid(),
            email,
            NormalizeRequired(email),
            passwordHash,
            UserRole.Organizer,
            AccountStatus.PendingApproval,
            nickname: null,
            organizerName,
            contactHandle: NormalizeContactHandle(contactHandle),
            createdByAdminId: null,
            createdAtUtc);
    }

    public static User CreateOrganizerByAdmin(
        string email,
        string passwordHash,
        string organizerName,
        string contactHandle,
        Guid createdByAdminId,
        DateTime createdAtUtc)
    {
        var user = new User(
            Guid.NewGuid(),
            email,
            NormalizeRequired(email),
            passwordHash,
            UserRole.Organizer,
            AccountStatus.Active,
            nickname: null,
            organizerName,
            contactHandle: NormalizeContactHandle(contactHandle),
            createdByAdminId,
            createdAtUtc);

        user.ApprovedAtUtc = createdAtUtc;
        return user;
    }

    public static User CreateAdmin(
        Guid id,
        string email,
        string passwordHash,
        DateTime createdAtUtc)
    {
        return new User(
            id,
            email,
            NormalizeRequired(email),
            passwordHash,
            UserRole.Admin,
            AccountStatus.Active,
            nickname: null,
            organizerName: null,
            contactHandle: null,
            createdByAdminId: null,
            createdAtUtc);
    }

    public static User CreateAdminByAdmin(
        string email,
        string passwordHash,
        Guid createdByAdminId,
        DateTime createdAtUtc)
    {
        return new User(
            Guid.NewGuid(),
            email,
            NormalizeRequired(email),
            passwordHash,
            UserRole.Admin,
            AccountStatus.Active,
            nickname: null,
            organizerName: null,
            contactHandle: null,
            createdByAdminId,
            createdAtUtc);
    }

    public void UpdateContactHandle(string contactHandle)
    {
        if (Role == UserRole.Admin)
        {
            throw new InvalidOperationException("Admin accounts do not use a contact handle.");
        }

        var normalized = NormalizeContactHandle(contactHandle);
        if (string.Equals(ContactHandle, normalized, StringComparison.Ordinal))
        {
            return;
        }

        ContactHandle = normalized;
        _domainEvents.Add(new UserContactHandleChangedEvent
        {
            UserId = Id,
            ContactHandle = normalized,
            ChangedAtUtc = DateTime.UtcNow
        });
    }

    public void Approve(DateTime approvedAtUtc)
    {
        if (Status == AccountStatus.Active)
        {
            return;
        }

        Status = AccountStatus.Active;
        ApprovedAtUtc = approvedAtUtc;
        RejectedAtUtc = null;
        AddUserCreatedEvent("Approval");
    }

    public void Reject(DateTime rejectedAtUtc)
    {
        if (Status == AccountStatus.Deleted)
        {
            throw new InvalidOperationException("Deleted account cannot be rejected.");
        }

        Status = AccountStatus.Rejected;
        RejectedAtUtc = rejectedAtUtc;
    }

    public void ResubmitOrganizerApplication(
        string organizerName,
        string contactHandle,
        string passwordHash,
        DateTime resubmittedAtUtc)
    {
        if (Role != UserRole.Organizer || Status != AccountStatus.Rejected)
        {
            throw new InvalidOperationException("Only a rejected organizer application can be resubmitted.");
        }

        Status = AccountStatus.PendingApproval;
        OrganizerName = organizerName;
        NormalizedOrganizerName = NormalizeOptional(organizerName);
        ContactHandle = NormalizeContactHandle(contactHandle);
        RejectedAtUtc = null;
        CreatedAtUtc = resubmittedAtUtc;
        SetPasswordHash(passwordHash);
    }

    public void SoftDelete(DateTime deletedAtUtc)
    {
        if (Status == AccountStatus.Deleted)
        {
            return;
        }

        Status = AccountStatus.Deleted;
        DeletedAtUtc = deletedAtUtc;
        _domainEvents.Add(new UserDeletedEvent
        {
            UserId = Id,
            Email = Email,
            DeletedAtUtc = deletedAtUtc
        });
    }

    public void ChangeRole(UserRole role, string? nickname, string? organizerName, DateTime changedAtUtc)
    {
        var previousRole = Role;
        Role = role;

        Nickname = role == UserRole.Player ? nickname : null;
        NormalizedNickname = role == UserRole.Player ? NormalizeOptional(nickname) : null;

        OrganizerName = role == UserRole.Organizer ? organizerName : null;
        NormalizedOrganizerName = role == UserRole.Organizer ? NormalizeOptional(organizerName) : null;

        if (previousRole == role)
        {
            return;
        }

        _domainEvents.Add(new UserRoleChangedEvent
        {
            UserId = Id,
            OldRole = previousRole.ToString(),
            NewRole = role.ToString(),
            Nickname = Nickname,
            OrganizerName = OrganizerName,
            ChangedAtUtc = changedAtUtc
        });
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void AddUserCreatedEvent(string creationSource)
    {
        _domainEvents.Add(new UserCreatedEvent
        {
            UserId = Id,
            Role = Role.ToString(),
            Email = Email,
            CreatedAtUtc = CreatedAtUtc,
            CreationSource = creationSource,
            PlayerNickname = Nickname,
            OrganizerName = OrganizerName,
            ContactHandle = ContactHandle
        });
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", nameof(value));
        }

        return value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeContactHandle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Contact handle cannot be empty.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 64)
        {
            throw new ArgumentException("Contact handle is too long (max 64 chars).", nameof(value));
        }

        return trimmed;
    }
}
