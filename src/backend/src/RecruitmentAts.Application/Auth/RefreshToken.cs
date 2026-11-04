using System.Net;

namespace RecruitmentAts.Application.Auth;

/// <summary>Table <c>refresh_tokens</c>.</summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public Guid FamilyId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedById { get; set; }
    public IPAddress? CreatedIp { get; set; }
    public string? UserAgent { get; set; }
}
