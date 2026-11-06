using FluentValidation;
using RecruitmentAts.Application.Lookups.Contracts;

namespace RecruitmentAts.Application.Lookups.Validation;

public sealed class UserOptionQueryValidator : AbstractValidator<UserOptionQuery>
{
    public UserOptionQueryValidator()
    {
        RuleFor(x => x.Q).MaximumLength(100).WithMessage("Từ khoá tối đa 100 ký tự.");
    }
}
