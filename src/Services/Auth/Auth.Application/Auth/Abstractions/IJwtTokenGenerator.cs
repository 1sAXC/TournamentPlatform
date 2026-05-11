using Auth.Domain.Users;

namespace Auth.Application.Auth.Abstractions;

public interface IJwtTokenGenerator
{
    JwtToken Generate(User user);
}
