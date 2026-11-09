using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Screening;

namespace RecruitmentAts.Application.Applications.Contracts;

/// <summary>
/// OpenAPI <c>ApplicationCreateForm</c>. Every field is optional here so that the validator, not the model
/// binder, reports what is missing; <c>Cv</c> is owned and disposed by the caller.
/// </summary>
public sealed record ApplicationCreateRequest(
    Guid? PositionId,
    string? FullName,
    string? Email,
    string? Phone,
    Source? Source,
    string? SourceNote,
    string? Notes,
    CvUpload? Cv);

/// <summary>OpenAPI <c>PositionRef</c> inside <c>ApplicationListItem</c>.</summary>
public sealed record ApplicationPositionRef(Guid Id, string Code, string Title);

/// <summary>OpenAPI <c>CandidateSummary</c>.</summary>
public sealed record CandidateSummary(
    Guid Id, string FullName, string? Email, string? Phone, bool EmailInvalid, bool InTalentPool);

/// <summary>OpenAPI <c>CvFile</c>.</summary>
public sealed record CvFileView(
    Guid Id, string OriginalName, string ContentType, int SizeBytes, DateTimeOffset UploadedAt, bool IsCurrent);

/// <summary>
/// OpenAPI <c>ApplicationDetail</c> (<c>ApplicationListItem</c> plus the detail members).
/// <c>AllowedStages</c> is filled by the service for the caller's role; the AI members stay null for an
/// Interviewer and until an analysis exists.
/// </summary>
public sealed record ApplicationDetail(
    Guid Id,
    CandidateSummary Candidate,
    ApplicationPositionRef Position,
    Stage Stage,
    Source Source,
    DateTimeOffset AppliedAt,
    DateTimeOffset StageChangedAt,
    int? FitScore,
    bool? MissingMustHave,
    AnalysisStatus? AnalysisStatus,
    DateTimeOffset? NextInterviewAt,
    string? SourceNote,
    IntakeChannel IntakeChannel,
    string? CoverMessage,
    string? Notes,
    RejectionReason? RejectionReason,
    string? RejectionNote,
    Guid? CurrentCvFileId,
    IReadOnlyList<CvFileView> CvFiles,
    IReadOnlyList<Stage> AllowedStages,
    int? PendingEmailCount,
    string RowVersion);

/// <summary>OpenAPI <c>ApplicationCreated</c>.</summary>
public sealed record ApplicationCreated(ApplicationDetail Application, bool CandidateMatched, bool ReceiptEmailQueued);
