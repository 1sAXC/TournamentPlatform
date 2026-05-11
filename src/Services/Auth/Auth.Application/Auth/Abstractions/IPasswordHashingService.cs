using Auth.Domain.Users;

namespace Auth.Application.Auth.Abstractions;

public interface IPasswordHashingService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string password);
}
