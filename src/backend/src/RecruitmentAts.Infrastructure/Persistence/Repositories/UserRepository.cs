using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Lookups.Contracts;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Application.Users.Contracts;

namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AtsDbContext db) : IUserRepository
{
    // lower(email) = lower(@email) uses the expression index ux_users_email.
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Set<User>().SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

    public async Task<User?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Set<User>().FindAsync([id], cancellationToken);

    public Task<bool> EmailExistsAsync(string email, Guid? exceptId, CancellationToken cancellationToken) =>
        db.Set<User>().AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != exceptId, cancellationToken);

    public async Task<PagedResult<User>> ListAsync(UserQuery query, CancellationToken cancellationToken)
    {
        var users = db.Set<User>().AsNoTracking();
        if (!string.IsNullOrEmpty(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            users = users.Where(u => EF.Functions.ILike(u.FullName, pattern, LikePattern.Escape)
                || EF.Functions.ILike(u.Email, pattern, LikePattern.Escape));
        }

        if (query.Role is { } role)
        {
            users = users.Where(u => u.Role == role);
        }

        if (query.IsActive is { } isActive)
        {
            users = users.Where(u => u.IsActive == isActive);
        }

        var total = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderBy(u => u.CreatedAt).ThenBy(u => u.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<User>(items, query.Page, query.PageSize, total);
    }

    public async Task<IReadOnlyList<UserOption>> ListOptionsAsync(UserOptionQuery query, CancellationToken cancellationToken)
    {
        var users = db.Set<User>().AsNoTracking().Where(u => u.IsActive);
        if (query.Roles is { Count: > 0 } roles)
        {
            users = users.Where(u => roles.Contains(u.Role));
        }

        if (!string.IsNullOrEmpty(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            users = users.Where(u => EF.Functions.ILike(u.FullName, pattern, LikePattern.Escape));
        }

        return await users
            .OrderBy(u => u.FullName).ThenBy(u => u.Id)
            .Select(u => new UserOption(u.Id, u.FullName, u.Role))
            .ToListAsync(cancellationToken);
    }

    public void Add(User user) => db.Set<User>().Add(user);
}
