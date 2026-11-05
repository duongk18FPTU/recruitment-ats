namespace RecruitmentAts.Application.Common;

/// <summary>Query parameters <c>page</c> (from 1) and <c>pageSize</c> (1–100, default 20) of every list operation.</summary>
public record PageQuery
{
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public int Skip => (Page - 1) * PageSize;
}
