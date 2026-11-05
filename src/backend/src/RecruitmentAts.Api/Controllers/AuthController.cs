using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Api.Errors;
using RecruitmentAts.Api.RateLimiting;
using RecruitmentAts.Application.Auth;
using RecruitmentAts.Application.Auth.Contracts;
using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, ActorAccessor actors, IProblemDetailsService problems) : ControllerBase
{
    [HttpPost("login", Name = "login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.Login)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, UserAgent(), actors.Current(), cancellationToken);
        RefreshCookie.Write(Response, result.RefreshToken, result.RefreshExpiresAt);
        return result.Response;
    }

    /// <summary>
    /// Anonymous for the bearer scheme: the refresh cookie is the credential. A refused cookie is
    /// deleted; the problem is written here because the exception handler would drop that header.
    /// </summary>
    [HttpPost("refresh", Name = "refreshToken")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        try
        {
            var result = await auth.RefreshAsync(RefreshCookie.Read(Request), UserAgent(), actors.Current(), cancellationToken);
            RefreshCookie.Write(Response, result.RefreshToken, result.RefreshExpiresAt);
            return result.Response;
        }
        catch (AppException ex) when (ex.Code == ErrorCode.Unauthenticated)
        {
            RefreshCookie.Delete(Response);
            var problem = ProblemDetailsMapper.Map(ex, HttpContext);
            Response.StatusCode = problem.Status!.Value;
            await problems.WriteAsync(new ProblemDetailsContext { HttpContext = HttpContext, ProblemDetails = problem });
            return new EmptyResult();
        }
    }

    /// <summary>Allowed before an issued password is replaced, so a user can always leave.</summary>
    [HttpPost("logout", Name = "logout")]
    [AllowPendingPasswordChange]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(RefreshCookie.Read(Request), actors.Current(), cancellationToken);
        RefreshCookie.Delete(Response);
        return NoContent();
    }

    private string? UserAgent() => Request.Headers.UserAgent.ToString() is { Length: > 0 } agent ? agent : null;
}
