using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Api.Errors;

/// <summary>
/// The only place where an <see cref="ErrorCode"/> becomes an HTTP status
/// (docs/08-openapi-contract.md, "Error codes"). Every problem+json response carries
/// <c>type</c>, <c>title</c>, <c>status</c>, <c>detail</c>, <c>errorCode</c>, and <c>traceId</c>.
/// </summary>
public static class ProblemDetailsMapper
{
    private const string TypeBase = "https://ats.example.vn/errors/";

    public static ProblemDetails Map(AppException exception, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(httpContext);

        var problem = new ProblemDetails
        {
            Status = StatusOf(exception.Code),
            Detail = exception.Detail,
            Instance = httpContext.Request.Path,
        };
        Describe(problem, exception.Code);

        foreach (var (name, value) in exception.Extensions)
        {
            problem.Extensions[name] = value;
        }

        return problem;
    }

    /// <summary>
    /// Completes problems created by the framework (unknown route, model binding, auth challenge,
    /// status-code pages) so they follow the same contract. <c>traceId</c> is added by ASP.NET Core.
    /// </summary>
    public static void Complete(ProblemDetails problem, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(problem);
        ArgumentNullException.ThrowIfNull(httpContext);

        problem.Instance ??= httpContext.Request.Path;
        if (problem.Extensions.ContainsKey("errorCode"))
        {
            return;
        }

        var code = CodeOf(problem.Status ?? StatusCodes.Status500InternalServerError);
        Describe(problem, code);
        problem.Detail = DefaultDetail(code);

        if (problem is ValidationProblemDetails validation)
        {
            var errors = validation.Errors.ToDictionary(e => CamelCasePath(e.Key), e => e.Value);
            validation.Errors.Clear();
            foreach (var (field, messages) in errors)
            {
                validation.Errors[field] = messages;
            }
        }
    }

    public static int StatusOf(ErrorCode code) => code switch
    {
        ErrorCode.ValidationFailed
            or ErrorCode.ExportTooLarge
            or ErrorCode.UnsupportedFileType
            or ErrorCode.NoExtractableText
            or ErrorCode.CaptchaFailed => StatusCodes.Status400BadRequest,
        ErrorCode.InvalidCredentials
            or ErrorCode.Unauthenticated => StatusCodes.Status401Unauthorized,
        ErrorCode.Forbidden
            or ErrorCode.PasswordChangeRequired => StatusCodes.Status403Forbidden,
        ErrorCode.NotFound
            or ErrorCode.ConsentTokenInvalid => StatusCodes.Status404NotFound,
        ErrorCode.ConcurrencyConflict
            or ErrorCode.DuplicateEmail
            or ErrorCode.DuplicateApplication
            or ErrorCode.PositionNotOpen
            or ErrorCode.InvalidPositionStatusChange
            or ErrorCode.InvalidStageTransition
            or ErrorCode.AnalysisInProgress
            or ErrorCode.AnalysisNotCompleted
            or ErrorCode.ApplicationNotInInterview
            or ErrorCode.InterviewOverlap
            or ErrorCode.InterviewCancelled
            or ErrorCode.ScorecardAlreadySubmitted
            or ErrorCode.EmailNotPending
            or ErrorCode.EmailNotFailed
            or ErrorCode.InboxItemProcessed
            or ErrorCode.ImportInvalidRows
            or ErrorCode.ImportExpired
            or ErrorCode.SelfDeactivation
            or ErrorCode.JobPostExpired => StatusCodes.Status409Conflict,
        ErrorCode.FileTooLarge => StatusCodes.Status413PayloadTooLarge,
        ErrorCode.RateLimited => StatusCodes.Status429TooManyRequests,
        ErrorCode.AiUnavailable => StatusCodes.Status502BadGateway,
        ErrorCode.InternalError => StatusCodes.Status500InternalServerError,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Error code has no HTTP status."),
    };

    /// <summary>The value written to <c>errorCode</c>, e.g. <c>INVALID_STAGE_TRANSITION</c>.</summary>
    public static string NameOf(ErrorCode code) => JsonNamingPolicy.SnakeCaseUpper.ConvertName(code.ToString());

    /// <summary>The code for a status the framework produced without an <see cref="AppException"/>.</summary>
    public static ErrorCode CodeOf(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => ErrorCode.Unauthenticated,
        StatusCodes.Status403Forbidden => ErrorCode.Forbidden,
        StatusCodes.Status404NotFound => ErrorCode.NotFound,
        StatusCodes.Status413PayloadTooLarge => ErrorCode.FileTooLarge,
        StatusCodes.Status429TooManyRequests => ErrorCode.RateLimited,
        >= 400 and < 500 => ErrorCode.ValidationFailed,
        _ => ErrorCode.InternalError,
    };

    /// <summary>Vietnamese message for problems that have no business-specific detail.</summary>
    public static string DefaultDetail(ErrorCode code) => code switch
    {
        ErrorCode.Unauthenticated => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
        ErrorCode.Forbidden => "Bạn không có quyền thực hiện thao tác này.",
        ErrorCode.NotFound => "Không tìm thấy dữ liệu.",
        ErrorCode.FileTooLarge => "Tệp vượt quá dung lượng cho phép.",
        ErrorCode.RateLimited => "Bạn thao tác quá nhiều lần. Vui lòng thử lại sau.",
        ErrorCode.ValidationFailed => "Dữ liệu không hợp lệ.",
        _ => "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.",
    };

    /// <summary><c>Criteria[0].Weight</c> → <c>criteria[0].weight</c>, as the fields are named in JSON.</summary>
    private static string CamelCasePath(string path) =>
        string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));

    private static void Describe(ProblemDetails problem, ErrorCode code)
    {
        var words = JsonNamingPolicy.SnakeCaseLower.ConvertName(code.ToString()).Replace('_', ' ');

        problem.Status ??= StatusOf(code);
        problem.Type = TypeBase + JsonNamingPolicy.KebabCaseLower.ConvertName(code.ToString());
        problem.Title = char.ToUpperInvariant(words[0]) + words[1..];
        problem.Extensions["errorCode"] = NameOf(code);
    }
}
