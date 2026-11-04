using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Lookups;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Seed;

/// <summary>
/// Seed data of docs/07-database-design.md, safe to run on every deployment:
/// the first Admin (only while no Admin exists) and the departments that are missing.
/// </summary>
/// <remarks>
/// Settings: <c>Seed__AdminEmail</c> and <c>Seed__AdminPassword</c> (secrets, needed only for the
/// first run), <c>Seed__AdminFullName</c>, and <c>Seed:Departments</c> (appsettings.json).
/// </remarks>
internal sealed partial class DatabaseSeeder(
    AtsDbContext db,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    AuditWriter audit,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    private const int PasswordMinLength = 10; // same as UserCreateRequest.temporaryPassword
    private const int PasswordMaxLength = 128;

    public Task SeedAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var actor = Actor.System("seed");
            await SeedAdminAsync(actor, cancellationToken);
            await SeedDepartmentsAsync(cancellationToken);
        }, cancellationToken);

    private async Task SeedAdminAsync(Actor actor, CancellationToken cancellationToken)
    {
        if (await db.Set<User>().AnyAsync(u => u.Role == Role.Admin, cancellationToken))
        {
            return;
        }

        var email = configuration["Seed:AdminEmail"]?.Trim();
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrEmpty(email) || !email.Contains('@', StringComparison.Ordinal) || email.Length > 254)
        {
            throw new InvalidOperationException("No Admin exists: set Seed__AdminEmail to the first Admin's email address.");
        }

        if (password is null || password.Length is < PasswordMinLength or > PasswordMaxLength)
        {
            throw new InvalidOperationException(
                $"No Admin exists: set Seed__AdminPassword ({PasswordMinLength}–{PasswordMaxLength} characters).");
        }

        var admin = new User
        {
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            FullName = configuration["Seed:AdminFullName"] is { Length: > 0 } name ? name : "Quản trị viên",
            Role = Role.Admin,
        };
        db.Add(admin);
        audit.Write(actor, AuditModule.User, AuditAction.Create, "User", admin.Id, "Tạo tài khoản Admin đầu tiên",
            new { admin.Email, admin.Role });
        LogAdminCreated(logger);
    }

    private async Task SeedDepartmentsAsync(CancellationToken cancellationToken)
    {
        var names = configuration.GetSection("Seed:Departments").GetChildren()
            .Select(section => section.Value?.Trim())
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var existing = await db.Set<Department>().Select(d => d.Name).ToListAsync(cancellationToken);
        var added = 0;
        for (var i = 0; i < names.Count; i++)
        {
            if (!existing.Contains(names[i], StringComparer.Ordinal))
            {
                db.Add(new Department { Name = names[i], SortOrder = i + 1 });
                added++;
            }
        }

        if (added > 0)
        {
            LogDepartmentsAdded(logger, added);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seed: created the first Admin")]
    private static partial void LogAdminCreated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seed: added {Count} departments")]
    private static partial void LogDepartmentsAdded(ILogger logger, int count);
}
