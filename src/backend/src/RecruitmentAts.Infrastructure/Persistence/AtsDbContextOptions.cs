using Microsoft.EntityFrameworkCore;

namespace RecruitmentAts.Infrastructure.Persistence;

internal static class AtsDbContextOptions
{
    /// <summary>One place for the provider settings, shared by DI, <c>dotnet ef</c>, and tests.</summary>
    public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history"))
            .UseSnakeCaseNamingConvention();
}
