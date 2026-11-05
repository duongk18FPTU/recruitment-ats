using FluentValidation;
using RecruitmentAts.Application.Auth.Contracts;

namespace RecruitmentAts.Application.Auth.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Nhập email.").MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().WithMessage("Nhập mật khẩu.").MaximumLength(128);
    }
}
