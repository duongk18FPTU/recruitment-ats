using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Screening;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Api.Tests.Positions;

/// <summary>AC-US04–US07 positions, and the lookups their screens use, over HTTP on a real database.</summary>
public sealed class PositionsApiTests(DatabaseApiFactory api) : IClassFixture<DatabaseApiFactory>
{
    private const string Password = "Mat-khau-dung-2026";
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    // ---------------------------------------------------------------- list (AC-US04 #1)

    [Fact]
    public async Task Search_matches_title_or_code_ignoring_case()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var department = await api.CreateDepartmentAsync();
        var backend = await api.CreatePositionAsync(recruiter.Id, department.Id, "Kỹ sư Backend Java");
        await api.CreatePositionAsync(recruiter.Id, department.Id, "Chuyên viên Marketing");

        var byTitle = await ListAsync(recruiter.Token, $"?departmentId={department.Id}&q=BACKEND");
        var byCode = await ListAsync(recruiter.Token, $"?departmentId={department.Id}&q={backend.Code.ToLowerInvariant()}");

        Assert.Equal(["Kỹ sư Backend Java"], Titles(byTitle));
        Assert.Equal(["Kỹ sư Backend Java"], Titles(byCode));
        var item = byTitle.GetProperty("items")[0];
        Assert.Matches("^JOB-\\d{3,}$", item.GetProperty("code").GetString());
        Assert.Equal(department.Name, item.GetProperty("department").GetProperty("name").GetString());
        Assert.Equal(recruiter.Id, item.GetProperty("owner").GetProperty("id").GetGuid());
        Assert.Equal("RECRUITER", item.GetProperty("owner").GetProperty("role").GetString());
        Assert.Equal("OPEN", item.GetProperty("status").GetString());
        Assert.Equal(0, item.GetProperty("applicationCount").GetInt32());
        Assert.Equal(0, item.GetProperty("hiredCount").GetInt32());
    }

    [Fact]
    public async Task Search_treats_wildcards_literally()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var department = await api.CreateDepartmentAsync();
        await api.CreatePositionAsync(recruiter.Id, department.Id, "Thực tập sinh 100% online");
        await api.CreatePositionAsync(recruiter.Id, department.Id, "Thực tập sinh văn phòng");

        var page = await ListAsync(recruiter.Token, $"?departmentId={department.Id}&q=%25");

        Assert.Equal(["Thực tập sinh 100% online"], Titles(page));
    }

    [Fact]
    public async Task Filters_by_status_department_and_owner()
    {
        var hr = await SignInAsync(Role.HrManager);
        var other = await api.CreateUserAsync(Password, Role.Recruiter);
        var department = await api.CreateDepartmentAsync();
        var elsewhere = await api.CreateDepartmentAsync();
        await api.CreatePositionAsync(hr.Id, department.Id, "Đang mở", PositionStatus.Open);
        await api.CreatePositionAsync(hr.Id, department.Id, "Bản nháp", PositionStatus.Draft);
        await api.CreatePositionAsync(other.Id, department.Id, "Của người khác", PositionStatus.Open);
        await api.CreatePositionAsync(hr.Id, elsewhere.Id, "Phòng khác", PositionStatus.Open);

        Assert.Equal(
            ["Của người khác", "Đang mở"],
            Titles(await ListAsync(hr.Token, $"?departmentId={department.Id}&status=OPEN&sort=title")));
        Assert.Equal(["Bản nháp"], Titles(await ListAsync(hr.Token, $"?departmentId={department.Id}&status=DRAFT")));
        Assert.Equal(["Của người khác"], Titles(await ListAsync(hr.Token, $"?departmentId={department.Id}&ownerId={other.Id}")));
    }

    [Fact]
    public async Task No_position_has_a_job_post_yet()
    {
        var admin = await SignInAsync(Role.Admin);
        var department = await api.CreateDepartmentAsync();
        await api.CreatePositionAsync(admin.Id, department.Id);

        var page = await ListAsync(admin.Token, $"?departmentId={department.Id}&jobPostStatus=PUBLISHED");

        Assert.Equal(0, page.GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task Pages_default_to_20_newest_first_and_sort_by_target_date()
    {
        var admin = await SignInAsync(Role.Admin);
        var department = await api.CreateDepartmentAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 1; i <= 22; i++)
        {
            await api.CreatePositionAsync(admin.Id, department.Id, $"Vị trí {i:00}", targetHireDate: today.AddDays(60 - i));
        }

        var first = await ListAsync(admin.Token, $"?departmentId={department.Id}");
        var second = await ListAsync(admin.Token, $"?departmentId={department.Id}&page=2");
        var byDate = await ListAsync(admin.Token, $"?departmentId={department.Id}&sort=targetHireDate&pageSize=3");

        Assert.Equal(20, first.GetProperty("pageSize").GetInt32());
        Assert.Equal(22, first.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, first.GetProperty("totalPages").GetInt32());
        Assert.Equal("Vị trí 22", Titles(first)[0]);
        Assert.Equal(["Vị trí 02", "Vị trí 01"], Titles(second));
        Assert.Equal(["Vị trí 22", "Vị trí 21", "Vị trí 20"], Titles(byDate));
    }

    [Theory]
    [InlineData("?pageSize=101")]
    [InlineData("?page=0")]
    [InlineData("?status=open")]
    [InlineData("?status=1")]
    [InlineData("?sort=code")]
    [InlineData("?departmentId=not-a-guid")]
    public async Task Invalid_query_parameters_are_VALIDATION_FAILED(string query)
    {
        var admin = await SignInAsync(Role.Admin);

        var response = await SendAsync(HttpMethod.Get, "/api/positions" + query, admin.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", await ErrorCodeOf(response));
    }

    [Fact]
    public async Task The_largest_page_is_100()
    {
        var admin = await SignInAsync(Role.Admin);

        var page = await ListAsync(admin.Token, "?pageSize=100");

        Assert.Equal(100, page.GetProperty("pageSize").GetInt32());
    }

    // ---------------------------------------------------------------- create (AC-US05)

    [Theory]
    [InlineData(Role.Recruiter, "OPEN")]
    [InlineData(Role.HrManager, "DRAFT")]
    [InlineData(Role.Admin, null)]
    public async Task A_created_position_is_owned_by_its_creator_at_criteria_version_1(Role role, string? status)
    {
        var creator = await SignInAsync(role);
        var department = await api.CreateDepartmentAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/positions", creator.Token, NewPosition(department.Id, status));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = body.GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/positions/{id}", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Matches("^JOB-\\d{3,}$", body.GetProperty("code").GetString());
        Assert.Equal("Kỹ sư dữ liệu", body.GetProperty("title").GetString());
        Assert.Equal(status ?? "DRAFT", body.GetProperty("status").GetString());
        Assert.Equal(creator.Id, body.GetProperty("owner").GetProperty("id").GetGuid());
        Assert.Equal(1, body.GetProperty("criteriaVersion").GetInt32());
        Assert.Equal(
            ["Python", "Spark"],
            body.GetProperty("criteria").EnumerateArray().Select(c => c.GetProperty("name").GetString()));
        Assert.All(body.GetProperty("criteria").EnumerateArray(), c => Assert.Equal(1, c.GetProperty("version").GetInt32()));
        Assert.True(await AuditedAsync(creator.Id, id, AuditAction.Create));
    }

    [Fact]
    public async Task No_criteria_a_bad_weight_a_past_date_or_no_headcount_are_400_with_field_errors()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var department = await api.CreateDepartmentAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));

        var noCriteria = await ErrorsOf(await SendAsync(HttpMethod.Post, "/api/positions", recruiter.Token,
            NewPosition(department.Id) with { criteria = [] }));
        var badWeight = await ErrorsOf(await SendAsync(HttpMethod.Post, "/api/positions", recruiter.Token,
            NewPosition(department.Id) with { criteria = [new { name = "Python", type = "MUST_HAVE", weight = 6 }] }));
        var pastDate = await ErrorsOf(await SendAsync(HttpMethod.Post, "/api/positions", recruiter.Token,
            NewPosition(department.Id) with { targetHireDate = today.ToString("yyyy-MM-dd") }));
        var noHeadcount = await ErrorsOf(await SendAsync(HttpMethod.Post, "/api/positions", recruiter.Token,
            NewPosition(department.Id) with { headcount = 0 }));

        Assert.Equal(["criteria"], noCriteria);
        Assert.Equal(["criteria[0].weight"], badWeight);
        Assert.Equal(["targetHireDate"], pastDate);
        Assert.Equal(["headcount"], noHeadcount);
    }

    [Fact]
    public async Task Missing_fields_and_an_inactive_department_are_field_errors()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var inactive = await api.CreateDepartmentAsync(isActive: false);

        var missing = await ErrorsOf(await SendAsync(HttpMethod.Post, "/api/positions", recruiter.Token, new
        {
            title = "A",
            location = "",
            description = "",
            criteria = new[] { new { name = "P" } },
        }));
        var closedStatus = await ErrorsOf(await SendAsync(HttpMethod.Post, "/api/positions", recruiter.Token,
            NewPosition(inactive.Id, "CLOSED")));
        var inactiveDepartment = await ErrorsOf(await SendAsync(HttpMethod.Post, "/api/positions", recruiter.Token,
            NewPosition(inactive.Id)));

        Assert.Equal(
            ["criteria[0].name", "criteria[0].type", "criteria[0].weight", "departmentId", "description", "employmentType",
                "headcount", "level", "location", "targetHireDate", "title"],
            missing);
        Assert.Equal(["status"], closedStatus);
        Assert.Equal(["departmentId"], inactiveDepartment);
    }

    [Fact]
    public async Task An_Interviewer_cannot_create_a_position()
    {
        var interviewer = await SignInAsync(Role.Interviewer);
        var department = await api.CreateDepartmentAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/positions", interviewer.Token, NewPosition(department.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN", await ErrorCodeOf(response));
    }

    // ---------------------------------------------------------------- edit (AC-US06)

    [Fact]
    public async Task The_owner_edits_fields_and_unchanged_criteria_keep_their_version()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var department = await api.CreateDepartmentAsync();
        var position = await api.CreatePositionAsync(recruiter.Id, department.Id);
        var before = await DetailAsync(recruiter.Token, position.Id);

        var response = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", recruiter.Token,
            EditOf(before) with { title = "Lập trình viên Backend (.NET)", headcount = 3 });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Lập trình viên Backend (.NET)", body.GetProperty("title").GetString());
        Assert.Equal(3, body.GetProperty("headcount").GetInt32());
        Assert.Equal(1, body.GetProperty("criteriaVersion").GetInt32());
        Assert.Equal(
            before.GetProperty("criteria")[0].GetProperty("id").GetGuid(),
            body.GetProperty("criteria")[0].GetProperty("id").GetGuid());
        Assert.NotEqual(before.GetProperty("rowVersion").GetString(), body.GetProperty("rowVersion").GetString());
        Assert.True(await AuditedAsync(recruiter.Id, position.Id, AuditAction.Update));
    }

    [Fact]
    public async Task A_Recruiter_who_does_not_own_the_position_gets_403()
    {
        var owner = await api.CreateUserAsync(Password, Role.Recruiter);
        var other = await SignInAsync(Role.Recruiter);
        var department = await api.CreateDepartmentAsync();
        var position = await api.CreatePositionAsync(owner.Id, department.Id);
        var detail = await DetailAsync(other.Token, position.Id);

        var response = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", other.Token, EditOf(detail));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("FORBIDDEN", await ErrorCodeOf(response));
    }

    [Fact]
    public async Task Changed_criteria_make_a_new_version_and_outdate_completed_analyses()
    {
        var hr = await SignInAsync(Role.HrManager);
        var owner = await api.CreateUserAsync(Password, Role.Recruiter);
        var department = await api.CreateDepartmentAsync();
        var position = await api.CreatePositionAsync(owner.Id, department.Id);
        var completed = await api.CreateAnalysisAsync(position.Id, 1, AnalysisStatus.Completed);
        var failed = await api.CreateAnalysisAsync(position.Id, 1, AnalysisStatus.Failed);
        var otherPosition = await api.CreatePositionAsync(owner.Id, department.Id);
        var elsewhere = await api.CreateAnalysisAsync(otherPosition.Id, 1, AnalysisStatus.Completed);
        var before = await DetailAsync(hr.Token, position.Id);
        var kept = before.GetProperty("criteria")[0].GetProperty("id").GetGuid();

        var response = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", hr.Token, EditOf(before) with
        {
            criteria =
            [
                new { id = kept, name = "C# và .NET", type = "MUST_HAVE", weight = 4 },
                new { name = "PostgreSQL", type = "NICE_TO_HAVE", weight = 2 },
            ],
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body.GetProperty("criteriaVersion").GetInt32());
        Assert.Equal(
            [("C# và .NET", 4, 2), ("PostgreSQL", 2, 2)],
            body.GetProperty("criteria").EnumerateArray().Select(c =>
                (c.GetProperty("name").GetString(), c.GetProperty("weight").GetInt32(), c.GetProperty("version").GetInt32())));
        Assert.Equal(AnalysisStatus.Outdated, await StatusOfAsync(completed.Id));
        Assert.Equal(AnalysisStatus.Failed, await StatusOfAsync(failed.Id));
        Assert.Equal(AnalysisStatus.Completed, await StatusOfAsync(elsewhere.Id));
        // Version 1 rows stay for the analyses that reference them.
        Assert.Equal(3, await api.QueryAsync(db => db.Set<PositionCriterion>().CountAsync(c => c.PositionId == position.Id, _ct)));
    }

    [Fact]
    public async Task A_stale_row_version_is_409_CONCURRENCY_CONFLICT()
    {
        var admin = await SignInAsync(Role.Admin);
        var department = await api.CreateDepartmentAsync();
        var position = await api.CreatePositionAsync(admin.Id, department.Id);
        var first = await DetailAsync(admin.Token, position.Id);
        var saved = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", admin.Token, EditOf(first) with { headcount = 4 });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var stale = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", admin.Token, EditOf(first) with { headcount = 5 });

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("CONCURRENCY_CONFLICT", await ErrorCodeOf(stale));
    }

    [Fact]
    public async Task Only_Admin_and_HR_Manager_reassign_the_owner_to_an_active_hiring_team_member()
    {
        var hr = await SignInAsync(Role.HrManager);
        var recruiter = await SignInAsync(Role.Recruiter);
        var colleague = await api.CreateUserAsync(Password, Role.Recruiter, fullName: "Lý Minh Tâm");
        var interviewer = await api.CreateUserAsync(Password, Role.Interviewer);
        var department = await api.CreateDepartmentAsync();
        var position = await api.CreatePositionAsync(recruiter.Id, department.Id);

        var byOwner = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", recruiter.Token,
            EditOf(await DetailAsync(recruiter.Token, position.Id)) with { ownerId = colleague.Id });
        var toInterviewer = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", hr.Token,
            EditOf(await DetailAsync(hr.Token, position.Id)) with { ownerId = interviewer.Id });
        var byHr = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", hr.Token,
            EditOf(await DetailAsync(hr.Token, position.Id)) with { ownerId = colleague.Id });

        Assert.Equal(HttpStatusCode.Forbidden, byOwner.StatusCode);
        Assert.Equal(["ownerId"], await ErrorsOf(toInterviewer));
        Assert.Equal(HttpStatusCode.OK, byHr.StatusCode);
        Assert.Equal("Lý Minh Tâm", (await byHr.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("owner").GetProperty("fullName").GetString());
    }

    [Fact]
    public async Task Edit_rejects_a_criterion_id_from_another_version_and_unknown_positions()
    {
        var admin = await SignInAsync(Role.Admin);
        var interviewer = await SignInAsync(Role.Interviewer);
        var department = await api.CreateDepartmentAsync();
        var position = await api.CreatePositionAsync(admin.Id, department.Id);
        var detail = await DetailAsync(admin.Token, position.Id);

        var foreignId = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", admin.Token, EditOf(detail) with
        {
            criteria = [new { id = Guid.NewGuid(), name = "C# và .NET", type = "MUST_HAVE", weight = 5 }],
        });
        var missing = await SendAsync(HttpMethod.Put, $"/api/positions/{Guid.NewGuid()}", admin.Token, EditOf(detail));
        var noVersion = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", admin.Token, EditOf(detail) with { rowVersion = "" });
        var byInterviewer = await SendAsync(HttpMethod.Put, $"/api/positions/{position.Id}", interviewer.Token, EditOf(detail));

        Assert.Equal(["criteria[0].id"], await ErrorsOf(foreignId));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(["rowVersion"], await ErrorsOf(noVersion));
        Assert.Equal(HttpStatusCode.Forbidden, byInterviewer.StatusCode);
    }

    // ---------------------------------------------------------------- detail

    [Fact]
    public async Task Detail_has_the_current_criteria_and_a_row_version()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var department = await api.CreateDepartmentAsync();
        var position = await api.CreatePositionAsync(recruiter.Id, department.Id);

        var response = await SendAsync(HttpMethod.Get, $"/api/positions/{position.Id}", recruiter.Token);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(position.Code, body.GetProperty("code").GetString());
        Assert.Equal("FULL_TIME", body.GetProperty("employmentType").GetString());
        Assert.Equal("SENIOR", body.GetProperty("level").GetString());
        Assert.Equal(1, body.GetProperty("criteriaVersion").GetInt32());
        var criterion = Assert.Single(body.GetProperty("criteria").EnumerateArray());
        Assert.Equal("MUST_HAVE", criterion.GetProperty("type").GetString());
        Assert.Equal(5, criterion.GetProperty("weight").GetInt32());
        Assert.NotEqual("0", body.GetProperty("rowVersion").GetString());
        Assert.Empty(body.GetProperty("stageCounts").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("closedAt").ValueKind);
    }

    [Fact]
    public async Task An_unknown_position_is_404_NOT_FOUND()
    {
        var admin = await SignInAsync(Role.Admin);

        var response = await SendAsync(HttpMethod.Get, $"/api/positions/{Guid.NewGuid()}", admin.Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("NOT_FOUND", await ErrorCodeOf(response));
    }

    // ---------------------------------------------------------------- Interviewer (AC-US04 #2 waits for panels, US14)

    [Fact]
    public async Task An_Interviewer_without_panels_sees_no_positions_and_cannot_open_one()
    {
        var interviewer = await SignInAsync(Role.Interviewer);
        var department = await api.CreateDepartmentAsync();
        var owner = await api.CreateUserAsync(Password, Role.Recruiter);
        var position = await api.CreatePositionAsync(owner.Id, department.Id);

        var page = await ListAsync(interviewer.Token, $"?departmentId={department.Id}");
        var detail = await SendAsync(HttpMethod.Get, $"/api/positions/{position.Id}", interviewer.Token);

        Assert.Equal(0, page.GetProperty("totalItems").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact]
    public async Task Anonymous_callers_get_401()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/positions", accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------- lookups

    [Fact]
    public async Task Departments_lists_active_ones_for_every_role()
    {
        var interviewer = await SignInAsync(Role.Interviewer);
        var active = await api.CreateDepartmentAsync();
        var inactive = await api.CreateDepartmentAsync(isActive: false);

        var response = await SendAsync(HttpMethod.Get, "/api/lookups/departments", interviewer.Token);
        var names = (await response.Content.ReadFromJsonAsync<JsonElement>(_ct)).EnumerateArray()
            .Select(d => d.GetProperty("name").GetString()).ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(active.Name, names);
        Assert.DoesNotContain(inactive.Name, names);
    }

    [Fact]
    public async Task User_options_filter_by_roles_and_name_and_skip_inactive_users()
    {
        var hr = await SignInAsync(Role.HrManager);
        var marker = Guid.NewGuid().ToString("N");
        await api.CreateUserAsync(Password, Role.Recruiter, fullName: $"Tuyển dụng {marker}");
        await api.CreateUserAsync(Password, Role.HrManager, fullName: $"Nhân sự {marker}");
        await api.CreateUserAsync(Password, Role.Interviewer, fullName: $"Phỏng vấn {marker}");
        await api.CreateUserAsync(Password, Role.Recruiter, isActive: false, fullName: $"Đã khoá {marker}");

        var response = await SendAsync(HttpMethod.Get, $"/api/lookups/users?roles=RECRUITER,HR_MANAGER&q={marker}", hr.Token);
        var names = (await response.Content.ReadFromJsonAsync<JsonElement>(_ct)).EnumerateArray()
            .Select(u => u.GetProperty("fullName").GetString()).ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([$"Nhân sự {marker}", $"Tuyển dụng {marker}"], names);
    }

    [Fact]
    public async Task User_options_reject_an_unknown_role_and_Interviewers()
    {
        var admin = await SignInAsync(Role.Admin);
        var interviewer = await SignInAsync(Role.Interviewer);

        var badRole = await SendAsync(HttpMethod.Get, "/api/lookups/users?roles=RECRUITER,BOSS", admin.Token);
        var forbidden = await SendAsync(HttpMethod.Get, "/api/lookups/users", interviewer.Token);

        Assert.Equal(HttpStatusCode.BadRequest, badRole.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    // ---------------------------------------------------------------- helpers

    private sealed record SignedIn(Guid Id, string Token);

    private async Task<SignedIn> SignInAsync(Role role)
    {
        var user = await api.CreateUserAsync(Password, role);
        var login = await SendAsync(HttpMethod.Post, "/api/auth/login", accessToken: null, new { email = user.Email, password = Password });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("accessToken").GetString()!;
        return new SignedIn(user.Id, token);
    }

    private async Task<JsonElement> ListAsync(string token, string query)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/positions" + query, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(_ct);
    }

    private sealed record PositionBody(
        string title,
        Guid departmentId,
        string location,
        string employmentType,
        string level,
        int headcount,
        string targetHireDate,
        string description,
        object[] criteria,
        string? status);

    private static PositionBody NewPosition(Guid departmentId, string? status = null) => new(
        "  Kỹ sư dữ liệu  ",
        departmentId,
        "TP. HCM",
        "FULL_TIME",
        "MIDDLE",
        2,
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)).ToString("yyyy-MM-dd"),
        "Xây dựng pipeline dữ liệu.",
        [
            new { name = "Python", type = "MUST_HAVE", weight = 5 },
            new { name = "Spark", description = "Đã chạy job trên cụm", type = "NICE_TO_HAVE", weight = 2 },
        ],
        status);

    private sealed record EditBody(
        string title,
        Guid departmentId,
        string location,
        string employmentType,
        string level,
        int headcount,
        string targetHireDate,
        string description,
        object[] criteria,
        string rowVersion,
        Guid? ownerId = null);

    /// <summary>The body that saves <paramref name="detail"/> unchanged.</summary>
    private static EditBody EditOf(JsonElement detail) => new(
        detail.GetProperty("title").GetString()!,
        detail.GetProperty("department").GetProperty("id").GetGuid(),
        detail.GetProperty("location").GetString()!,
        detail.GetProperty("employmentType").GetString()!,
        detail.GetProperty("level").GetString()!,
        detail.GetProperty("headcount").GetInt32(),
        detail.GetProperty("targetHireDate").GetString()!,
        detail.GetProperty("description").GetString()!,
        [.. detail.GetProperty("criteria").EnumerateArray().Select(c => (object)new
        {
            id = c.GetProperty("id").GetGuid(),
            name = c.GetProperty("name").GetString(),
            description = c.GetProperty("description").GetString(),
            type = c.GetProperty("type").GetString(),
            weight = c.GetProperty("weight").GetInt32(),
        })],
        detail.GetProperty("rowVersion").GetString()!);

    private async Task<JsonElement> DetailAsync(string token, Guid positionId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/positions/{positionId}", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(_ct);
    }

    private Task<AnalysisStatus> StatusOfAsync(Guid analysisId) =>
        api.QueryAsync(db => db.Set<CvAnalysis>().Where(a => a.Id == analysisId).Select(a => a.Status).SingleAsync(_ct));

    private async Task<List<string>> ErrorsOf(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);
        return [.. problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Order(StringComparer.Ordinal)];
    }

    private Task<bool> AuditedAsync(Guid actorId, Guid positionId, AuditAction action) =>
        api.QueryAsync(db => db.Set<AuditLog>().AnyAsync(
            a => a.ActorId == actorId && a.EntityId == positionId && a.Module == AuditModule.Position && a.Action == action, _ct));

    private static List<string?> Titles(JsonElement page) =>
        [.. page.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("title").GetString())];

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? accessToken, object? body = null)
    {
        using var client = api.CreateClient(new() { HandleCookies = false });
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        return await client.SendAsync(request, _ct);
    }

    private async Task<string?> ErrorCodeOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("errorCode").GetString();
}
