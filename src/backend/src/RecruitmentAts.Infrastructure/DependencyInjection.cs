using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Infrastructure.Time;

namespace RecruitmentAts.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the adapters for every Application port.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
