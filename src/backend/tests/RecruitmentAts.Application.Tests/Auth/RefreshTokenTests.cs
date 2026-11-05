using RecruitmentAts.Application.Auth;

namespace RecruitmentAts.Application.Tests.Auth;

public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Rotation_revokes_the_old_token_and_keeps_the_family()
    {
        var family = Guid.CreateVersion7();
        var current = new RefreshToken { TokenHash = "a", FamilyId = family, ExpiresAt = Now.AddDays(7) };
        var next = new RefreshToken { TokenHash = "b", ExpiresAt = Now.AddDays(7) };

        current.RotateTo(next, Now);

        Assert.Equal(family, next.FamilyId);
        Assert.Equal(Now, current.RevokedAt);
        Assert.Equal(next.Id, current.ReplacedById);
        Assert.False(current.IsActive(Now));
        Assert.True(next.IsActive(Now));
    }

    [Fact]
    public void An_expired_token_is_not_active() =>
        Assert.False(new RefreshToken { TokenHash = "a", ExpiresAt = Now }.IsActive(Now));
}
