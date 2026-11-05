using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Api.Auth;

/// <summary>
/// Builds the <see cref="Actor"/> of the current request: a signed-in user from the access-token
/// claims, otherwise an anonymous public caller (<see cref="Actor.Candidate"/>).
/// </summary>
public sealed class ActorAccessor(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>Access-token claim with the user id (claims are not remapped to WS-* names).</summary>
    public const string UserIdClaim = "sub";

    /// <summary>Access-token claim with the role as stored, e.g. <c>HR_MANAGER</c>.</summary>
    public const string RoleClaim = "role";

    /// <summary>Access-token claim with the refresh-token family of the login.</summary>
    public const string SessionIdClaim = "sid";

    /// <summary>Present while the user must replace an issued password (<see cref="PasswordChangeGate"/>).</summary>
    public const string MustChangePasswordClaim = "pwd_change";

    public Actor Current()
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("ActorAccessor is used outside an HTTP request.");

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var ipAddress = httpContext.Connection.RemoteIpAddress;
        if (ipAddress is { IsIPv4MappedToIPv6: true })
        {
            ipAddress = ipAddress.MapToIPv4();
        }

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return Actor.Candidate(ipAddress, traceId);
        }

        if (!Guid.TryParse(user.FindFirstValue(UserIdClaim), out var userId)
            || !TryParseRole(user.FindFirstValue(RoleClaim), out var role))
        {
            throw new AppException(ErrorCode.Unauthenticated, "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
        }

        var sessionId = Guid.TryParse(user.FindFirstValue(SessionIdClaim), out var sid) ? sid : (Guid?)null;
        return Actor.User(userId, role, ipAddress, traceId, sessionId);
    }

    private static bool TryParseRole(string? value, out Role role)
    {
        foreach (var candidate in Enum.GetValues<Role>())
        {
            if (JsonNamingPolicy.SnakeCaseUpper.ConvertName(candidate.ToString()) == value)
            {
                role = candidate;
                return true;
            }
        }

        role = default;
        return false;
    }
}
