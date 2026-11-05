using FluentValidation;
using RecruitmentAts.Application.Profile.Contracts;

namespace RecruitmentAts.Application.Profile.Validation;

/// <summary>New password: 10–128 characters with letters and digits (OpenAPI), different from the current one.</summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Nhập mật khẩu hiện tại.");
        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nhập mật khẩu mới.")
            .Length(10, 128).WithMessage("Mật khẩu mới phải có từ 10 đến 128 ký tự.")
            .Must(password => password.Any(char.IsLetter) && password.Any(char.IsDigit))
            .WithMessage("Mật khẩu mới phải có cả chữ và số.")
            .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");
    }
}
