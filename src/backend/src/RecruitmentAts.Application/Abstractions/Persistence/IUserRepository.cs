using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface IUserRepository
{
    /// <summary>Case-insensitive, like the unique index on <c>lower(email)</c>.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);
}
