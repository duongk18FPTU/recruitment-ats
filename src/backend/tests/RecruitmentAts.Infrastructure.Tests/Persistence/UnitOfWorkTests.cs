using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Lookups;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RecruitmentAts.Infrastructure.Tests.Persistence;

public sealed class UnitOfWorkTests(MigratedDatabase database) : IClassFixture<MigratedDatabase>
{
    private static readonly DateTimeOffset T1 = new(2026, 11, 4, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = T1.AddHours(1);

    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Saves_the_changes_and_the_audit_entry_in_one_commit()
    {
        database.Clock.UtcNow = T1;
        var user = NewUser();

        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
            var audit = new AuditWriter(scope.ServiceProvider.GetRequiredService<IAuditLogRepository>(), database.Clock);

            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync(() =>
            {
                db.Add(user);
                audit.Write(Actor.User(user.Id, Role.Admin, IPAddress.Loopback, "trace-commit"),
                    AuditModule.User, AuditAction.Create, "User", user.Id, "Created user");
                return Task.CompletedTask;
            }, _ct);
        }

        await using var read = database.Services.CreateAsyncScope();
        var readDb = read.ServiceProvider.GetRequiredService<AtsDbContext>();
        var saved = await readDb.Set<User>().SingleAsync(u => u.Id == user.Id, _ct);
        var entry = await readDb.Set<AuditLog>().SingleAsync(a => a.TraceId == "trace-commit", _ct);

        Assert.Equal(T1, saved.CreatedAt);
        Assert.Equal(T1, saved.UpdatedAt);
        Assert.Equal(user.Id, entry.ActorId);
        Assert.Equal(T1, entry.OccurredAt);
        Assert.Equal(IPAddress.Loopback, entry.IpAddress);
    }

    [Fact]
    public async Task Saves_nothing_when_the_work_throws()
    {
        var name = "Rollback " + Guid.NewGuid();

        await using (var scope = database.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteAsync(async () =>
            {
                db.Add(new Department { Name = name });
                await db.SaveChangesAsync(_ct); // written inside the transaction, then rolled back
                throw new InvalidOperationException("boom");
            }, _ct));
        }

        await using var read = database.Services.CreateAsyncScope();
        Assert.False(await read.ServiceProvider.GetRequiredService<AtsDbContext>()
            .Set<Department>().AnyAsync(d => d.Name == name, _ct));
    }

    [Fact]
    public async Task Update_sets_UpdatedAt_and_keeps_CreatedAt()
    {
        database.Clock.UtcNow = T1;
        var user = NewUser();
        await InScopeAsync((db, unitOfWork) => unitOfWork.ExecuteAsync(() =>
        {
            db.Add(user);
            return Task.CompletedTask;
        }, _ct));

        database.Clock.UtcNow = T2;
        await InScopeAsync((db, unitOfWork) => unitOfWork.ExecuteAsync(async () =>
        {
            var loaded = await db.Set<User>().SingleAsync(u => u.Id == user.Id, _ct);
            loaded.FullName = "Renamed";
        }, _ct));

        await using var read = database.Services.CreateAsyncScope();
        var saved = await read.ServiceProvider.GetRequiredService<AtsDbContext>()
            .Set<User>().SingleAsync(u => u.Id == user.Id, _ct);
        Assert.Equal(T1, saved.CreatedAt);
        Assert.Equal(T2, saved.UpdatedAt);
    }

    [Fact]
    public async Task A_row_changed_by_someone_else_becomes_CONCURRENCY_CONFLICT()
    {
        var user = NewUser();
        await InScopeAsync((db, unitOfWork) => unitOfWork.ExecuteAsync(() =>
        {
            db.Add(user);
            return Task.CompletedTask;
        }, _ct));

        await using var scope = database.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        var loaded = await db.Set<User>().SingleAsync(u => u.Id == user.Id, _ct);

        await InScopeAsync((other, _) => other.Set<User>().Where(u => u.Id == user.Id).ExecuteDeleteAsync(_ct));

        var error = await Assert.ThrowsAsync<AppException>(() =>
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync(() =>
            {
                loaded.FullName = "Stale";
                return Task.CompletedTask;
            }, _ct));
        Assert.Equal(ErrorCode.ConcurrencyConflict, error.Code);
    }

    private async Task InScopeAsync(Func<AtsDbContext, IUnitOfWork, Task> action)
    {
        await using var scope = database.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<AtsDbContext>(), scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
    }

    private static User NewUser() => new()
    {
        Email = $"{Guid.NewGuid():N}@example.test",
        PasswordHash = "not-a-real-hash",
        FullName = "Test User",
        Role = Role.Recruiter,
    };
}

/// <summary>A migrated database and the Infrastructure services wired to it, with a settable clock.</summary>
public sealed class MigratedDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16").Build();
    private ServiceProvider? _services;

    public SettableClock Clock { get; } = new();

    public IServiceProvider Services => _services ?? throw new InvalidOperationException("Not initialized.");

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Ats"] = _postgres.GetConnectionString() })
            .Build();
        var services = new ServiceCollection().AddInfrastructure(configuration);
        services.AddSingleton<IClock>(Clock);
        _services = services.BuildServiceProvider();

        await using var scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AtsDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    public sealed class SettableClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }
}
