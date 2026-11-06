using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Lookups;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Screening;
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

    public async Task<Department> CreateDepartmentAsync(string? name = null, bool isActive = true)
    {
        var department = new Department { Name = name ?? $"Phòng {Guid.NewGuid():N}", IsActive = isActive };
        await QueryAsync(db =>
        {
            db.Add(department);
            return db.SaveChangesAsync();
        });
        return department;
    }

    /// <summary>A position with one must-have criterion (version 1), saved as the given status.</summary>
    public async Task<Position> CreatePositionAsync(
        Guid ownerId,
        Guid departmentId,
        string title = "Lập trình viên Backend .NET",
        PositionStatus status = PositionStatus.Open,
        DateOnly? targetHireDate = null,
        int headcount = 2)
    {
        var position = new Position
        {
            Title = title,
            DepartmentId = departmentId,
            Location = "Hà Nội",
            EmploymentType = EmploymentType.FullTime,
            Level = PositionLevel.Senior,
            Headcount = headcount,
            TargetHireDate = targetHireDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Description = "Phát triển API tuyển dụng.",
            Status = status,
            OwnerId = ownerId,
            CreatedBy = ownerId,
        };
        position.Criteria.Add(new PositionCriterion { Version = 1, Name = "C# và .NET", Type = CriterionType.MustHave, Weight = 5 });
        await QueryAsync(db =>
        {
            db.Add(position);
            return db.SaveChangesAsync();
        });
        return position;
    }

    /// <summary>An application to the position with one CV analysis made with <paramref name="criteriaVersion"/>.</summary>
    public async Task<CvAnalysis> CreateAnalysisAsync(Guid positionId, int criteriaVersion, AnalysisStatus status)
    {
        var candidate = new Candidate { FullName = "Trần Thị Lan", Email = $"{Guid.NewGuid():N}@example.test" };
        var cv = new CvFile
        {
            CandidateId = candidate.Id,
            StorageKey = $"cv/{Guid.NewGuid():N}.pdf",
            OriginalName = "cv-tran-thi-lan.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Sha256 = new string('a', 64),
        };
        var application = new JobApplication
        {
            PositionId = positionId,
            CandidateId = candidate.Id,
            Source = Source.Linkedin,
            IntakeChannel = IntakeChannel.Manual,
            CurrentCvFileId = cv.Id,
        };
        var analysis = new CvAnalysis
        {
            ApplicationId = application.Id,
            CvFileId = cv.Id,
            CriteriaVersion = criteriaVersion,
            Status = status,
        };
        await QueryAsync(db =>
        {
            db.AddRange(candidate, cv, application, analysis);
            return db.SaveChangesAsync();
        });
        return analysis;
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
