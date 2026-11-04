using Microsoft.Extensions.DependencyInjection;

namespace RecruitmentAts.Application;

public static class DependencyInjection
{
    /// <summary>Registers the use-case services of every module.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services) => services;
}
