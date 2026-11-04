namespace RecruitmentAts.Application.Applications;

/// <summary>Table <c>stage_history</c>.</summary>
public sealed class StageHistoryEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ApplicationId { get; set; }
    public Stage? FromStage { get; set; }
    public required Stage ToStage { get; set; }
    public RejectionReason? RejectionReason { get; set; }
    public string? Note { get; set; }
    public Guid? ActorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
