namespace RecruitmentAts.Application.Screening;

/// <summary>Stored and serialized as UPPER_SNAKE_CASE.</summary>
public enum AiCallOutcome
{
    Success,
    Timeout,
    RateLimit,
    Error,
    Invalid,
}
