using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Profile.Contracts;

/// <summary>OpenAPI <c>UserProfile</c>: the signed-in user, never the password hash.</summary>
public sealed record UserProfile(
    Guid Id,
    string Email,
    string FullName,
    string? Phone,
    string? AvatarUrl,
    string Timezone,
    Role Role,
    bool MustChangePassword)
{
    public static UserProfile From(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new(user.Id, user.Email, user.FullName, user.Phone, user.AvatarUrl, user.Timezone, user.Role, user.MustChangePassword);
    }
}
