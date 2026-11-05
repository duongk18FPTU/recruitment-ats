using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Auth;
using RecruitmentAts.Application.Auth.Contracts;
using RecruitmentAts.Application.Auth.Validation;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Profile;
using RecruitmentAts.Application.Profile.Contracts;
using RecruitmentAts.Application.Profile.Validation;

namespace RecruitmentAts.Application;

public static class DependencyInjection
{
    /// <summary>Registers the use-case services of every module.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuditWriter>();

        services.AddScoped<AuthService>();
        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();

        services.AddScoped<ProfileService>();
        services.AddSingleton<IValidator<ChangePasswordRequest>, ChangePasswordRequestValidator>();

        return services;
    }
}
