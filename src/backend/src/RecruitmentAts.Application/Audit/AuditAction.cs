namespace RecruitmentAts.Application.Audit;

/// <summary>Stored and serialized as UPPER_SNAKE_CASE.</summary>
public enum AuditAction
{
    Login,
    LoginFailed,
    Logout,
    Create,
    Update,
    StatusChange,
    StageChange,
    AiOverride,
    Publish,
    Unpublish,
    Export,
    Import,
    Assign,
    Dismiss,
    Cancel,
    Resend,
    Submit,
    ConsentChange,
    CvDelete,
    Anonymize,
}
