using FluentValidation;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Users.Contracts;

namespace RecruitmentAts.Application.Users;

/// <summary>
/// User administration (US20, Admin only — enforced by the Api from <c>x-roles</c>). Accounts are
/// issued with a temporary password that must be replaced at the first login (AC-US20 #5).
/// </summary>
public sealed class UserService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    AuditWriter audit,
    IClock clock,
    IValidator<UserQuery> queryValidator,
    IValidator<UserCreateRequest> createValidator,
    IValidator<UserUpdateRequest> updateValidator,
    IValidator<UserStatusRequest> statusValidator)
{
    public async Task<PagedResult<UserAccount>> ListAsync(UserQuery query, CancellationToken cancellationToken)
    {
        await queryValidator.ValidateOrThrowAsync(query, cancellationToken);
        var page = await users.ListAsync(query with { Q = query.Q?.Trim() }, cancellationToken);
        return page.Map(UserAccount.From);
    }

    public async Task<UserAccount> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        UserAccount.From(await users.FindAsync(userId, cancellationToken) ?? throw UserNotFound());

    public async Task<UserAccount> CreateAsync(UserCreateRequest request, Actor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);
        var email = request.Email.Trim();

        return await unitOfWork.ExecuteAsync(async () =>
        {
            if (await users.EmailExistsAsync(email, null, cancellationToken))
            {
                throw DuplicateEmail();
            }

            var user = new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(request.TemporaryPassword),
                FullName = request.FullName.Trim(),
                Phone = Blank(request.Phone),
                Role = request.Role!.Value,
                MustChangePassword = true,
                CreatedAt = clock.UtcNow, // returned before the interceptor sets it on save
            };
            users.Add(user);
            audit.Write(actor, AuditModule.User, AuditAction.Create, "User", user.Id, $"Tạo người dùng {user.Email}",
                new { user.Email, user.FullName, user.Role });
            return UserAccount.From(user);
        }, cancellationToken);
    }

    public async Task<UserAccount> UpdateAsync(Guid userId, UserUpdateRequest request, Actor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        await updateValidator.ValidateOrThrowAsync(request, cancellationToken);
        var email = request.Email.Trim();

        return await unitOfWork.ExecuteAsync(async () =>
        {
            var user = await users.FindAsync(userId, cancellationToken) ?? throw UserNotFound();
            if (await users.EmailExistsAsync(email, user.Id, cancellationToken))
            {
                throw DuplicateEmail();
            }

            var before = new { user.Email, user.FullName, user.Phone, user.Role };
            user.Email = email;
            user.FullName = request.FullName.Trim();
            user.Phone = Blank(request.Phone);
            user.Role = request.Role!.Value;

            var passwordIssued = request.TemporaryPassword is not null;
            if (passwordIssued)
            {
                user.IssuePassword(passwordHasher.Hash(request.TemporaryPassword!));
                await refreshTokens.RevokeAllForUserAsync(user.Id, clock.UtcNow, null, cancellationToken);
            }

            audit.Write(actor, AuditModule.User, AuditAction.Update, "User", user.Id,
                passwordIssued ? $"Sửa người dùng {user.Email} và cấp mật khẩu tạm" : $"Sửa người dùng {user.Email}",
                new { before, after = new { user.Email, user.FullName, user.Phone, user.Role }, passwordIssued });
            return UserAccount.From(user);
        }, cancellationToken);
    }

    /// <summary>
    /// Deactivating signs the user out everywhere (refresh tokens revoked; an access token already
    /// issued lasts at most 15 minutes). An Admin cannot deactivate their own account.
    /// </summary>
    public async Task<UserAccount> SetActiveAsync(Guid userId, UserStatusRequest request, Actor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        await statusValidator.ValidateOrThrowAsync(request, cancellationToken);
        var isActive = request.IsActive!.Value;
        if (!isActive && userId == actor.UserId)
        {
            throw new AppException(ErrorCode.SelfDeactivation, "Bạn không thể vô hiệu hoá tài khoản của chính mình.");
        }

        return await unitOfWork.ExecuteAsync(async () =>
        {
            var user = await users.FindAsync(userId, cancellationToken) ?? throw UserNotFound();
            if (user.IsActive == isActive)
            {
                return UserAccount.From(user);
            }

            if (isActive)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
                await refreshTokens.RevokeAllForUserAsync(user.Id, clock.UtcNow, null, cancellationToken);
            }

            audit.Write(actor, AuditModule.User, AuditAction.StatusChange, "User", user.Id,
                isActive ? $"Kích hoạt người dùng {user.Email}" : $"Vô hiệu hoá người dùng {user.Email}",
                new { isActive });
            return UserAccount.From(user);
        }, cancellationToken);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AppException UserNotFound() => AppException.NotFound("Không tìm thấy người dùng.");

    private static AppException DuplicateEmail() =>
        new(ErrorCode.DuplicateEmail, "Email này đã được dùng cho một người dùng khác.");
}
