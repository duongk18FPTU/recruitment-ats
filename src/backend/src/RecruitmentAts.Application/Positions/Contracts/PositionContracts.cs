using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.JobPosts;
using RecruitmentAts.Application.Lookups.Contracts;

namespace RecruitmentAts.Application.Positions.Contracts;

/// <summary>
/// Query of <c>listPositions</c>: <c>q</c> matches title or code. <c>JobPostStatus</c> matches nothing
/// until job posts exist (US22).
/// </summary>
public sealed record PositionQuery : PageQuery
{
    public const string DefaultSort = "-createdAt";

    public static readonly IReadOnlyList<string> SortValues = ["createdAt", "-createdAt", "targetHireDate", "-targetHireDate", "title"];

    public string? Q { get; init; }
    public PositionStatus? Status { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? OwnerId { get; init; }
    public JobPostStatus? JobPostStatus { get; init; }
    public string? Sort { get; init; }
}

/// <summary>OpenAPI <c>Criterion</c>.</summary>
public sealed record CriterionView(Guid Id, string Name, string? Description, CriterionType Type, int Weight, int Version);

/// <summary>OpenAPI <c>StageCount</c>.</summary>
public sealed record StageCount(Stage Stage, int Count);

/// <summary>OpenAPI <c>PositionSummary</c>. <c>jobPostStatus</c> is left out until job posts exist (US22).</summary>
public sealed record PositionSummary(
    Guid Id,
    string Code,
    string Title,
    DepartmentOption Department,
    string Location,
    PositionStatus Status,
    UserOption Owner,
    int Headcount,
    DateOnly TargetHireDate,
    int ApplicationCount,
    int HiredCount);

/// <summary>OpenAPI <c>PositionDetail</c>: the summary plus the current criteria and the concurrency token.</summary>
public sealed record PositionDetail(
    Guid Id,
    string Code,
    string Title,
    DepartmentOption Department,
    string Location,
    PositionStatus Status,
    UserOption Owner,
    int Headcount,
    DateOnly TargetHireDate,
    int ApplicationCount,
    int HiredCount,
    EmploymentType EmploymentType,
    PositionLevel Level,
    string Description,
    int CriteriaVersion,
    IReadOnlyList<CriterionView> Criteria,
    IReadOnlyList<StageCount> StageCounts,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    string RowVersion);
