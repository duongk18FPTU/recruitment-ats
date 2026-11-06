using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Infrastructure.Persistence.Seed;

namespace RecruitmentAts.Infrastructure.Persistence;

public static class DatabaseInitialization
{
    /// <summary>
    /// The one-off deployment step (docs/06-c4-architecture.md, "Migrations"): applies the EF Core
    /// migrations, then the seed data, and with <paramref name="demo"/> the demo data for trying the
    /// app locally (never in Production). Safe to repeat.
    /// </summary>
    public static async Task MigrateAndSeedAsync(this IServiceProvider services, CancellationToken cancellationToken, bool demo = false)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AtsDbContext>().Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
        if (demo)
        {
            await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(cancellationToken);
        }
    }
}
