using Microsoft.AspNetCore.Diagnostics;
using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Api.Errors;

/// <summary>
/// Writes every exception as problem+json. <see cref="AppException"/> keeps its code and detail;
/// anything else is <c>INTERNAL_ERROR</c> with a generic message — details only in the log,
/// linked by <c>traceId</c>.
/// </summary>
internal sealed partial class AppExceptionHandler(IProblemDetailsService problemDetails, ILogger<AppExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var appException = exception switch
        {
            AppException known => known,
            BadHttpRequestException bad => Framework(bad.StatusCode),
            _ => null,
        };

        if (appException is null)
        {
            LogUnhandled(logger, exception);
            appException = Framework(StatusCodes.Status500InternalServerError);
        }

        var problem = ProblemDetailsMapper.Map(appException, httpContext);
        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static AppException Framework(int status)
    {
        var code = ProblemDetailsMapper.CodeOf(status);
        return new AppException(code, ProblemDetailsMapper.DefaultDetail(code));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandled(ILogger logger, Exception exception);
}
