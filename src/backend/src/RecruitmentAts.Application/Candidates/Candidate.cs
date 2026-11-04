namespace RecruitmentAts.Application.Candidates;

/// <summary>Table <c>candidates</c>.</summary>
public sealed class Candidate
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? EmailNormalized { get; set; }
    public bool EmailInvalid { get; set; }
    public string? Phone { get; set; }
    public string? PhoneNormalized { get; set; }
    public string? Notes { get; set; }
    public List<string> Skills { get; set; } = [];
    public DateTimeOffset? PoolConsentAt { get; set; }
    public DateTimeOffset? PoolConsentExpiresAt { get; set; }
    public DateTimeOffset? PoolRenewalSentAt { get; set; }
    public int ConsentTokenVersion { get; set; } = 1;
    public DateTimeOffset LastActivityAt { get; set; }
    public bool IsAnonymized { get; set; }
    public DateTimeOffset? AnonymizedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
