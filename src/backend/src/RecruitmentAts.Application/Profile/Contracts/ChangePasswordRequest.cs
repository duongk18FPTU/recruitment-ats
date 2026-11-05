namespace RecruitmentAts.Application.Profile.Contracts;

/// <summary>OpenAPI <c>ChangePasswordRequest</c>.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
