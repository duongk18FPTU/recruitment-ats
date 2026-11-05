using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AtsDbContext db) : IUserRepository
{
    // lower(email) = lower(@email) uses the expression index ux_users_email.
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Set<User>().SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

    public async Task<User?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Set<User>().FindAsync([id], cancellationToken);

    public void Add(User user) => db.Set<User>().Add(user);
}
