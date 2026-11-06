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

/// <summary>
/// OpenAPI <c>CriterionInput</c>. <c>Id</c> names a criterion of the current version that is kept
/// (US06); enums and numbers are nullable so a missing value is a validation error.
/// </summary>
public sealed record CriterionInput(Guid? Id, string Name, string? Description, CriterionType? Type, int? Weight);

/// <summary>OpenAPI <c>PositionFields</c>, shared by create and update.</summary>
public interface IPositionFields
{
    string Title { get; }
    Guid? DepartmentId { get; }
    string Location { get; }
    EmploymentType? EmploymentType { get; }
    PositionLevel? Level { get; }
    int? Headcount { get; }
    DateOnly? TargetHireDate { get; }
    string Description { get; }
    IReadOnlyList<CriterionInput>? Criteria { get; }
}

/// <summary>OpenAPI <c>PositionCreateRequest</c>: saved as <c>DRAFT</c> (default) or <c>OPEN</c>.</summary>
public sealed record PositionCreateRequest(
    string Title,
    Guid? DepartmentId,
    string Location,
    EmploymentType? EmploymentType,
    PositionLevel? Level,
    int? Headcount,
    DateOnly? TargetHireDate,
    string Description,
    IReadOnlyList<CriterionInput>? Criteria,
    PositionStatus? Status = null) : IPositionFields;

/// <summary>OpenAPI <c>PositionStatusRequest</c>; nullable so a missing status is a validation error.</summary>
public sealed record PositionStatusRequest(PositionStatus? Status);

/// <summary>
/// OpenAPI <c>PositionUpdateRequest</c>. <c>OwnerId</c> may change only for Admin and HR Manager;
/// <c>RowVersion</c> is the token from the last read.
/// </summary>
public sealed record PositionUpdateRequest(
    string Title,
    Guid? DepartmentId,
    string Location,
    EmploymentType? EmploymentType,
    PositionLevel? Level,
    int? Headcount,
    DateOnly? TargetHireDate,
    string Description,
    IReadOnlyList<CriterionInput>? Criteria,
    string RowVersion,
    Guid? OwnerId = null) : IPositionFields;
