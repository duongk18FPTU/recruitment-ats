using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Infrastructure.Persistence;
using RecruitmentAts.Infrastructure.Time;

namespace RecruitmentAts.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the adapters for every Application port.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IClock, SystemClock>();

        // Read when the context is first resolved, so hosts and tests without a database still start.
        services.AddDbContext<AtsDbContext>(options => AtsDbContextOptions.Configure(
            options,
            configuration.GetConnectionString("Ats")
                ?? throw new InvalidOperationException("Connection string 'Ats' (ConnectionStrings__Ats) is not set.")));

        return services;
    }
}
