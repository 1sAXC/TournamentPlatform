using System.Text.RegularExpressions;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Dto;
using Tournament.Application.Tournaments.Exceptions;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Shared.Common;

namespace Tournament.Application.Tournaments.Services;

public sealed class TournamentService(
    ITournamentRepository tournaments,
    IUserProjectionRepository users,
    ITournamentLifecycleService lifecycleService) : ITournamentService
{
    private const int MaxTournamentPlayers = 120;

    private static readonly Regex TitleRegex = new(
        @"^(?!.* {2,})(?!.*-{2,})[A-Za-z0-9][A-Za-z0-9 -]*[A-Za-z0-9]$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public async Task<Result<TournamentDetailsResponse>> CreateAsync(
        CreateTournamentRequest request,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!IsActiveOrganizer(currentUser))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.AccessDenied);
        }

        var title = request.Title.Trim();
        if (!TitleRegex.IsMatch(title))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.InvalidTitle);
        }

        if (!TryParseTournamentFormat(request.Format, out var format))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.InvalidFormat);
        }

        var swissRoundsValidation = ValidateSwissRounds(format, request.SwissRounds);
        if (swissRoundsValidation.IsFailure)
        {
            return Result<TournamentDetailsResponse>.Failure(swissRoundsValidation.Error);
        }

        if (request.MaxPlayers > MaxTournamentPlayers)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.InvalidMaxPlayers);
        }

        if (request.MaxPlayers % request.TeamSize != 0)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.MaxPlayersNotMultipleOfTeamSize);
        }

        var disciplineCode = request.DisciplineCode.Trim();
        var discipline = await tournaments.GetActiveDisciplineAsync(disciplineCode, cancellationToken);
        if (discipline is null)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.DisciplineNotFound);
        }

        if (!discipline.AllowsTeamSize(request.TeamSize))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.InvalidTeamSize);
        }

        var normalizedTitle = NormalizeTitle(title);
        if (await tournaments.TitleExistsAsync(normalizedTitle, cancellationToken))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.DuplicateTitle);
        }

        var tournament = Domain.Tournaments.Tournament.Create(
            title,
            normalizedTitle,
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            discipline.Code,
            format,
            request.SwissRounds,
            request.TeamSize,
            request.MaxPlayers,
            currentUser.Id,
            DateTime.UtcNow);

        tournaments.Add(tournament);
        await tournaments.SaveChangesAsync(cancellationToken);

        return Result<TournamentDetailsResponse>.Success(ToDetailsResponse(tournament));
    }

    public async Task<Result<TournamentDetailsResponse>> CreateByAdminAsync(
        AdminCreateTournamentRequest request,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdmin(currentUser))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.AdminAccessDenied);
        }

        var organizer = await users.GetByIdAsync(request.OrganizerId, cancellationToken);
        if (organizer is null)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.OrganizerNotFound);
        }

        if (!string.Equals(organizer.Role, UserRole.Organizer.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.OrganizerRoleRequired);
        }

        if (organizer.IsBlocked)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.OrganizerInactive);
        }

        return await CreateAsync(
            request.ToCreateTournamentRequest(),
            new CurrentTournamentUser(organizer.UserId, UserRole.Organizer.ToString(), AccountStatus.Active.ToString()),
            cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await tournaments.GetAllAsync(cancellationToken);
        return Result<IReadOnlyCollection<TournamentListItemResponse>>.Success(ToListResponse(result));
    }

    public async Task<Result<TournamentDetailsResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tournament = await tournaments.GetByIdAsync(id, cancellationToken);
        return tournament is null
            ? Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentNotFound)
            : Result<TournamentDetailsResponse>.Success(ToDetailsResponse(tournament));
    }

    public async Task<Result<MatchDetailsResponse>> GetMatchDetailsAsync(
        Guid tournamentId,
        Guid matchId,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<MatchDetailsResponse>.Failure(TournamentErrors.TournamentNotFound);
        }

        var round = tournament.Rounds.FirstOrDefault(r => r.Matches.Any(m => m.Id == matchId));
        var match = round?.Matches.FirstOrDefault(m => m.Id == matchId);
        if (round is null || match is null)
        {
            return Result<MatchDetailsResponse>.Failure(TournamentErrors.TournamentNotFound);
        }

        var teamA = match.TeamAId is null ? null : tournament.Teams.FirstOrDefault(t => t.Id == match.TeamAId);
        var teamB = match.TeamBId is null ? null : tournament.Teams.FirstOrDefault(t => t.Id == match.TeamBId);

        // Contact handles are sensitive: visible only to participants of either
        // team in the match, the organizer who owns the tournament, and admins.
        // Everyone else sees the page with names/ELO but the handles are null.
        var viewerIsAdmin = IsAdmin(currentUser);
        var viewerIsOrganizerOwner = IsOrganizer(currentUser) && tournament.OrganizerId == currentUser.Id;
        var viewerIsTeamMember = (teamA?.Members.Any(m => m.PlayerId == currentUser.Id) ?? false)
            || (teamB?.Members.Any(m => m.PlayerId == currentUser.Id) ?? false);
        var canSeeContacts = viewerIsAdmin || viewerIsOrganizerOwner || viewerIsTeamMember;

        // Single batch lookup against the local UserProjection — no HTTP to
        // Auth.Api: contact handles are projected via integration events.
        var contactUserIds = new List<Guid> { tournament.OrganizerId };
        if (teamA is not null)
        {
            contactUserIds.AddRange(teamA.Members.Select(m => m.PlayerId));
        }
        if (teamB is not null)
        {
            contactUserIds.AddRange(teamB.Members.Select(m => m.PlayerId));
        }

        var projections = (await users.GetByIdsAsync(contactUserIds.Distinct().ToArray(), cancellationToken))
            .ToDictionary(projection => projection.UserId);

        string? ResolveContact(Guid userId)
        {
            return projections.TryGetValue(userId, out var projection) ? projection.ContactHandle : null;
        }

        string? ResolveOrganizerName(Guid userId)
        {
            return projections.TryGetValue(userId, out var projection) ? projection.OrganizerName : null;
        }

        MatchTeamResponse? MapTeam(Domain.Tournaments.Team? team)
        {
            if (team is null)
            {
                return null;
            }

            return new MatchTeamResponse(
                team.Id,
                team.Name,
                team.CaptainPlayerId,
                team.Seed,
                team.AverageElo,
                team.Members
                    .Select(member => new MatchTeamMemberResponse(
                        member.PlayerId,
                        member.Nickname,
                        member.Elo,
                        member.PlayerId == team.CaptainPlayerId,
                        canSeeContacts ? ResolveContact(member.PlayerId) : null))
                    .ToArray());
        }

        var organizer = new MatchOrganizerResponse(
            tournament.OrganizerId,
            OrganizerName: ResolveOrganizerName(tournament.OrganizerId),
            ContactHandle: ResolveContact(tournament.OrganizerId));

        return Result<MatchDetailsResponse>.Success(new MatchDetailsResponse(
            tournament.Id,
            tournament.Title,
            tournament.Description,
            tournament.DisciplineCode,
            tournament.Format.ToString(),
            tournament.TeamSize,
            tournament.Status.ToString(),
            match.Id,
            match.MatchNumber,
            round.Number,
            match.Status.ToString(),
            match.WinnerScore,
            match.LoserScore,
            match.WinnerMaps,
            match.LoserMaps,
            match.WinnerTeamId,
            match.CreatedAtUtc,
            match.CompletedAtUtc,
            organizer,
            MapTeam(teamA),
            MapTeam(teamB),
            canSeeContacts));
    }

    public async Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await tournaments.GetAvailableAsync(cancellationToken);
        return Result<IReadOnlyCollection<TournamentListItemResponse>>.Success(ToListResponse(result));
    }

    public async Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await tournaments.GetByStatusAsync(TournamentStatus.InProgress, cancellationToken);
        return Result<IReadOnlyCollection<TournamentListItemResponse>>.Success(ToListResponse(result));
    }

    public async Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetCompletedAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await tournaments.GetByStatusAsync(TournamentStatus.Completed, cancellationToken);
        return Result<IReadOnlyCollection<TournamentListItemResponse>>.Success(ToListResponse(result));
    }

    public async Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetMyAsync(
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!IsPlayer(currentUser))
        {
            return Result<IReadOnlyCollection<TournamentListItemResponse>>.Failure(TournamentErrors.PlayerAccessDenied);
        }

        var result = await tournaments.GetByPlayerAsync(currentUser.Id, cancellationToken);
        return Result<IReadOnlyCollection<TournamentListItemResponse>>.Success(ToListResponse(result));
    }

    public async Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetOrganizerTournamentsAsync(
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!IsOrganizer(currentUser))
        {
            return Result<IReadOnlyCollection<TournamentListItemResponse>>.Failure(TournamentErrors.AccessDenied);
        }

        var result = await tournaments.GetByOrganizerAsync(currentUser.Id, cancellationToken);
        return Result<IReadOnlyCollection<TournamentListItemResponse>>.Success(ToListResponse(result));
    }

    public async Task<Result<TournamentDetailsResponse>> RegisterPlayerAsync(
        Guid tournamentId,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!IsPlayer(currentUser))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.PlayerAccessDenied);
        }

        if (string.IsNullOrWhiteSpace(currentUser.Nickname))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.MissingNickname);
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var result = await RegisterPlayerCoreAsync(tournamentId, currentUser, cancellationToken);
            if (result.Error != TournamentErrors.RegistrationConflict || attempt == 1)
            {
                return result;
            }
        }

        return Result<TournamentDetailsResponse>.Failure(TournamentErrors.RegistrationConflict);
    }

    public async Task<Result<TournamentDetailsResponse>> LeaveAsync(
        Guid tournamentId,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!IsPlayer(currentUser))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.PlayerAccessDenied);
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var result = await LeaveCoreAsync(tournamentId, currentUser, cancellationToken);
            if (result.Error != TournamentErrors.RegistrationConflict || attempt == 1)
            {
                return result;
            }
        }

        return Result<TournamentDetailsResponse>.Failure(TournamentErrors.RegistrationConflict);
    }

    public async Task<Result<TournamentDetailsResponse>> UpdateAsync(
        Guid tournamentId,
        UpdateTournamentRequest request,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentNotFound);
        }

        if (!CanManageTournament(tournament, currentUser))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.AccessDenied);
        }

        if (tournament.Status is not (TournamentStatus.Open or TournamentStatus.Full))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentEditNotAllowed);
        }

        var title = request.Title.Trim();
        if (!TitleRegex.IsMatch(title))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.InvalidTitle);
        }

        var normalizedTitle = NormalizeTitle(title);
        if (normalizedTitle != tournament.NormalizedTitle
            && await tournaments.TitleExistsAsync(normalizedTitle, cancellationToken))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.DuplicateTitle);
        }

        tournament.UpdateDetails(
            title,
            normalizedTitle,
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim());

        await tournaments.SaveChangesAsync(cancellationToken);
        return Result<TournamentDetailsResponse>.Success(ToDetailsResponse(tournament));
    }

    public async Task<Result<TournamentDetailsResponse>> CancelAsync(
        Guid tournamentId,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentNotFound);
        }

        if (!CanManageTournament(tournament, currentUser))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.AccessDenied);
        }

        if (tournament.Status == TournamentStatus.Completed)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.CannotCancelCompleted);
        }

        if (tournament.Status == TournamentStatus.Cancelled)
        {
            return Result<TournamentDetailsResponse>.Success(ToDetailsResponse(tournament));
        }

        var now = DateTime.UtcNow;
        tournament.Cancel(now);
        foreach (var match in tournament.Rounds.SelectMany(round => round.Matches))
        {
            match.Cancel();
        }

        await tournaments.SaveChangesAsync(cancellationToken);
        return Result<TournamentDetailsResponse>.Success(ToDetailsResponse(tournament));
    }

    public async Task<Result> DeleteAsync(
        Guid tournamentId,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdmin(currentUser))
        {
            return Result.Failure(TournamentErrors.AdminAccessDenied);
        }

        var tournament = await tournaments.GetByIdAsync(tournamentId, cancellationToken);
        if (tournament is null)
        {
            return Result.Failure(TournamentErrors.TournamentNotFound);
        }

        if (!tournament.IsDeleted)
        {
            tournament.SoftDelete(DateTime.UtcNow);
            await tournaments.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    public static string NormalizeTitle(string title)
    {
        return title.Trim().ToUpperInvariant();
    }

    // Enum.TryParse(ignoreCase: true) accepts numeric strings ("0", "1", …)
    // as valid enum values, which would silently let a request like
    // { "format": "1" } through. Restrict to the named members only.
    private static bool TryParseTournamentFormat(string? value, out TournamentFormat format)
    {
        format = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0 || char.IsDigit(trimmed[0]))
        {
            return false;
        }

        return Enum.TryParse(trimmed, ignoreCase: true, out format)
            && Enum.IsDefined(format);
    }

    private static Result ValidateSwissRounds(TournamentFormat format, int? swissRounds)
    {
        if (format == TournamentFormat.Swiss)
        {
            return swissRounds is > 0
                ? Result.Success()
                : Result.Failure(TournamentErrors.InvalidSwissRounds);
        }

        return swissRounds is null
            ? Result.Success()
            : Result.Failure(TournamentErrors.InvalidSwissRounds);
    }

    private static bool IsActiveOrganizer(CurrentTournamentUser currentUser)
    {
        return string.Equals(currentUser.Role, UserRole.Organizer.ToString(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(currentUser.AccountStatus, AccountStatus.Active.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOrganizer(CurrentTournamentUser currentUser)
    {
        return string.Equals(currentUser.Role, UserRole.Organizer.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPlayer(CurrentTournamentUser currentUser)
    {
        return string.Equals(currentUser.Role, UserRole.Player.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAdmin(CurrentTournamentUser currentUser)
    {
        return string.Equals(currentUser.Role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanManageTournament(Domain.Tournaments.Tournament tournament, CurrentTournamentUser currentUser)
    {
        return IsAdmin(currentUser)
            || (IsOrganizer(currentUser) && tournament.OrganizerId == currentUser.Id);
    }

    private async Task<Result<TournamentDetailsResponse>> RegisterPlayerCoreAsync(
        Guid tournamentId,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await tournaments.BeginTransactionAsync(cancellationToken);

            var tournament = await tournaments.GetByIdAsync(tournamentId, cancellationToken);
            if (tournament is null)
            {
                return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentNotFound);
            }

            if (tournament.Status != TournamentStatus.Open)
            {
                return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentRegistrationClosed);
            }

            if (tournament.HasActiveParticipant(currentUser.Id))
            {
                return Result<TournamentDetailsResponse>.Failure(TournamentErrors.DuplicateRegistration);
            }

            if (!tournament.HasFreeSlots())
            {
                return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentFull);
            }

            if (await tournaments.BlockedUserExistsAsync(currentUser.Id, cancellationToken))
            {
                return Result<TournamentDetailsResponse>.Failure(TournamentErrors.PlayerAccessDenied);
            }

            var now = DateTime.UtcNow;
            tournament.RegisterParticipant(currentUser.Id, currentUser.Nickname!, now);

            if (tournament.ActiveParticipantsCount == tournament.MaxPlayers)
            {
                await lifecycleService.TryStartTournamentAsync(tournament, cancellationToken);
            }

            await tournaments.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<TournamentDetailsResponse>.Success(ToDetailsResponse(tournament));
        }
        catch (TournamentPersistenceConflictException)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.RegistrationConflict);
        }
    }

    private async Task<Result<TournamentDetailsResponse>> LeaveCoreAsync(
        Guid tournamentId,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await tournaments.BeginTransactionAsync(cancellationToken);

            var tournament = await tournaments.GetByIdAsync(tournamentId, cancellationToken);
            if (tournament is null)
            {
                return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentNotFound);
            }

            if (tournament.Status is not (TournamentStatus.Open or TournamentStatus.Full))
            {
                return Result<TournamentDetailsResponse>.Failure(TournamentErrors.TournamentAlreadyStarted);
            }

            var participant = tournament.GetActiveParticipant(currentUser.Id);
            if (participant is null)
            {
                return Result<TournamentDetailsResponse>.Failure(TournamentErrors.ParticipantNotFound);
            }

            var now = DateTime.UtcNow;
            participant.Leave(now);
            tournament.TouchConcurrencyToken();

            if (tournament.Status == TournamentStatus.Full && tournament.ActiveParticipantsCount < tournament.MaxPlayers)
            {
                tournament.Reopen();
            }

            await tournaments.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<TournamentDetailsResponse>.Success(ToDetailsResponse(tournament));
        }
        catch (TournamentPersistenceConflictException)
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.RegistrationConflict);
        }
    }

    private static IReadOnlyCollection<TournamentListItemResponse> ToListResponse(
        IReadOnlyCollection<TournamentSummaryDto> source)
    {
        return source.Select(ToListItemResponse).ToArray();
    }

    private static TournamentListItemResponse ToListItemResponse(TournamentSummaryDto tournament)
    {
        return new TournamentListItemResponse(
            tournament.Id,
            tournament.Title,
            tournament.Description,
            tournament.DisciplineCode,
            tournament.Format.ToString(),
            tournament.SwissRounds,
            tournament.TeamSize,
            tournament.MaxPlayers,
            tournament.OrganizerId,
            tournament.Status.ToString(),
            tournament.CurrentRoundNumber,
            tournament.CurrentPlayersCount,
            tournament.CreatedAtUtc,
            tournament.StartedAtUtc,
            tournament.CompletedAtUtc,
            tournament.CancelledAtUtc);
    }

    private static TournamentDetailsResponse ToDetailsResponse(Domain.Tournaments.Tournament tournament)
    {
        return TournamentResponseMapper.ToDetailsResponse(tournament);
    }
}
