using RecruitmentAts.Application.Positions.Contracts;

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

    /// <summary>A new position owned by its creator, with criteria version 1 (AC-US05 #1).</summary>
    public static Position Create(IPositionFields fields, PositionStatus status, Guid creatorId)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (status is not (PositionStatus.Draft or PositionStatus.Open))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "A new position is a draft or open.");
        }

        var position = new Position
        {
            Title = fields.Title.Trim(),
            Location = fields.Location.Trim(),
            EmploymentType = fields.EmploymentType!.Value,
            Level = fields.Level!.Value,
            Description = fields.Description,
            Status = status,
            OwnerId = creatorId,
            CreatedBy = creatorId,
        };
        position.SetFields(fields);
        position.Criteria.AddRange(NewCriteria(position.Id, 1, fields.Criteria!));
        return position;
    }

    /// <summary>The criteria of <see cref="CriteriaVersion"/>, in their order.</summary>
    public IEnumerable<PositionCriterion> CurrentCriteria() =>
        Criteria.Where(c => c.Version == CriteriaVersion).OrderBy(c => c.SortOrder);

    /// <summary>
    /// Moves along docs/11 "Position status": <c>DRAFT → OPEN</c>, <c>OPEN → CLOSED</c>, <c>CLOSED → OPEN</c>.
    /// Closing records <see cref="ClosedAt"/>; reopening clears it. Existing applications are not changed (BR-05).
    /// </summary>
    public void ChangeStatus(PositionStatus target, DateTimeOffset now)
    {
        if (!CanMoveTo(target))
        {
            throw new InvalidOperationException($"A position cannot move from {Status} to {target}.");
        }

        Status = target;
        ClosedAt = target == PositionStatus.Closed ? now : null;
    }

    public bool CanMoveTo(PositionStatus target) => (Status, target) is
        (PositionStatus.Draft, PositionStatus.Open) or
        (PositionStatus.Open, PositionStatus.Closed) or
        (PositionStatus.Closed, PositionStatus.Open);

    /// <summary>Replaces the general fields (US06); the criteria change through <see cref="ReplaceCriteria"/>.</summary>
    public void Update(IPositionFields fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        SetFields(fields);
    }

    /// <summary>
    /// Keeps the current version when the list is the same (names, descriptions, types, weights, order);
    /// otherwise appends the new list as version <c>CriteriaVersion + 1</c>. Old rows stay for the
    /// analyses and scorecards that reference them (AC-US06 #2).
    /// </summary>
    public bool ReplaceCriteria(IReadOnlyList<CriterionInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var current = CurrentCriteria().ToList();
        if (current.Count == inputs.Count && current.Zip(inputs).All(pair => Same(pair.First, pair.Second)))
        {
            return false;
        }

        CriteriaVersion++;
        Criteria.AddRange(NewCriteria(Id, CriteriaVersion, inputs));
        return true;
    }

    private static bool Same(PositionCriterion criterion, CriterionInput input) =>
        criterion.Name == input.Name.Trim()
        && criterion.Description == (string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim())
        && criterion.Type == input.Type
        && criterion.Weight == input.Weight;

    private void SetFields(IPositionFields fields)
    {
        Title = fields.Title.Trim();
        DepartmentId = fields.DepartmentId!.Value;
        Location = fields.Location.Trim();
        EmploymentType = fields.EmploymentType!.Value;
        Level = fields.Level!.Value;
        Headcount = fields.Headcount!.Value;
        TargetHireDate = fields.TargetHireDate!.Value;
        Description = fields.Description;
    }

    private static IEnumerable<PositionCriterion> NewCriteria(Guid positionId, int version, IReadOnlyList<CriterionInput> inputs) =>
        inputs.Select((input, index) => new PositionCriterion
        {
            PositionId = positionId,
            Version = version,
            Name = input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Type = input.Type!.Value,
            Weight = input.Weight!.Value,
            SortOrder = index + 1,
        });
}
