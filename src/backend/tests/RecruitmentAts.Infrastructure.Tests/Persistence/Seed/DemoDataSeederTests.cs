using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Infrastructure.Persistence;

namespace RecruitmentAts.Infrastructure.Tests.Persistence.Seed;

/// <summary>`migrate --demo`: demo users, positions, and applications for trying the app; added once.</summary>
public sealed class DemoDataSeederTests(SeedPostgres postgres) : IClassFixture<SeedPostgres>
{
    private static readonly Dictionary<string, string?> Settings = new()
    {
        ["Seed:AdminEmail"] = "admin@example.test",
        ["Seed:AdminPassword"] = "Admin-Password-1",
        ["Seed:DemoPassword"] = "Demo-Password-1",
        ["Seed:Departments:0"] = "Phòng Nhân sự",
        ["Seed:Departments:1"] = "Phòng Công nghệ thông tin",
    };

    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Demo_data_has_every_role_and_position_status_and_is_added_once()
    {
        await using var services = await postgres.NewDatabaseAsync(Settings);

        await services.MigrateAndSeedAsync(_ct, demo: true);
        await services.MigrateAndSeedAsync(_ct, demo: true);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        var users = await db.Set<User>().ToListAsync(_ct);
        Assert.Equal([Role.Admin, Role.HrManager, Role.Recruiter, Role.Interviewer], users.Select(u => u.Role).Distinct().Order());
        Assert.Equal(6, users.Count);
        var demoUser = users.Single(u => u.Email == "minh.anh@demo.test");
        Assert.False(demoUser.MustChangePassword);
        Assert.True(scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Verify(demoUser.PasswordHash, "Demo-Password-1"));

        var positions = await db.Set<Position>().Include(p => p.Criteria).ToListAsync(_ct);
        Assert.Equal(11, positions.Count);
        Assert.Equal([PositionStatus.Draft, PositionStatus.Open, PositionStatus.Closed], positions.Select(p => p.Status).Distinct().Order());
        Assert.All(positions, p => Assert.NotEmpty(p.Criteria));
        Assert.All(positions.Where(p => p.Status == PositionStatus.Closed), p => Assert.NotNull(p.ClosedAt));
        Assert.True(await db.Set<JobApplication>().AnyAsync(a => a.Stage == Stage.Hired, _ct));
        Assert.False(await db.Set<JobApplication>().AnyAsync(
            a => db.Set<Position>().Any(p => p.Id == a.PositionId && p.Status == PositionStatus.Draft), _ct));
    }

    [Fact]
    public async Task Demo_data_needs_its_password_and_plain_migrate_adds_none()
    {
        await using var services = await postgres.NewDatabaseAsync(new(Settings) { ["Seed:DemoPassword"] = null });

        await services.MigrateAndSeedAsync(_ct);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => services.MigrateAndSeedAsync(_ct, demo: true));

        Assert.Contains("Seed__DemoPassword", error.Message, StringComparison.Ordinal);
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        Assert.Equal(1, await db.Set<User>().CountAsync(_ct));
        Assert.False(await db.Set<Position>().AnyAsync(_ct));
    }
}
