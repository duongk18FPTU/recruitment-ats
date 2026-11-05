using FluentValidation;

namespace RecruitmentAts.Application.Common.Validation;

public static class RuleBuilderExtensions
{
    /// <summary><c>page</c> from 1, <c>pageSize</c> 1–100 (OpenAPI parameters <c>Page</c>, <c>PageSize</c>).</summary>
    public static void IncludePageRules<T>(this AbstractValidator<T> validator)
        where T : PageQuery
    {
        ArgumentNullException.ThrowIfNull(validator);
        validator.RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Trang bắt đầu từ 1.");
        validator.RuleFor(x => x.PageSize).InclusiveBetween(1, PageQuery.MaxPageSize)
            .WithMessage($"Số dòng mỗi trang từ 1 đến {PageQuery.MaxPageSize}.");
    }

    /// <summary>A password a user sets or an Admin issues: 10–128 characters with letters and digits.</summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilderInitial<T, string> rule, string label)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        return rule
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage($"Nhập {char.ToLowerInvariant(label[0])}{label[1..]}.")
            .Length(10, 128).WithMessage($"{label} phải có từ 10 đến 128 ký tự.")
            .Must(password => password.Any(char.IsLetter) && password.Any(char.IsDigit))
            .WithMessage($"{label} phải có cả chữ và số.");
    }
}
