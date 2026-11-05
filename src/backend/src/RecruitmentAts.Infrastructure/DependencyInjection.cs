using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Infrastructure.Persistence;
using RecruitmentAts.Infrastructure.Persistence.Repositories;
using RecruitmentAts.Infrastructure.Persistence.Seed;
using RecruitmentAts.Infrastructure.Security;
using RecruitmentAts.Infrastructure.Time;

namespace RecruitmentAts.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the adapters for every Application port.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<TimestampInterceptor>();

        // Read when the context is first resolved, so hosts and tests without a database still start.
        services.AddDbContext<AtsDbContext>((provider, options) =>
        {
            AtsDbContextOptions.Configure(
                options,
                configuration.GetConnectionString("Ats")
                    ?? throw new InvalidOperationException("Connection string 'Ats' (ConnectionStrings__Ats) is not set."));
            options.AddInterceptors(provider.GetRequiredService<TimestampInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<DatabaseSeeder>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenIssuer>(provider =>
            new JwtTokenIssuer(configuration, provider.GetRequiredService<IClock>()));

        return services;
    }
}
