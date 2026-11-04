namespace RecruitmentAts.Application.Applications;

/// <summary>Table <c>applications</c>. Named JobApplication to avoid a clash with the RecruitmentAts.Application namespace.</summary>
public sealed class JobApplication
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PositionId { get; set; }
    public Guid CandidateId { get; set; }
    public Stage Stage { get; set; } = Stage.New;
    public required Source Source { get; set; }
    public string? SourceNote { get; set; }
    public required IntakeChannel IntakeChannel { get; set; }
    public string? CoverMessage { get; set; }
    public Guid? CurrentCvFileId { get; set; }
    public Guid? CurrentAnalysisId { get; set; }
    public int? FitScore { get; set; }
    public bool MissingMustHave { get; set; }
    public RejectionReason? RejectionReason { get; set; }
    public string? RejectionNote { get; set; }
    public Guid? ImportBatchId { get; set; }
    public DateTimeOffset AppliedAt { get; set; }
    public DateTimeOffset StageChangedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
