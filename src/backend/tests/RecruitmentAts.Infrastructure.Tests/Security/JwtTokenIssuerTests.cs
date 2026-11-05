using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Infrastructure.Security;

namespace RecruitmentAts.Infrastructure.Tests.Security;

public class JwtTokenIssuerTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = "unit-test-signing-key-0123456789abcdef" })
        .Build();

    private readonly JwtTokenIssuer _issuer = new(Configuration, new NowClock());

    [Fact]
    public async Task Access_token_is_valid_for_15_minutes_with_id_role_and_session()
    {
        var user = NewUser(mustChangePassword: false);
        var session = Guid.CreateVersion7();

        var token = _issuer.IssueAccessToken(user, session);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, JwtSettings.From(Configuration).ValidationParameters());

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal(user.Id.ToString(), result.Claims[JwtSettings.UserIdClaim]);
        Assert.Equal("HR_MANAGER", result.Claims[JwtSettings.RoleClaim]);
        Assert.Equal(session.ToString(), result.Claims[JwtSettings.SessionIdClaim]);
        Assert.False(result.Claims.ContainsKey(JwtSettings.MustChangePasswordClaim));
        Assert.InRange(token.ExpiresAt - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task Issued_password_adds_the_restriction_claim()
    {
        var token = _issuer.IssueAccessToken(NewUser(mustChangePassword: true), Guid.CreateVersion7());
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, JwtSettings.From(Configuration).ValidationParameters());

        Assert.Equal("true", result.Claims[JwtSettings.MustChangePasswordClaim]);
    }

    [Fact]
    public void Refresh_tokens_are_random_and_stored_as_sha256_hex()
    {
        var first = _issuer.NewRefreshToken();
        var second = _issuer.NewRefreshToken();

        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(64, first.Hash.Length);
        Assert.Equal(first.Hash, _issuer.HashRefreshToken(first.Value));
        Assert.Matches("^[0-9a-f]{64}$", first.Hash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public void A_missing_or_short_signing_key_is_rejected(string? key)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = key })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(() => JwtSettings.From(configuration));
        Assert.Contains("Jwt__SigningKey", error.Message, StringComparison.Ordinal);
    }

    private static User NewUser(bool mustChangePassword) => new()
    {
        Email = "ha.pham@example.test",
        PasswordHash = "h",
        FullName = "Phạm Thu Hà",
        Role = Role.HrManager,
        MustChangePassword = mustChangePassword,
    };

    private sealed class NowClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
