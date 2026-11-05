using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Auth;

namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

/// <summary>Bulk revocations run as single UPDATEs inside the caller's unit-of-work transaction.</summary>
internal sealed class RefreshTokenRepository(AtsDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.Set<RefreshToken>().SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public void Add(RefreshToken token) => db.Set<RefreshToken>().Add(token);

    public Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.Set<RefreshToken>()
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.RevokedAt, now), cancellationToken);

    public Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, Guid? exceptFamilyId, CancellationToken cancellationToken) =>
        db.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.FamilyId != exceptFamilyId)
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.RevokedAt, now), cancellationToken);
}
