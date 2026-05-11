using Auth.Application.Auth.Abstractions;
using Auth.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Auth;

public sealed class PasswordHashingService(IPasswordHasher<User> passwordHasher) : IPasswordHashingService
{
    public string HashPassword(User user, string password)
    {
        return passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password)
    {
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
