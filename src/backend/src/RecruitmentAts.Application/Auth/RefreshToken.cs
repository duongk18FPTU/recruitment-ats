using System.Net;

namespace RecruitmentAts.Application.Auth;

/// <summary>Table <c>refresh_tokens</c>. Only the SHA-256 hash of the opaque token is stored.</summary>
public sealed class RefreshToken
{
    /// <summary>Lifetime of a refresh token (AD-06).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }

    /// <summary>All rotations of one login share a family; reuse of a rotated token revokes the family.</summary>
    public Guid FamilyId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedById { get; set; }
    public IPAddress? CreatedIp { get; set; }
    public string? UserAgent { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    /// <summary>Revokes this token in favour of <paramref name="next"/>, which joins the same family.</summary>
    public void RotateTo(RefreshToken next, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(next);

        next.FamilyId = FamilyId;
        RevokedAt = now;
        ReplacedById = next.Id;
    }
}
