using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Lookups.Contracts;

/// <summary>OpenAPI <c>Department</c>.</summary>
public sealed record DepartmentOption(Guid Id, string Name);

/// <summary>OpenAPI <c>UserOption</c>: a user in pickers and as a position owner.</summary>
public sealed record UserOption(Guid Id, string FullName, Role Role);

/// <summary>Query of <c>listUserOptions</c>: active users with one of <c>roles</c> (all roles when empty).</summary>
public sealed record UserOptionQuery
{
    public IReadOnlyList<Role>? Roles { get; init; }
    public string? Q { get; init; }
}
