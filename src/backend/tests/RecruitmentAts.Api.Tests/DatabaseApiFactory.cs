using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Users;
using RecruitmentAts.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace RecruitmentAts.Api.Tests;

/// <summary>The API on a migrated PostgreSQL, plus <see cref="ProtectedTestController"/> (an ordinary signed-in endpoint).</summary>
public sealed class DatabaseApiFactory : TestApiFactory, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AtsDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("ConnectionStrings:Ats", _postgres.GetConnectionString());
        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ProtectedTestController).Assembly));
    }

    public async Task<User> CreateUserAsync(
        string password,
        Role role = Role.Recruiter,
        bool mustChangePassword = false,
        bool isActive = true,
        string? fullName = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var user = new User
        {
            Email = $"{Guid.NewGuid():N}@example.test",
            PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Hash(password),
            FullName = fullName ?? "Nguyễn Minh Anh",
            Role = role,
            MustChangePassword = mustChangePassword,
            IsActive = isActive,
        };
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        db.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<T> QueryAsync<T>(Func<AtsDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<AtsDbContext>());
    }
}

/// <summary>Stands for any internal operation: signed-in users only, no exemption from the password change.</summary>
[ApiController]
[Route("test/protected")]
public sealed class ProtectedTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { ok = true });
}

/// <summary>Anonymous endpoint used to check that [AllowAnonymous] still works for everyone.</summary>
[ApiController]
[AllowAnonymous]
[Route("test/anonymous")]
public sealed class AnonymousTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
