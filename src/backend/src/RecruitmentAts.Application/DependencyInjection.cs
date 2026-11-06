using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Auth;
using RecruitmentAts.Application.Auth.Contracts;
using RecruitmentAts.Application.Auth.Validation;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Lookups;
using RecruitmentAts.Application.Lookups.Contracts;
using RecruitmentAts.Application.Lookups.Validation;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Positions.Contracts;
using RecruitmentAts.Application.Positions.Validation;
using RecruitmentAts.Application.Profile;
using RecruitmentAts.Application.Profile.Contracts;
using RecruitmentAts.Application.Profile.Validation;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Application.Users.Contracts;
using RecruitmentAts.Application.Users.Validation;

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

        services.AddScoped<UserService>();
        services.AddSingleton<IValidator<UserQuery>, UserQueryValidator>();
        services.AddSingleton<IValidator<UserCreateRequest>, UserCreateRequestValidator>();
        services.AddSingleton<IValidator<UserUpdateRequest>, UserUpdateRequestValidator>();
        services.AddSingleton<IValidator<UserStatusRequest>, UserStatusRequestValidator>();

        services.AddScoped<LookupService>();
        services.AddSingleton<IValidator<UserOptionQuery>, UserOptionQueryValidator>();

        services.AddScoped<PositionService>();
        services.AddSingleton<IValidator<PositionQuery>, PositionQueryValidator>();
        services.AddSingleton<IValidator<PositionCreateRequest>, PositionCreateRequestValidator>();

        return services;
    }
}
