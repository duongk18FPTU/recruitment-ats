namespace RecruitmentAts.Application.Common;

/// <summary>
/// A business error with a stable <see cref="ErrorCode"/>. The Api turns it into a
/// problem+json response; <see cref="Extensions"/> become extra members of that response
/// (for example <c>allowedStages</c> or <c>existingApplicationId</c>).
/// </summary>
public sealed class AppException : Exception
{
    public AppException(
        ErrorCode code,
        string detail,
        IReadOnlyDictionary<string, object?>? extensions = null,
        Exception? innerException = null)
        : base(detail, innerException)
    {
        Code = code;
        Detail = detail;
        Extensions = extensions ?? new Dictionary<string, object?>();
    }

    public ErrorCode Code { get; }

    /// <summary>Vietnamese message that is safe to show to the user.</summary>
    public string Detail { get; }

    public IReadOnlyDictionary<string, object?> Extensions { get; }

    /// <summary><c>VALIDATION_FAILED</c> with <c>errors</c>: field path (camelCase) → messages.</summary>
    public static AppException Validation(IReadOnlyDictionary<string, string[]> errors, string detail = "Dữ liệu không hợp lệ.") =>
        new(ErrorCode.ValidationFailed, detail, new Dictionary<string, object?> { ["errors"] = errors });

    public static AppException NotFound(string detail = "Không tìm thấy dữ liệu.") => new(ErrorCode.NotFound, detail);

    public static AppException Forbidden(string detail = "Bạn không có quyền thực hiện thao tác này.") =>
        new(ErrorCode.Forbidden, detail);
}
