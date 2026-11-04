namespace RecruitmentAts.Application.Common;

/// <summary>
/// The <c>ErrorCode</c> enumeration of docs/api/openapi.yaml; serialized as UPPER_SNAKE_CASE.
/// Only the Api maps a code to an HTTP status.
/// </summary>
public enum ErrorCode
{
    ValidationFailed,
    InvalidCredentials,
    Unauthenticated,
    Forbidden,
    NotFound,
    ConcurrencyConflict,
    DuplicateEmail,
    DuplicateApplication,
    PositionNotOpen,
    InvalidPositionStatusChange,
    InvalidStageTransition,
    AnalysisInProgress,
    AnalysisNotCompleted,
    ApplicationNotInInterview,
    InterviewOverlap,
    InterviewCancelled,
    ScorecardAlreadySubmitted,
    EmailNotPending,
    EmailNotFailed,
    InboxItemProcessed,
    ImportInvalidRows,
    ImportExpired,
    ExportTooLarge,
    UnsupportedFileType,
    NoExtractableText,
    FileTooLarge,
    SelfDeactivation,
    JobPostExpired,
    CaptchaFailed,
    RateLimited,
    ConsentTokenInvalid,
    AiUnavailable,
    InternalError,
}
