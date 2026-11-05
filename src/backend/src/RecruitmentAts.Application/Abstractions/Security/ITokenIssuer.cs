using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Abstractions.Security;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>An opaque refresh token for the cookie and the hash that is stored.</summary>
public sealed record RawRefreshToken(string Value, string Hash);

public interface ITokenIssuer
{
    /// <summary>JWT for 15 minutes; <paramref name="sessionId"/> is the refresh-token family of the login.</summary>
    AccessToken IssueAccessToken(User user, Guid sessionId);

    RawRefreshToken NewRefreshToken();

    /// <summary>The stored form of a refresh token (SHA-256, lowercase hex).</summary>
    string HashRefreshToken(string value);
}
