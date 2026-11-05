using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace RecruitmentAts.Infrastructure.Security;

/// <summary>
/// Access-token settings shared by <see cref="JwtTokenIssuer"/> and the Api's JWT validation.
/// <c>Jwt__SigningKey</c> is a secret of at least 32 bytes; issuer and audience have safe defaults.
/// </summary>
public sealed class JwtSettings
{
    public const string UserIdClaim = "sub";
    public const string RoleClaim = "role";
    public const string SessionIdClaim = "sid";

    /// <summary>Present (value <c>true</c>) while the user must replace an issued password.</summary>
    public const string MustChangePasswordClaim = "pwd_change";

    public static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);

    private const int MinKeyBytes = 32;

    private JwtSettings(SymmetricSecurityKey signingKey, string issuer, string audience)
    {
        SigningKey = signingKey;
        Issuer = issuer;
        Audience = audience;
    }

    public SymmetricSecurityKey SigningKey { get; }
    public string Issuer { get; }
    public string Audience { get; }

    public static JwtSettings From(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var key = configuration["Jwt:SigningKey"];
        if (key is null || Encoding.UTF8.GetByteCount(key) < MinKeyBytes)
        {
            throw new InvalidOperationException($"Set Jwt__SigningKey to a secret of at least {MinKeyBytes} bytes.");
        }

        return new JwtSettings(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            configuration["Jwt:Issuer"] ?? "recruitment-ats",
            configuration["Jwt:Audience"] ?? "recruitment-ats");
    }

    /// <summary>Validation used by the Api: signature, issuer, audience, and lifetime (30 s clock skew).</summary>
    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = SigningKey,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        NameClaimType = UserIdClaim,
        RoleClaimType = RoleClaim,
        ClockSkew = TimeSpan.FromSeconds(30),
    };
}
