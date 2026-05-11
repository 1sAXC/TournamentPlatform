using System.Text.RegularExpressions;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Dto;
using Tournament.Application.Tournaments.Exceptions;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Shared.Common;

namespace Tournament.Application.Tournaments.Services;

public sealed class TournamentService(
    ITournamentRepository tournaments,
    IOutboxWriter outboxWriter,
    ITournamentLifecycleService lifecycleService) : ITournamentService
{
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

        if (!Enum.TryParse<TournamentFormat>(request.Format, ignoreCase: true, out var format))
        {
            return Result<TournamentDetailsResponse>.Failure(TournamentErrors.InvalidFormat);
        }

        var swissRoundsValidation = ValidateSwissRounds(format, request.SwissRounds);
        if (swissRoundsValidation.IsFailure)
        {
            return Result<TournamentDetailsResponse>.Failure(swissRoundsValidation.Error);
        }

        if (request.MaxPlayers > 1000)
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

        outboxWriter.Add(new TournamentCancelledEvent
        {
            TournamentId = tournament.Id,
            TournamentName = tournament.Title,
            DisciplineCode = tournament.DisciplineCode,
            CancelledAtUtc = now
        });

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

            if (await tournaments.DeletedUserExistsAsync(currentUser.Id, cancellationToken))
            {
                return Result<TournamentDetailsResponse>.Failure(TournamentErrors.PlayerAccessDenied);
            }

            var now = DateTime.UtcNow;
            tournament.RegisterParticipant(currentUser.Id, currentUser.Nickname!, now);

            outboxWriter.Add(new PlayerRegisteredToTournamentEvent
            {
                TournamentId = tournament.Id,
                PlayerId = currentUser.Id,
                PlayerNickname = currentUser.Nickname!,
                DisciplineCode = tournament.DisciplineCode,
                RegisteredAtUtc = now
            });

            if (tournament.ActiveParticipantsCount == tournament.MaxPlayers)
            {
                await lifecycleService.TryStartTournamentAsync(tournament.Id, cancellationToken);
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

            outboxWriter.Add(new PlayerLeftTournamentEvent
            {
                TournamentId = tournament.Id,
                PlayerId = currentUser.Id,
                DisciplineCode = tournament.DisciplineCode,
                LeftAtUtc = now
            });

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
        IReadOnlyCollection<Domain.Tournaments.Tournament> source)
    {
        return source.Select(ToListItemResponse).ToArray();
    }

    private static TournamentListItemResponse ToListItemResponse(Domain.Tournaments.Tournament tournament)
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
            tournament.ActiveParticipantsCount,
            tournament.ActiveParticipantsCount,
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
