using RecruitmentAts.Application.Auth.Contracts;

namespace RecruitmentAts.Application.Auth;

/// <summary>A login or refresh: the response body plus the refresh token the Api puts in the cookie.</summary>
public sealed record AuthResult(AuthResponse Response, string RefreshToken, DateTimeOffset RefreshExpiresAt);
