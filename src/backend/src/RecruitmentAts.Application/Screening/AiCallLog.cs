namespace RecruitmentAts.Application.Screening;

/// <summary>Table <c>ai_call_logs</c>. One row per call to the external AI service (BR-26).</summary>
public sealed class AiCallLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public AiCallPurpose Purpose { get; set; } = AiCallPurpose.Analysis;
    public Guid? AnalysisId { get; set; }
    public Guid? ActorId { get; set; }
    public int Attempt { get; set; } = 1;
    public required string Model { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int DurationMs { get; set; }
    public required AiCallOutcome Outcome { get; set; }
    public int? HttpStatus { get; set; }
    public int InputChars { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public string? ErrorMessage { get; set; }
}
