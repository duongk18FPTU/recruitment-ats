using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Common.Audit;

namespace RecruitmentAts.Application;

public static class DependencyInjection
{
    /// <summary>Registers the use-case services of every module.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuditWriter>();

        return services;
    }
}
