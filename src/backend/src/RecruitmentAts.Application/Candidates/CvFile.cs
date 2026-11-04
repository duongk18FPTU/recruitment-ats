namespace RecruitmentAts.Application.Candidates;

/// <summary>Table <c>cv_files</c>.</summary>
public sealed class CvFile
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CandidateId { get; set; }
    public required string StorageKey { get; set; }
    public required string OriginalName { get; set; }
    public required string ContentType { get; set; }
    public int SizeBytes { get; set; }
    public required string Sha256 { get; set; }
    public string? ExtractedText { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
