using FluentValidation;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Auth.Contracts;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Profile.Contracts;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Application.Auth;

/// <summary>Login and refresh-token rotation (US01, AD-06; docs/11 seq_login, seq_refresh_token).</summary>
public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    AuditWriter audit,
    IClock clock,
    IValidator<LoginRequest> loginValidator)
{
    private const int UserAgentMaxLength = 300;

    /// <summary>
    /// Unknown email, deactivated or locked account, and wrong password all end in the same
    /// <c>INVALID_CREDENTIALS</c>, so the response never reveals which accounts exist (AC-US01 #2).
    /// </summary>
    public async Task<AuthResult> LoginAsync(LoginRequest request, string? userAgent, Actor caller, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(caller);
        await loginValidator.ValidateOrThrowAsync(request, cancellationToken);

        // Failed attempts are saved too, so the outcome is decided inside the transaction and thrown after it.
        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            var user = await users.FindByEmailAsync(request.Email.Trim(), cancellationToken);
            if (user is null)
            {
                return null;
            }

            var now = clock.UtcNow;
            var actor = Actor.User(user.Id, user.Role, caller.IpAddress, caller.TraceId);
            if (!user.IsActive || user.IsLocked(now))
            {
                audit.Write(actor, AuditModule.Auth, AuditAction.LoginFailed, "User", user.Id,
                    user.IsActive ? "Đăng nhập khi tài khoản đang bị khoá" : "Đăng nhập vào tài khoản đã vô hiệu hoá");
                return null;
            }

            if (!passwordHasher.Verify(user.PasswordHash, request.Password))
            {
                user.RegisterFailedLogin(now);
                audit.Write(actor, AuditModule.Auth, AuditAction.LoginFailed, "User", user.Id,
                    user.IsLocked(now) ? "Sai mật khẩu 5 lần, khoá tài khoản 15 phút" : "Sai mật khẩu");
                return null;
            }

            user.RegisterSuccessfulLogin(now);
            var sessionId = Guid.CreateVersion7();
            audit.Write(actor with { SessionId = sessionId }, AuditModule.Auth, AuditAction.Login, "User", user.Id, "Đăng nhập");
            return StartSession(user, sessionId, userAgent, caller, now);
        }, cancellationToken);

        return result ?? throw new AppException(ErrorCode.InvalidCredentials, "Email hoặc mật khẩu không đúng.");
    }

    /// <summary>
    /// Rotates the refresh token. Presenting a token that was already rotated means it was copied:
    /// the whole family is revoked and the caller must log in again.
    /// </summary>
    public async Task<AuthResult> RefreshAsync(string? rawRefreshToken, string? userAgent, Actor caller, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(caller);
        if (string.IsNullOrEmpty(rawRefreshToken))
        {
            throw SessionEnded();
        }

        var result = await unitOfWork.ExecuteAsync(async () =>
        {
            var now = clock.UtcNow;
            var current = await refreshTokens.FindByHashAsync(tokenIssuer.HashRefreshToken(rawRefreshToken), cancellationToken);
            if (current is null || current.ExpiresAt <= now)
            {
                return null;
            }

            if (current.RevokedAt is not null)
            {
                await refreshTokens.RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
                return null;
            }

            var user = await users.FindAsync(current.UserId, cancellationToken);
            if (user is null || !user.IsActive)
            {
                await refreshTokens.RevokeFamilyAsync(current.FamilyId, now, cancellationToken);
                return null;
            }

            return StartSession(user, current.FamilyId, userAgent, caller, now, rotated: current);
        }, cancellationToken);

        return result ?? throw SessionEnded();
    }

    private AuthResult StartSession(
        User user,
        Guid familyId,
        string? userAgent,
        Actor caller,
        DateTimeOffset now,
        RefreshToken? rotated = null)
    {
        var raw = tokenIssuer.NewRefreshToken();
        var next = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = raw.Hash,
            FamilyId = familyId,
            ExpiresAt = now + RefreshToken.Lifetime,
            CreatedIp = caller.IpAddress,
            UserAgent = userAgent is { Length: > UserAgentMaxLength } ? userAgent[..UserAgentMaxLength] : userAgent,
        };
        refreshTokens.Add(next);
        rotated?.RotateTo(next, now);

        var accessToken = tokenIssuer.IssueAccessToken(user, familyId);
        return new AuthResult(
            new AuthResponse(accessToken.Value, accessToken.ExpiresAt, UserProfile.From(user)),
            raw.Value,
            next.ExpiresAt);
    }

    private static AppException SessionEnded() =>
        new(ErrorCode.Unauthenticated, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
}
