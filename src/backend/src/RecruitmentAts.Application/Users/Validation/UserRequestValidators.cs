using FluentValidation;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Users.Contracts;

namespace RecruitmentAts.Application.Users.Validation;

public sealed class UserQueryValidator : AbstractValidator<UserQuery>
{
    public UserQueryValidator()
    {
        this.IncludePageRules();
        RuleFor(x => x.Q).MaximumLength(100).WithMessage("Từ khoá tối đa 100 ký tự.");
    }
}

public sealed class UserCreateRequestValidator : AbstractValidator<UserCreateRequest>
{
    public UserCreateRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.FullName).ValidFullName();
        RuleFor(x => x.Phone).ValidPhone();
        RuleFor(x => x.Role).NotNull().WithMessage("Chọn vai trò.").IsInEnum();
        RuleFor(x => x.TemporaryPassword).StrongPassword("Mật khẩu tạm");
    }
}

public sealed class UserUpdateRequestValidator : AbstractValidator<UserUpdateRequest>
{
    public UserUpdateRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.FullName).ValidFullName();
        RuleFor(x => x.Phone).ValidPhone();
        RuleFor(x => x.Role).NotNull().WithMessage("Chọn vai trò.").IsInEnum();
        RuleFor(x => x.TemporaryPassword!).StrongPassword("Mật khẩu tạm").When(x => x.TemporaryPassword is not null);
    }
}

public sealed class UserStatusRequestValidator : AbstractValidator<UserStatusRequest>
{
    public UserStatusRequestValidator()
    {
        RuleFor(x => x.IsActive).NotNull().WithMessage("Chọn trạng thái.");
    }
}

internal static class UserFieldRules
{
    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilderInitial<T, string> rule) => rule
        .Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("Nhập email.")
        .MaximumLength(254).WithMessage("Email tối đa 254 ký tự.")
        .EmailAddress().WithMessage("Email không hợp lệ.");

    public static IRuleBuilderOptions<T, string> ValidFullName<T>(this IRuleBuilderInitial<T, string> rule) => rule
        .Cascade(CascadeMode.Stop)
        .NotEmpty().WithMessage("Nhập họ tên.")
        .Must(name => name.Trim().Length is >= 2 and <= 120).WithMessage("Họ tên phải có từ 2 đến 120 ký tự.");

    public static IRuleBuilderOptions<T, string?> ValidPhone<T>(this IRuleBuilder<T, string?> rule) => rule
        .MaximumLength(20).WithMessage("Số điện thoại tối đa 20 ký tự.");
}
