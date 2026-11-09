using FluentValidation;
using RecruitmentAts.Application.Applications.Contracts;
using RecruitmentAts.Application.Candidates.Contracts;

namespace RecruitmentAts.Application.Applications.Validation;

/// <summary>AC-US09 #5: a source from the BR-18 list, and a note when it is <c>OTHER</c>.</summary>
public sealed class ApplicationCreateRequestValidator : AbstractValidator<ApplicationCreateRequest>
{
    public ApplicationCreateRequestValidator()
    {
        RuleFor(x => x.PositionId).NotNull().WithMessage("Chọn vị trí tuyển dụng.");
        RuleFor(x => x.FullName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Nhập họ tên.")
            .Must(name => name!.Trim().Length is >= 2 and <= 120).WithMessage("Họ tên phải có từ 2 đến 120 ký tự.");
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(254).WithMessage("Email tối đa 254 ký tự.")
            .EmailAddress().WithMessage("Email không hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone)
            .Must(phone => phone!.Trim().Length <= 20).WithMessage("Số điện thoại tối đa 20 ký tự.")
            .When(x => x.Phone is not null);
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Nhập email hoặc số điện thoại.")
            .When(x => string.IsNullOrWhiteSpace(x.Email) && string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Source).NotNull().WithMessage("Chọn nguồn hồ sơ.").IsInEnum();
        RuleFor(x => x.SourceNote)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Ghi rõ nguồn khi chọn \"Khác\".")
            .MaximumLength(200).WithMessage("Ghi chú nguồn tối đa 200 ký tự.")
            .When(x => x.Source == Source.Other);
        RuleFor(x => x.SourceNote)
            .MaximumLength(200).WithMessage("Ghi chú nguồn tối đa 200 ký tự.")
            .When(x => x.Source != Source.Other);
        RuleFor(x => x.Notes).MaximumLength(5000).WithMessage("Ghi chú tối đa 5000 ký tự.");
        RuleFor(x => x.Cv).NotNull().WithMessage("Chọn tệp CV (PDF hoặc DOCX).");
    }
}

public sealed class CandidateMatchQueryValidator : AbstractValidator<CandidateMatchQuery>
{
    public CandidateMatchQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Nhập email hoặc số điện thoại.")
            .When(x => string.IsNullOrWhiteSpace(x.Email) && string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Email)
            .MaximumLength(254).WithMessage("Email tối đa 254 ký tự.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone)
            .Must(phone => phone!.Trim().Length <= 20).WithMessage("Số điện thoại tối đa 20 ký tự.")
            .When(x => x.Phone is not null);
    }
}
