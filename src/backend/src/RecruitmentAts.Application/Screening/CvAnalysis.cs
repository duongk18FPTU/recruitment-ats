namespace RecruitmentAts.Application.Screening;

/// <summary>Table <c>cv_analyses</c>. Also the Worker's job queue (AD-02).</summary>
public sealed class CvAnalysis
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ApplicationId { get; set; }
    public Guid CvFileId { get; set; }
    public int CriteriaVersion { get; set; }
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public Guid? RequestedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? Model { get; set; }
    public string? Extracted { get; set; }
    public int? AiScore { get; set; }
    public int? FinalScore { get; set; }
    public bool? MissingMustHave { get; set; }
    public bool IsOverridden { get; set; }
    public List<CriterionResult> Results { get; set; } = [];
}
