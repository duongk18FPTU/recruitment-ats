using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Users;
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

    [Fact]
    public void RoleNames_are_the_contract_names_of_every_role()
    {
        string[] names = [RoleNames.Admin, RoleNames.HrManager, RoleNames.Recruiter, RoleNames.Interviewer];

        Assert.Equal(
            Enum.GetNames<Role>().Select(System.Text.Json.JsonNamingPolicy.SnakeCaseUpper.ConvertName),
            names);
    }
}
