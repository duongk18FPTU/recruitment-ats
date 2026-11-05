using RecruitmentAts.Api.Auth;
using RecruitmentAts.Infrastructure.Security;

namespace RecruitmentAts.Api.Tests.Auth;

/// <summary>The Api reads the claims Infrastructure writes; the hosts may not reference Infrastructure types outside Program.</summary>
public class ClaimNamesTests
{
    [Fact]
    public void ActorAccessor_reads_the_claims_JwtTokenIssuer_writes()
    {
        Assert.Equal(JwtSettings.UserIdClaim, ActorAccessor.UserIdClaim);
        Assert.Equal(JwtSettings.RoleClaim, ActorAccessor.RoleClaim);
        Assert.Equal(JwtSettings.SessionIdClaim, ActorAccessor.SessionIdClaim);
        Assert.Equal(JwtSettings.MustChangePasswordClaim, ActorAccessor.MustChangePasswordClaim);
    }
}
