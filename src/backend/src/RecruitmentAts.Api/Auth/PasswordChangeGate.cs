using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Api.Auth;

/// <summary>Marks the endpoints a user may call before replacing an issued password.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowPendingPasswordChangeAttribute : Attribute;

/// <summary>
/// While the access token carries <see cref="ActorAccessor.MustChangePasswordClaim"/>, every endpoint
/// without <see cref="AllowPendingPasswordChangeAttribute"/> answers 403 <c>PASSWORD_CHANGE_REQUIRED</c>,
/// so the restriction cannot be skipped by calling the API directly.
/// </summary>
public sealed class PasswordChangeGate(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.User.HasClaim(c => c.Type == ActorAccessor.MustChangePasswordClaim)
            && context.GetEndpoint()?.Metadata.GetMetadata<AllowPendingPasswordChangeAttribute>() is null)
        {
            throw new AppException(ErrorCode.PasswordChangeRequired, "Bạn cần đổi mật khẩu trước khi tiếp tục.");
        }

        return next(context);
    }
}
