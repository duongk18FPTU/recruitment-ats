using FluentValidation;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Lookups.Contracts;

namespace RecruitmentAts.Application.Lookups;

/// <summary>Values for forms, filters, and pickers.</summary>
public sealed class LookupService(
    IDepartmentRepository departments,
    IUserRepository users,
    IValidator<UserOptionQuery> userOptionValidator)
{
    public async Task<IReadOnlyList<DepartmentOption>> DepartmentsAsync(CancellationToken cancellationToken) =>
        [.. (await departments.ListActiveAsync(cancellationToken)).Select(d => new DepartmentOption(d.Id, d.Name))];

    public async Task<IReadOnlyList<UserOption>> UsersAsync(UserOptionQuery query, CancellationToken cancellationToken)
    {
        await userOptionValidator.ValidateOrThrowAsync(query, cancellationToken);
        return await users.ListOptionsAsync(query with { Q = query.Q?.Trim() }, cancellationToken);
    }
}
