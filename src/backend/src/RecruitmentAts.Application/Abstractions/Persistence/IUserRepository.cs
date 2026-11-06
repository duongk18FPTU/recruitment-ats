using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Lookups.Contracts;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Application.Users.Contracts;

namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface IUserRepository
{
    /// <summary>Case-insensitive, like the unique index on <c>lower(email)</c>.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Case-insensitive; <paramref name="exceptId"/> is the user being edited.</summary>
    Task<bool> EmailExistsAsync(string email, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>Oldest account first; <c>q</c> matches name or email, ignoring case.</summary>
    Task<PagedResult<User>> ListAsync(UserQuery query, CancellationToken cancellationToken);

    /// <summary>Active users for pickers, by name; <c>q</c> matches the name, ignoring case.</summary>
    Task<IReadOnlyList<UserOption>> ListOptionsAsync(UserOptionQuery query, CancellationToken cancellationToken);

    void Add(User user);
}
