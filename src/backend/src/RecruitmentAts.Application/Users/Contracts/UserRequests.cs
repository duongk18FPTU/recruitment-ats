using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Application.Users.Contracts;

/// <summary>Query of <c>listUsers</c>: <c>q</c> matches name or email.</summary>
public sealed record UserQuery : PageQuery
{
    public string? Q { get; init; }
    public Role? Role { get; init; }
    public bool? IsActive { get; init; }
}

/// <summary>OpenAPI <c>UserCreateRequest</c>. <c>Role</c> is nullable so a missing role is a validation error, not <c>ADMIN</c>.</summary>
public sealed record UserCreateRequest(string Email, string FullName, string? Phone, Role? Role, string TemporaryPassword);

/// <summary>
/// OpenAPI <c>UserUpdateRequest</c>. A <c>TemporaryPassword</c> replaces the password (the user forgot it);
/// the user must change it at the next login.
/// </summary>
public sealed record UserUpdateRequest(string Email, string FullName, string? Phone, Role? Role, string? TemporaryPassword = null);

/// <summary>Body of <c>updateUserStatus</c>; nullable so a missing value is not read as "deactivate".</summary>
public sealed record UserStatusRequest(bool? IsActive);
