namespace RecruitmentAts.Application.Auth.Contracts;

/// <summary>OpenAPI <c>LoginRequest</c>.</summary>
public sealed record LoginRequest(string Email, string Password);
