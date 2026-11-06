using FluentValidation;
using RecruitmentAts.Application.Common.Validation;
using RecruitmentAts.Application.Positions.Contracts;

namespace RecruitmentAts.Application.Positions.Validation;

public sealed class PositionQueryValidator : AbstractValidator<PositionQuery>
{
    public PositionQueryValidator()
    {
        this.IncludePageRules();
        RuleFor(x => x.Q).MaximumLength(100).WithMessage("Từ khoá tối đa 100 ký tự.");
        RuleFor(x => x.Sort)
            .Must(sort => sort is null || PositionQuery.SortValues.Contains(sort))
            .WithMessage($"Sắp xếp không hợp lệ. Chấp nhận: {string.Join(", ", PositionQuery.SortValues)}.");
    }
}
