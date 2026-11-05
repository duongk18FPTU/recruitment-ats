using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using RecruitmentAts.Api.Errors;
using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Api.RateLimiting;

/// <summary>Rate limits of docs/08-openapi-contract.md ("Rate limits"); exceeding one is 429 <c>RATE_LIMITED</c>.</summary>
public static class RateLimits
{
    /// <summary>10 login attempts per IP per minute.</summary>
    public const string Login = "login";

    public static IServiceCollection AddAtsRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(Login, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                var problem = ProblemDetailsMapper.Map(
                    new AppException(ErrorCode.RateLimited, "Bạn thao tác quá nhiều lần. Vui lòng thử lại sau ít phút."), http);
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await http.RequestServices.GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem });
            };
        });
}
