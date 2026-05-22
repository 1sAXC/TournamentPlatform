using Auth.Api.Controllers;
using Auth.Application.Auth;
using Auth.Application.Auth.Dto;
using Auth.Application.Auth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TournamentPlatform.Shared.Common;

namespace Auth.Tests;

public sealed class AuthControllerChangePasswordTests
{
    [Fact]
    public async Task ChangePassword_ShouldReturnNoContent_WhenPasswordChanged()
    {
        var userId = Guid.NewGuid();
        var service = new StubAuthService
        {
            ChangePasswordResult = Result.Success()
        };
        var controller = CreateController(service, userId);

        var result = await controller.ChangePassword(new ChangePasswordRequest("Password1", "NewPassword1"), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(userId, service.ChangedUserId);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnUnauthorized_WhenUserClaimIsMissing()
    {
        var controller = new AuthController(new StubAuthService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.ChangePassword(new ChangePasswordRequest("Password1", "NewPassword1"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_WhenCurrentPasswordIsInvalid()
    {
        var service = new StubAuthService
        {
            ChangePasswordResult = Result.Failure(AuthErrors.InvalidCurrentPassword)
        };
        var controller = CreateController(service, Guid.NewGuid());

        var result = await controller.ChangePassword(new ChangePasswordRequest("WrongPassword1", "NewPassword1"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static AuthController CreateController(IAuthService service, Guid userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim("sub", userId.ToString()), new Claim(ClaimTypes.Role, "Player")],
            authenticationType: "Test");

        return new AuthController(service)
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

    private sealed class StubAuthService : IAuthService
    {
        public Guid? ChangedUserId { get; private set; }

        public Result ChangePasswordResult { get; init; } = Result.Success();

        public Task<Result<AuthResponse>> RegisterPlayerAsync(RegisterPlayerRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<AuthResponse>> RegisterOrganizerAsync(RegisterOrganizerRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            ChangedUserId = userId;
            return Task.FromResult(ChangePasswordResult);
        }
    }
}
