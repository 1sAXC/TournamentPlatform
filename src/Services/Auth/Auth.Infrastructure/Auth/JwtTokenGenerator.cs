using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Application.Auth.Abstractions;
using Auth.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Auth;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        _options = new JwtOptions
        {
            Issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured."),
            Audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured."),
            Secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured."),
            ExpiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var expiresMinutes)
                ? expiresMinutes
                : 60
        };

        if (string.IsNullOrWhiteSpace(_options.Secret) || _options.Secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be at least 32 characters long.");
        }
    }

    public JwtToken Generate(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("role", user.Role.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("account_status", user.Status.ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.Nickname))
        {
            claims.Add(new Claim("nickname", user.Nickname));
        }

        if (!string.IsNullOrWhiteSpace(user.OrganizerName))
        {
            claims.Add(new Claim("organizer_name", user.OrganizerName));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
