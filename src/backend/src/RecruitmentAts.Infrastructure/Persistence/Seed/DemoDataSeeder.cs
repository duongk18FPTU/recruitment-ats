using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Security;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Common.Audit;
using RecruitmentAts.Application.Lookups;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Infrastructure.Persistence.Seed;

/// <summary>
/// Demo data for trying the built stories by hand (`api migrate --demo`, never in Production): one user
/// per role, positions in every status with weighted criteria, and applications in several stages so the
/// counts on SCR-10 / SCR-12 are not all zero. Runs once: skipped when the demo HR Manager exists.
/// </summary>
/// <remarks>Setting: <c>Seed__DemoPassword</c>, the password of every demo account (not committed).</remarks>
internal sealed partial class DemoDataSeeder(
    AtsDbContext db,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    AuditWriter audit,
    IClock clock,
    IConfiguration configuration,
    ILogger<DemoDataSeeder> logger)
{
    public const string HrManagerEmail = "thu.ha@demo.test";

    private static readonly (string Email, string FullName, Role Role)[] DemoUsers =
    [
        (HrManagerEmail, "Trần Thu Hà", Role.HrManager),
        ("minh.anh@demo.test", "Nguyễn Minh Anh", Role.Recruiter),
        ("minh.tam@demo.test", "Lý Minh Tâm", Role.Recruiter),
        ("van.khoa@demo.test", "Lê Văn Khoa", Role.Interviewer),
        ("thanh.binh@demo.test", "Phạm Thanh Bình", Role.Interviewer),
    ];

    public Task SeedAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            if (await db.Set<User>().AnyAsync(u => u.Email == HrManagerEmail, cancellationToken))
            {
                LogSkipped(logger);
                return;
            }

            var password = configuration["Seed:DemoPassword"];
            if (password is null || password.Length is < 10 or > 128)
            {
                throw new InvalidOperationException("Demo data: set Seed__DemoPassword (10–128 characters).");
            }

            var departments = await db.Set<Department>().Where(d => d.IsActive).ToDictionaryAsync(d => d.Name, d => d.Id, cancellationToken);
            var users = SeedUsers(password);
            var positions = SeedPositions(users, departments);
            var applications = SeedApplications(positions);

            audit.Write(Actor.System("seed-demo"), AuditModule.User, AuditAction.Create, null, null,
                "Tạo dữ liệu demo", new { users = users.Count, positions = positions.Count, applications });
            LogSeeded(logger, users.Count, positions.Count, applications);
        }, cancellationToken);

    private Dictionary<string, User> SeedUsers(string password)
    {
        var hash = passwordHasher.Hash(password);
        var users = DemoUsers.ToDictionary(
            u => u.FullName,
            u => new User { Email = u.Email, PasswordHash = hash, FullName = u.FullName, Role = u.Role });
        db.AddRange(users.Values);
        return users;
    }

    private List<Position> SeedPositions(Dictionary<string, User> users, Dictionary<string, Guid> departments)
    {
        var today = BusinessCalendar.Today(clock);
        Guid Department(string name) =>
            departments.TryGetValue(name, out var id) ? id : departments.Values.First();

        var specs = new[]
        {
            new PositionSpec("Backend Developer (.NET)", "Phòng Công nghệ thông tin", "Hà Nội", PositionLevel.Senior, 2, 6, PositionStatus.Open, "Nguyễn Minh Anh",
                [("≥ 3 năm kinh nghiệm C# / .NET", CriterionType.MustHave, 5), ("Thành thạo SQL (PostgreSQL / SQL Server)", CriterionType.MustHave, 4),
                 ("Thiết kế và xây dựng REST API", CriterionType.MustHave, 4), ("Kinh nghiệm Docker / CI-CD", CriterionType.NiceToHave, 2),
                 ("Đọc hiểu tài liệu tiếng Anh", CriterionType.NiceToHave, 2)]),
            new PositionSpec("UI/UX Designer", "Phòng Công nghệ thông tin", "Hà Nội", PositionLevel.Middle, 1, 25, PositionStatus.Open, "Nguyễn Minh Anh",
                [("Portfolio sản phẩm số", CriterionType.MustHave, 5), ("Thành thạo Figma", CriterionType.MustHave, 4), ("Nghiên cứu người dùng", CriterionType.NiceToHave, 3)]),
            new PositionSpec("Kế toán tổng hợp", "Phòng Tài chính – Kế toán", "TP. HCM", PositionLevel.Middle, 1, 10, PositionStatus.Open, "Lý Minh Tâm",
                [("Chứng chỉ kế toán", CriterionType.MustHave, 5), ("Lập báo cáo tài chính", CriterionType.MustHave, 4), ("Sử dụng MISA", CriterionType.NiceToHave, 2)]),
            new PositionSpec("Chuyên viên Marketing", "Phòng Marketing", "Hà Nội", PositionLevel.Junior, 1, 30, PositionStatus.Open, "Lý Minh Tâm",
                [("Chạy quảng cáo Facebook / Google", CriterionType.MustHave, 4), ("Viết nội dung", CriterionType.NiceToHave, 3)]),
            new PositionSpec("QA Engineer (Automation)", "Phòng Công nghệ thông tin", "Đà Nẵng", PositionLevel.Middle, 1, 40, PositionStatus.Open, "Trần Thu Hà",
                [("Viết test tự động (Playwright / Selenium)", CriterionType.MustHave, 5), ("Hiểu quy trình CI", CriterionType.NiceToHave, 3)]),
            new PositionSpec("Thực tập sinh Data", "Phòng Công nghệ thông tin", "Hà Nội", PositionLevel.Intern, 3, 45, PositionStatus.Open, "Nguyễn Minh Anh",
                [("SQL cơ bản", CriterionType.MustHave, 4), ("Python", CriterionType.NiceToHave, 3)], EmploymentType.Internship),
            new PositionSpec("Trưởng nhóm Kinh doanh", "Phòng Kinh doanh", "TP. HCM", PositionLevel.Lead, 1, 50, PositionStatus.Open, "Lý Minh Tâm",
                [("≥ 5 năm bán hàng B2B", CriterionType.MustHave, 5), ("Quản lý nhóm", CriterionType.MustHave, 4), ("Tiếng Anh giao tiếp", CriterionType.NiceToHave, 2)]),
            new PositionSpec("Chuyên viên Tuyển dụng", "Phòng Nhân sự", "Hà Nội", PositionLevel.Junior, 1, 35, PositionStatus.Draft, "Nguyễn Minh Anh",
                [("Kinh nghiệm tuyển dụng IT", CriterionType.MustHave, 4), ("Sử dụng LinkedIn Recruiter", CriterionType.NiceToHave, 2)]),
            new PositionSpec("Chuyên viên Pháp chế", "Ban Giám đốc", "Hà Nội", PositionLevel.Senior, 1, 60, PositionStatus.Draft, "Trần Thu Hà",
                [("Cử nhân Luật", CriterionType.MustHave, 5), ("Soạn thảo hợp đồng", CriterionType.MustHave, 4)]),
            new PositionSpec("Nhân viên Hành chính", "Phòng Vận hành", "Hà Nội", PositionLevel.Fresher, 1, 20, PositionStatus.Closed, "Lý Minh Tâm",
                [("Tin học văn phòng", CriterionType.MustHave, 3), ("Giao tiếp tốt", CriterionType.NiceToHave, 2)], EmploymentType.FullTime),
            new PositionSpec("Nhân viên Kinh doanh (bán thời gian)", "Phòng Kinh doanh", "TP. HCM", PositionLevel.Junior, 2, 15, PositionStatus.Closed, "Nguyễn Minh Anh",
                [("Kỹ năng bán hàng", CriterionType.MustHave, 4)], EmploymentType.PartTime),
        };

        var positions = new List<Position>();
        foreach (var spec in specs)
        {
            var owner = users[spec.Owner];
            var position = new Position
            {
                Title = spec.Title,
                DepartmentId = Department(spec.Department),
                Location = spec.Location,
                EmploymentType = spec.EmploymentType,
                Level = spec.Level,
                Headcount = spec.Headcount,
                TargetHireDate = today.AddDays(spec.DaysToTarget),
                Description = $"{spec.Title} tại {spec.Location}.\nPhối hợp với các nhóm liên quan để hoàn thành mục tiêu của phòng.",
                Status = spec.Status,
                OwnerId = owner.Id,
                CreatedBy = owner.Id,
                ClosedAt = spec.Status == PositionStatus.Closed ? clock.UtcNow.AddDays(-3) : null,
            };
            position.Criteria.AddRange(spec.Criteria.Select((c, i) => new PositionCriterion
            {
                PositionId = position.Id,
                Version = 1,
                Name = c.Name,
                Type = c.Type,
                Weight = c.Weight,
                SortOrder = i + 1,
            }));
            positions.Add(position);
        }

        db.AddRange(positions);
        return positions;
    }

    /// <summary>Candidates in several stages on the open and closed positions, for the counts.</summary>
    private int SeedApplications(List<Position> positions)
    {
        string[] names =
        [
            "Trần Thị Lan", "Lê Hoàng Nam", "Đỗ Minh Khôi", "Hồ Thanh Tùng", "Mai Anh Tuấn", "Bùi Gia Bảo", "Vũ Đức Thành",
            "Trịnh Quang Vinh", "Phan Ngọc Ánh", "Đặng Thu Trang", "Nguyễn Hải Đăng", "Lý Thu Thảo", "Võ Minh Quân", "Hoàng Yến Nhi",
        ];
        Stage[] stages = [Stage.New, Stage.New, Stage.Screening, Stage.Screening, Stage.Interview, Stage.Offer, Stage.Hired, Stage.Rejected];
        Source[] sources = [Source.Linkedin, Source.Topcv, Source.CareerSite, Source.Vietnamworks, Source.Referral];
        var plan = positions.Where(p => p.Status != PositionStatus.Draft).Select((p, i) => (Position: p, Count: 8 - (i % 5))).ToList();

        var count = 0;
        var candidateIndex = 0;
        foreach (var (position, applicationCount) in plan)
        {
            for (var i = 0; i < applicationCount; i++)
            {
                var name = names[candidateIndex % names.Length];
                var candidate = new Candidate
                {
                    FullName = name,
                    Email = $"ung.vien.{++candidateIndex}@demo.test",
                    LastActivityAt = clock.UtcNow,
                };
                var stage = stages[(i + count) % stages.Length];
                db.Add(candidate);
                db.Add(new JobApplication
                {
                    PositionId = position.Id,
                    CandidateId = candidate.Id,
                    Stage = stage,
                    Source = sources[i % sources.Length],
                    IntakeChannel = IntakeChannel.Manual,
                    RejectionReason = stage == Stage.Rejected ? RejectionReason.SkillsMismatch : null,
                    AppliedAt = clock.UtcNow.AddDays(-i - 1),
                    StageChangedAt = clock.UtcNow.AddDays(-i),
                    ClosedAt = stage is Stage.Hired or Stage.Rejected ? clock.UtcNow.AddDays(-i) : null,
                });
                count++;
            }
        }

        return count;
    }

    private sealed record PositionSpec(
        string Title,
        string Department,
        string Location,
        PositionLevel Level,
        int Headcount,
        int DaysToTarget,
        PositionStatus Status,
        string Owner,
        (string Name, CriterionType Type, int Weight)[] Criteria,
        EmploymentType EmploymentType = EmploymentType.FullTime);

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo seed: skipped, the demo data already exists")]
    private static partial void LogSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Demo seed: {Users} users, {Positions} positions, {Applications} applications")]
    private static partial void LogSeeded(ILogger logger, int users, int positions, int applications);
}
