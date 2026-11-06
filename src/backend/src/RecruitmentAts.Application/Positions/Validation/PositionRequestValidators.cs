using System.Globalization;
using FluentValidation;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Positions.Contracts;

namespace RecruitmentAts.Application.Positions.Validation;

public sealed class PositionCreateRequestValidator : AbstractValidator<PositionCreateRequest>
{
    public PositionCreateRequestValidator(IClock clock)
    {
        this.IncludePositionFieldRules(clock);
        RuleFor(x => x.Status)
            .Must(status => status is null or PositionStatus.Draft or PositionStatus.Open)
            .WithMessage("Vị trí mới chỉ được lưu nháp hoặc mở tuyển.");
    }
}

public sealed class PositionUpdateRequestValidator : AbstractValidator<PositionUpdateRequest>
{
    public PositionUpdateRequestValidator(IClock clock)
    {
        this.IncludePositionFieldRules(clock);
        RuleFor(x => x.RowVersion)
            .Must(rowVersion => uint.TryParse(rowVersion, NumberStyles.None, CultureInfo.InvariantCulture, out _))
            .WithMessage("Thiếu phiên bản dữ liệu; tải lại vị trí rồi sửa.");
    }
}

public sealed class CriterionInputValidator : AbstractValidator<CriterionInput>
{
    public CriterionInputValidator()
    {
        RuleFor(x => x.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nhập tên tiêu chí.")
            .Must(name => name.Trim().Length is >= 2 and <= 150).WithMessage("Tên tiêu chí phải có từ 2 đến 150 ký tự.");
        RuleFor(x => x.Description).MaximumLength(500).WithMessage("Mô tả tiêu chí tối đa 500 ký tự.");
        RuleFor(x => x.Type).NotNull().WithMessage("Chọn loại tiêu chí.").IsInEnum();
        RuleFor(x => x.Weight).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Chọn trọng số.")
            .InclusiveBetween(1, 5).WithMessage("Trọng số phải từ 1 đến 5.");
    }
}

/// <summary>Rules of OpenAPI <c>PositionFields</c> (AC-US05 #2).</summary>
public static class PositionFieldRules
{
    public const int MaxCriteria = 30;

    public static void IncludePositionFieldRules<T>(this AbstractValidator<T> validator, IClock clock)
        where T : IPositionFields
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(clock);

        validator.RuleFor(x => x.Title).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nhập tên vị trí.")
            .Must(title => title.Trim().Length is >= 2 and <= 150).WithMessage("Tên vị trí phải có từ 2 đến 150 ký tự.");
        validator.RuleFor(x => x.DepartmentId).NotNull().WithMessage("Chọn phòng ban.");
        validator.RuleFor(x => x.Location).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nhập địa điểm.")
            .MaximumLength(100).WithMessage("Địa điểm tối đa 100 ký tự.");
        validator.RuleFor(x => x.EmploymentType).NotNull().WithMessage("Chọn hình thức.").IsInEnum();
        validator.RuleFor(x => x.Level).NotNull().WithMessage("Chọn cấp bậc.").IsInEnum();
        validator.RuleFor(x => x.Headcount).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Nhập số lượng.")
            .GreaterThanOrEqualTo(1).WithMessage("Số lượng phải từ 1 trở lên.");
        validator.RuleFor(x => x.TargetHireDate).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Chọn hạn tuyển.")
            .Must(date => date > BusinessCalendar.Today(clock)).WithMessage("Hạn tuyển phải sau hôm nay.");
        validator.RuleFor(x => x.Description).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nhập mô tả công việc.")
            .MaximumLength(20000).WithMessage("Mô tả công việc tối đa 20.000 ký tự.");
        validator.RuleFor(x => x.Criteria).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Thêm ít nhất một tiêu chí đánh giá.")
            .Must(criteria => criteria!.Count <= MaxCriteria).WithMessage($"Tối đa {MaxCriteria} tiêu chí.");
        validator.RuleForEach(x => x.Criteria).SetValidator(new CriterionInputValidator());
    }
}
