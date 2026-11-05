namespace RecruitmentAts.Application.Users.Contracts;

/// <summary>OpenAPI <c>User</c>: an account as the Admin sees it, never the password hash.</summary>
public sealed record UserAccount(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    Role Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt)
{
    public static UserAccount From(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new(user.Id, user.Email, user.FullName, user.Phone, user.Role, user.IsActive, user.LastLoginAt, user.CreatedAt);
    }
}
