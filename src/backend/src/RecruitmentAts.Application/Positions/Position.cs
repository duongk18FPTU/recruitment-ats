namespace RecruitmentAts.Application.Positions;

/// <summary>Table <c>positions</c>.</summary>
public sealed class Position
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Code { get; set; } = null!;
    public required string Title { get; set; }
    public Guid DepartmentId { get; set; }
    public required string Location { get; set; }
    public required EmploymentType EmploymentType { get; set; }
    public required PositionLevel Level { get; set; }
    public int Headcount { get; set; }
    public DateOnly TargetHireDate { get; set; }
    public required string Description { get; set; }
    public PositionStatus Status { get; set; } = PositionStatus.Draft;
    public int CriteriaVersion { get; set; } = 1;
    public Guid OwnerId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public List<PositionCriterion> Criteria { get; set; } = [];

    /// <summary>PostgreSQL <c>xmin</c>: the optimistic concurrency token (<c>rowVersion</c> in the contract).</summary>
    public uint RowVersion { get; set; }
}
