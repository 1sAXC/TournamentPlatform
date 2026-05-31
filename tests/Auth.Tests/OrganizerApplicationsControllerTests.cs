using Auth.Api.Controllers;
using Auth.Application.Admin;
using Auth.Application.Admin.Dto;
using Auth.Application.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Pagination;
using TournamentPlatform.Shared.Security;

namespace Auth.Tests;

public sealed class OrganizerApplicationsControllerTests
{
    [Fact]
    public async Task GetApplications_ShouldReturnOk()
    {
        var service = new StubAdminUsersService
        {
            OrganizerApplicationsResult = Result<PagedResult<OrganizerApplicationResponse>>.Success(
                new PagedResult<OrganizerApplicationResponse>([], 1, 20, 0))
        };
        var controller = new OrganizerApplicationsController(service);

        var result = await controller.GetApplications(new OrganizerApplicationsQuery(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<PagedResult<OrganizerApplicationResponse>>(ok.Value);
    }

    [Fact]
    public async Task GetApplication_ShouldReturnOk()
    {
        var application = Application();
        var service = new StubAdminUsersService
        {
            OrganizerApplicationResult = Result<OrganizerApplicationResponse>.Success(application)
        };
        var controller = new OrganizerApplicationsController(service);

        var result = await controller.GetApplication(application.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(application, ok.Value);
    }

    [Fact]
    public async Task GetApplication_ShouldReturnNotFound_WhenApplicationMissing()
    {
        var service = new StubAdminUsersService
        {
            OrganizerApplicationResult = Result<OrganizerApplicationResponse>.Failure(AdminErrors.UserNotFound)
        };
        var controller = new OrganizerApplicationsController(service);

        var result = await controller.GetApplication(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ApproveApplication_ShouldReturnOk()
    {
        var application = Application(status: "Active");
        var service = new StubAdminUsersService
        {
            ApproveOrganizerApplicationResult = Result<OrganizerApplicationResponse>.Success(application)
        };
        var controller = new OrganizerApplicationsController(service);

        var result = await controller.ApproveApplication(application.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(application, ok.Value);
    }

    [Fact]
    public async Task RejectApplication_ShouldReturnOk()
    {
        var application = Application(status: "Rejected");
        var service = new StubAdminUsersService
        {
            RejectOrganizerApplicationResult = Result<OrganizerApplicationResponse>.Success(application)
        };
        var controller = new OrganizerApplicationsController(service);

        var result = await controller.RejectApplication(application.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(application, ok.Value);
    }

    [Fact]
    public void Controller_ShouldRequireAdminPolicy()
    {
        var authorize = Assert.Single(typeof(OrganizerApplicationsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>());

        Assert.Equal(AuthorizationPolicies.RequireAdmin, authorize.Policy);
    }

    private static OrganizerApplicationResponse Application(string status = "PendingApproval")
    {
        return new OrganizerApplicationResponse(
            Guid.NewGuid(),
            "organizer@example.com",
            status,
            "Organizer Inc",
            "@organizer_inc",
            DateTime.UtcNow,
            null,
            null);
    }

    private sealed class StubAdminUsersService : IAdminUsersService
    {
        public Result<PagedResult<OrganizerApplicationResponse>> OrganizerApplicationsResult { get; init; } =
            Result<PagedResult<OrganizerApplicationResponse>>.Success(new PagedResult<OrganizerApplicationResponse>([], 1, 20, 0));

        public Result<OrganizerApplicationResponse> OrganizerApplicationResult { get; init; } =
            Result<OrganizerApplicationResponse>.Failure(AdminErrors.UserNotFound);

        public Result<OrganizerApplicationResponse> ApproveOrganizerApplicationResult { get; init; } =
            Result<OrganizerApplicationResponse>.Failure(AdminErrors.UserNotFound);

        public Result<OrganizerApplicationResponse> RejectOrganizerApplicationResult { get; init; } =
            Result<OrganizerApplicationResponse>.Failure(AdminErrors.UserNotFound);

        public Task<Result<PagedResult<AdminUserResponse>>> GetUsersAsync(AdminUsersQuery query, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<PagedResult<OrganizerApplicationResponse>>> GetOrganizerApplicationsAsync(OrganizerApplicationsQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(OrganizerApplicationsResult);

        public Task<Result<PagedResult<OrganizerApplicationResponse>>> GetOrganizerApplicationsHistoryAsync(OrganizerApplicationsQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(OrganizerApplicationsResult);

        public Task<Result<OrganizerApplicationResponse>> GetOrganizerApplicationAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(OrganizerApplicationResult);

        public Task<Result<OrganizerApplicationResponse>> ApproveOrganizerApplicationAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ApproveOrganizerApplicationResult);

        public Task<Result<OrganizerApplicationResponse>> RejectOrganizerApplicationAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(RejectOrganizerApplicationResult);

        public Task<Result<AdminUserResponse>> CreateUserAsync(CreateAdminUserRequest request, Guid adminUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result> BlockUserAsync(Guid userId, Guid currentAdminUserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<AdminUserResponse>> UnblockUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<ResetPasswordResponse>> ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
