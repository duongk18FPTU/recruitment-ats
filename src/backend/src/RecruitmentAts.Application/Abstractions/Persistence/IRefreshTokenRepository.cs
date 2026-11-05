using RecruitmentAts.Application.Auth;

namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(RefreshToken token);

    Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Revokes every active token of the user, except the family of the current session.</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, Guid? exceptFamilyId, CancellationToken cancellationToken);
}
