using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Controllers;
using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Dto;
using Tournament.Application.Tournaments.Services;
using TournamentPlatform.Contracts.Common;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Security;

namespace Tournament.Tests;

public sealed class AdminTournamentsControllerTests
{
    [Fact]
    public async Task Create_ShouldReturnOk()
    {
        var response = TournamentResponse(Guid.NewGuid(), Guid.NewGuid());
        var service = new StubTournamentService
        {
            CreateByAdminResult = Result<TournamentDetailsResponse>.Success(response)
        };
        var controller = CreateController(service);

        var result = await controller.Create(AdminRequest(response.OrganizerId), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task Create_ShouldReturnNotFound_WhenOrganizerDoesNotExist()
    {
        var service = new StubTournamentService
        {
            CreateByAdminResult = Result<TournamentDetailsResponse>.Failure(TournamentErrors.OrganizerNotFound)
        };
        var controller = CreateController(service);

        var result = await controller.Create(AdminRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Create_ShouldReturnForbid_WhenCurrentUserIsNotAdmin()
    {
        var service = new StubTournamentService
        {
            CreateByAdminResult = Result<TournamentDetailsResponse>.Failure(TournamentErrors.AdminAccessDenied)
        };
        var controller = CreateController(service, role: "Organizer");

        var result = await controller.Create(AdminRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void Controller_ShouldRequireAdminPolicy()
    {
        var authorize = Assert.Single(typeof(AdminTournamentsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(AuthorizationPolicies.RequireAdmin, authorize.Policy);
    }

    private static AdminTournamentsController CreateController(
        ITournamentService service,
        string role = "Admin")
    {
        var identity = new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, role), new Claim("account_status", "Active")],
            authenticationType: "Test");

        return new AdminTournamentsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static AdminCreateTournamentRequest AdminRequest(Guid organizerId)
    {
        return new AdminCreateTournamentRequest(
            organizerId,
            "Admin Cup",
            "Description",
            DisciplineCodes.CS2,
            "SingleElimination",
            null,
            1,
            16);
    }

    private static TournamentDetailsResponse TournamentResponse(Guid id, Guid organizerId)
    {
        return new TournamentDetailsResponse(
            id,
            "Admin Cup",
            "Description",
            DisciplineCodes.CS2,
            "SingleElimination",
            null,
            1,
            16,
            organizerId,
            "Open",
            0,
            0,
            DateTime.UtcNow,
            null,
            null,
            null,
            [],
            [],
            [],
            true,
            false);
    }

    private sealed class StubTournamentService : ITournamentService
    {
        public Result<TournamentDetailsResponse> CreateByAdminResult { get; init; } =
            Result<TournamentDetailsResponse>.Failure(TournamentErrors.OrganizerNotFound);

        public Task<Result<TournamentDetailsResponse>> CreateAsync(CreateTournamentRequest request, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<TournamentDetailsResponse>> CreateByAdminAsync(AdminCreateTournamentRequest request, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateByAdminResult);

        public Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetAllAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<TournamentDetailsResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetAvailableAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetCompletedAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetMyAsync(CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetOrganizerTournamentsAsync(CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<TournamentDetailsResponse>> RegisterPlayerAsync(Guid tournamentId, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<TournamentDetailsResponse>> LeaveAsync(Guid tournamentId, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<TournamentDetailsResponse>> UpdateAsync(Guid tournamentId, UpdateTournamentRequest request, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<TournamentDetailsResponse>> CancelAsync(Guid tournamentId, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result> DeleteAsync(Guid tournamentId, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<MatchDetailsResponse>> GetMatchDetailsAsync(Guid tournamentId, Guid matchId, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
