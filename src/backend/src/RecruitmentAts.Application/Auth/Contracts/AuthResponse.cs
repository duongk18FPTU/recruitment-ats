using RecruitmentAts.Application.Profile.Contracts;

namespace RecruitmentAts.Application.Auth.Contracts;

/// <summary>OpenAPI <c>AuthResponse</c>. The refresh token travels only in the cookie.</summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserProfile User);
