using FluentValidation;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Profile.Contracts;

namespace RecruitmentAts.Application.Profile.Validation;

/// <summary>New password: 10–128 characters with letters and digits (OpenAPI), different from the current one.</summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Nhập mật khẩu hiện tại.");
        RuleFor(x => x.NewPassword)
            .StrongPassword("Mật khẩu mới")
            .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");
    }
}
