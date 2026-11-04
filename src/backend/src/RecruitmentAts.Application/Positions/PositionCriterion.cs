namespace RecruitmentAts.Application.Positions;

/// <summary>Table <c>position_criteria</c>.</summary>
public sealed class PositionCriterion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid PositionId { get; set; }
    public int Version { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required CriterionType Type { get; set; }
    public int Weight { get; set; }
    public int SortOrder { get; set; }
}
