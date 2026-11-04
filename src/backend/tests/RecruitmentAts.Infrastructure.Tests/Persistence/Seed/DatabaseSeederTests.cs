using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RecruitmentAts.Application;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Lookups;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RecruitmentAts.Infrastructure.Tests.Persistence.Seed;

/// <summary>The deployment step `migrate`: migrations, then the seed data; safe to repeat.</summary>
public sealed class DatabaseSeederTests(SeedPostgres postgres) : IClassFixture<SeedPostgres>
{
    private static readonly Dictionary<string, string?> FirstRun = new()
    {
        ["Seed:AdminEmail"] = "admin@example.test",
        ["Seed:AdminPassword"] = "Admin-Password-1",
        ["Seed:AdminFullName"] = "Quản trị viên",
        ["Seed:Departments:0"] = "Phòng Nhân sự",
        ["Seed:Departments:1"] = "Phòng Công nghệ thông tin",
    };

    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task First_run_creates_the_Admin_and_the_departments()
    {
        await using var services = await postgres.NewDatabaseAsync(FirstRun);

        await services.MigrateAndSeedAsync(_ct);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        var admin = await db.Set<User>().SingleAsync(_ct);
        Assert.Equal("admin@example.test", admin.Email);
        Assert.Equal(Role.Admin, admin.Role);
        Assert.True(admin.IsActive);
        Assert.True(scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Verify(admin.PasswordHash, "Admin-Password-1"));

        var departments = await db.Set<Department>().OrderBy(d => d.SortOrder).ToListAsync(_ct);
        Assert.Equal(["Phòng Nhân sự", "Phòng Công nghệ thông tin"], departments.Select(d => d.Name));
        Assert.All(departments, d => Assert.True(d.IsActive));

        var entry = await db.Set<AuditLog>().SingleAsync(_ct);
        Assert.Equal((AuditActorType.System, AuditModule.User, AuditAction.Create, admin.Id),
            (entry.ActorType, entry.Module, entry.Action, entry.EntityId));
        Assert.DoesNotContain("Password", entry.Details!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Later_runs_add_only_new_departments_and_need_no_Admin_secrets()
    {
        await using var first = await postgres.NewDatabaseAsync(FirstRun);
        await first.MigrateAndSeedAsync(_ct);

        await using var later = postgres.Services(first.Database, new()
        {
            ["Seed:Departments:0"] = "Phòng Nhân sự",
            ["Seed:Departments:1"] = "Phòng Công nghệ thông tin",
            ["Seed:Departments:2"] = "Phòng Kinh doanh",
        });
        await later.MigrateAndSeedAsync(_ct);
        await later.MigrateAndSeedAsync(_ct);

        await using var scope = later.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        Assert.Equal(1, await db.Set<User>().CountAsync(_ct));
        Assert.Equal(1, await db.Set<AuditLog>().CountAsync(_ct));
        Assert.Equal(["Phòng Nhân sự", "Phòng Công nghệ thông tin", "Phòng Kinh doanh"],
            await db.Set<Department>().OrderBy(d => d.SortOrder).Select(d => d.Name).ToListAsync(_ct));
    }

    [Theory]
    [InlineData(null, "Admin-Password-1")]
    [InlineData("not-an-email", "Admin-Password-1")]
    [InlineData("admin@example.test", null)]
    [InlineData("admin@example.test", "short")]
    public async Task Without_an_Admin_the_secrets_are_required_and_nothing_is_written(string? email, string? password)
    {
        await using var services = await postgres.NewDatabaseAsync(new()
        {
            ["Seed:AdminEmail"] = email,
            ["Seed:AdminPassword"] = password,
            ["Seed:Departments:0"] = "Phòng Nhân sự",
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => services.MigrateAndSeedAsync(_ct));

        Assert.Contains("Seed__Admin", error.Message, StringComparison.Ordinal);
        await using var scope = services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<AtsDbContext>().Set<Department>().AnyAsync(_ct));
    }
}

/// <summary>One container; each test gets its own empty database.</summary>
public sealed class SeedPostgres : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16").Build();

    public ValueTask InitializeAsync() => new(_postgres.StartAsync());

    public ValueTask DisposeAsync() => _postgres.DisposeAsync();

    public async Task<SeedServices> NewDatabaseAsync(Dictionary<string, string?> settings)
    {
        var name = "seed_" + Guid.NewGuid().ToString("N");
        await using (var connection = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE {name}", connection);
            await command.ExecuteNonQueryAsync();
        }

        return Services(name, settings);
    }

    public SeedServices Services(string database, Dictionary<string, string?> settings)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = database }.ConnectionString;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Ats"] = connectionString })
            .Build();

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication()
            .AddInfrastructure(configuration);
        return new SeedServices(database, services.BuildServiceProvider());
    }
}

public sealed class SeedServices(string database, ServiceProvider provider) : IServiceProvider, IAsyncDisposable
{
    public string Database { get; } = database;

    public object? GetService(Type serviceType) => provider.GetService(serviceType);

    public ValueTask DisposeAsync() => provider.DisposeAsync();
}
