namespace RecruitmentAts.Application.Screening;

/// <summary>Table <c>analysis_criterion_results</c>.</summary>
public sealed class CriterionResult
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AnalysisId { get; set; }
    public Guid CriterionId { get; set; }
    public required CriterionOutcome AiResult { get; set; }
    public string? AiQuote { get; set; }
    public string? AiExplanation { get; set; }
    public required CriterionOutcome FinalResult { get; set; }
    public string? OverrideReason { get; set; }
    public Guid? OverriddenBy { get; set; }
    public DateTimeOffset? OverriddenAt { get; set; }
}
