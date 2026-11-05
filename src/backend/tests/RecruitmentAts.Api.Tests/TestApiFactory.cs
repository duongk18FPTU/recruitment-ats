using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace RecruitmentAts.Api.Tests;

/// <summary>
/// The real API with test settings: a JWT signing key, and a client IP per request (header
/// <c>X-Test-Ip</c>, otherwise random) so the per-IP rate limits do not leak between tests.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-signing-key-for-api-tests-only-0123456789";
    public const string TestIpHeader = "X-Test-Ip";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("ConnectionStrings:Ats", "Host=localhost;Database=not-used");
        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, TestIpStartupFilter>());
    }

    private sealed class TestIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = context.Request.Headers.TryGetValue(TestIpHeader, out var ip)
                    ? IPAddress.Parse(ip.ToString())
                    : new IPAddress(Random.Shared.NextInt64(0x01000000, 0xDF000000) & 0xFFFFFFFF);
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
