using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Security;

/// <summary>HS256 access tokens (15 minutes) and opaque 256-bit refresh tokens stored as SHA-256 hashes.</summary>
internal sealed class JwtTokenIssuer(IConfiguration configuration, IClock clock) : ITokenIssuer
{
    private readonly Lazy<JwtSettings> _settings = new(() => JwtSettings.From(configuration));
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken IssueAccessToken(User user, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(user);

        var settings = _settings.Value;
        var now = clock.UtcNow;
        var expiresAt = now + JwtSettings.AccessTokenLifetime;
        var claims = new Dictionary<string, object>
        {
            [JwtSettings.UserIdClaim] = user.Id.ToString(),
            [JwtSettings.RoleClaim] = JsonNamingPolicy.SnakeCaseUpper.ConvertName(user.Role.ToString()),
            [JwtSettings.SessionIdClaim] = sessionId.ToString(),
        };
        if (user.MustChangePassword)
        {
            claims[JwtSettings.MustChangePasswordClaim] = "true";
        }

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256),
        });
        return new AccessToken(token, expiresAt);
    }

    public RawRefreshToken NewRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return new RawRefreshToken(value, HashRefreshToken(value));
    }

    public string HashRefreshToken(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
