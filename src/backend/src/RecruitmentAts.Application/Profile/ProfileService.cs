using FluentValidation;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Profile.Contracts;

namespace RecruitmentAts.Application.Profile;

/// <summary>The signed-in user's own account. In M1: changing the issued password at first login.</summary>
public sealed class ProfileService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    AuditWriter audit,
    IClock clock,
    IValidator<ChangePasswordRequest> changePasswordValidator)
{
    /// <summary>
    /// Replaces the password and clears <c>MustChangePassword</c>; every other session of the user is
    /// signed out. The caller then refreshes to get an access token without the restriction.
    /// </summary>
    public async Task ChangePasswordAsync(ChangePasswordRequest request, Actor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        await changePasswordValidator.ValidateOrThrowAsync(request, cancellationToken);
        var userId = actor.UserId ?? throw new AppException(ErrorCode.Unauthenticated, "Vui lòng đăng nhập.");

        await unitOfWork.ExecuteAsync(async () =>
        {
            var user = await users.FindAsync(userId, cancellationToken)
                ?? throw new AppException(ErrorCode.Unauthenticated, "Vui lòng đăng nhập.");
            if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
            {
                throw AppException.Validation(
                    new Dictionary<string, string[]> { ["currentPassword"] = ["Mật khẩu hiện tại không đúng."] });
            }

            user.ChangePassword(passwordHasher.Hash(request.NewPassword));
            await refreshTokens.RevokeAllForUserAsync(user.Id, clock.UtcNow, actor.SessionId, cancellationToken);
            audit.Write(actor, AuditModule.User, AuditAction.Update, "User", user.Id, "Đổi mật khẩu");
        }, cancellationToken);
    }
}
