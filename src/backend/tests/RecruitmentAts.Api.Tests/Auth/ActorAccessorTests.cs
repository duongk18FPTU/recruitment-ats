using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Api.Tests.Auth;

public class ActorAccessorTests
{
    [Fact]
    public void Signed_in_user_comes_from_the_token_claims()
    {
        var userId = Guid.CreateVersion7();
        var httpContext = Request(new Claim(ActorAccessor.UserIdClaim, userId.ToString()), new Claim(ActorAccessor.RoleClaim, "HR_MANAGER"));
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:192.168.1.20");
        httpContext.TraceIdentifier = "trace-1";

        var actor = Accessor(httpContext).Current();

        Assert.Equal(AuditActorType.User, actor.Type);
        Assert.Equal(userId, actor.UserId);
        Assert.Equal(Role.HrManager, actor.Role);
        Assert.Equal(IPAddress.Parse("192.168.1.20"), actor.IpAddress);
        Assert.Equal("trace-1", actor.TraceId);
    }

    [Fact]
    public void Anonymous_caller_is_a_candidate()
    {
        var actor = Accessor(new DefaultHttpContext()).Current();

        Assert.Equal(AuditActorType.Candidate, actor.Type);
        Assert.Null(actor.UserId);
        Assert.Null(actor.Role);
    }

    [Theory]
    [InlineData("not-a-guid", "ADMIN")]
    [InlineData("0190a1b2-0000-7000-8000-000000000001", "HrManager")]
    public void Token_with_unusable_claims_is_UNAUTHENTICATED(string userId, string role)
    {
        var httpContext = Request(new Claim(ActorAccessor.UserIdClaim, userId), new Claim(ActorAccessor.RoleClaim, role));

        var error = Assert.Throws<AppException>(() => Accessor(httpContext).Current());

        Assert.Equal(ErrorCode.Unauthenticated, error.Code);
    }

    private static DefaultHttpContext Request(params Claim[] claims) =>
        new() { User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Bearer")) };

    private static ActorAccessor Accessor(HttpContext httpContext) =>
        new(new HttpContextAccessor { HttpContext = httpContext });
}
