using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Screening;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Api.Tests.Applications;

/// <summary>AC-US09 add an application with a CV, and the candidate match behind it, over HTTP on a real database.</summary>
public sealed class ApplicationsApiTests(DatabaseApiFactory api) : IClassFixture<DatabaseApiFactory>
{
    private const string Password = "Mat-khau-dung-2026";
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    // ---------------------------------------------------------------- AC-US09 #1

    [Fact]
    public async Task Adds_an_application_in_NEW_with_history_a_pending_analysis_a_stored_file_and_an_audit_entry()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var position = await OpenPositionAsync(recruiter.Id);
        var email = NewEmail();

        var response = await AddAsync(recruiter.Token, position.Id, email: email, phone: "0912 345 678");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);
        Assert.False(body.GetProperty("candidateMatched").GetBoolean());
        Assert.False(body.GetProperty("receiptEmailQueued").GetBoolean()); // US27
        var application = body.GetProperty("application");
        var id = application.GetProperty("id").GetGuid();
        Assert.Equal($"/api/applications/{id}", response.Headers.Location?.ToString());
        Assert.Equal("NEW", application.GetProperty("stage").GetString());
        Assert.Equal("LINKEDIN", application.GetProperty("source").GetString());
        Assert.Equal("MANUAL", application.GetProperty("intakeChannel").GetString());
        Assert.Equal("PENDING", application.GetProperty("analysisStatus").GetString());
        Assert.Equal(["SCREENING", "REJECTED"], Strings(application.GetProperty("allowedStages")));
        Assert.Equal(position.Code, application.GetProperty("position").GetProperty("code").GetString());
        Assert.Equal("Nguyễn Văn An", application.GetProperty("candidate").GetProperty("fullName").GetString());
        var cv = Assert.Single(application.GetProperty("cvFiles").EnumerateArray());
        Assert.True(cv.GetProperty("isCurrent").GetBoolean());
        Assert.Equal("cv-nguyen-van-an.pdf", cv.GetProperty("originalName").GetString());
        Assert.Equal(application.GetProperty("currentCvFileId").GetGuid(), cv.GetProperty("id").GetGuid());
        Assert.False(string.IsNullOrEmpty(application.GetProperty("rowVersion").GetString()));

        var stored = await api.QueryAsync(async db =>
        {
            var row = await db.Set<JobApplication>().AsNoTracking().SingleAsync(a => a.Id == id, _ct);
            var file = await db.Set<CvFile>().AsNoTracking().SingleAsync(f => f.Id == row.CurrentCvFileId, _ct);
            var history = await db.Set<StageHistoryEntry>().AsNoTracking().Where(h => h.ApplicationId == id).ToListAsync(_ct);
            var analysis = await db.Set<CvAnalysis>().AsNoTracking().Where(x => x.ApplicationId == id).ToListAsync(_ct);
            var candidate = await db.Set<Candidate>().AsNoTracking().SingleAsync(c => c.Id == row.CandidateId, _ct);
            var audited = await db.Set<AuditLog>().AnyAsync(
                a => a.ActorId == recruiter.Id && a.EntityId == id && a.Module == AuditModule.Application && a.Action == AuditAction.Create, _ct);
            return (row, file, history, analysis, candidate, audited);
        });
        Assert.Equal(Stage.New, stored.row.Stage);
        Assert.Equal(recruiter.Id, stored.row.CreatedBy);
        var entry = Assert.Single(stored.history);
        Assert.Null(entry.FromStage);
        Assert.Equal(Stage.New, entry.ToStage);
        Assert.Equal(recruiter.Id, entry.ActorId);
        var analysis = Assert.Single(stored.analysis);
        Assert.Equal(AnalysisStatus.Pending, analysis.Status);
        Assert.Equal(stored.file.Id, analysis.CvFileId);
        Assert.Equal(position.CriteriaVersion, analysis.CriteriaVersion);
        Assert.Equal(email, stored.candidate.EmailNormalized);
        Assert.Equal("0912345678", stored.candidate.PhoneNormalized);
        Assert.Contains("Backend Developer", stored.file.ExtractedText, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(api.FileRoot, stored.file.StorageKey)));
        Assert.True(stored.audited);
    }

    // ---------------------------------------------------------------- AC-US09 #2

    [Fact]
    public async Task Reuses_the_candidate_with_the_same_normalized_email()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var first = await OpenPositionAsync(recruiter.Id);
        var second = await OpenPositionAsync(recruiter.Id);
        var email = NewEmail();
        var created = await Json(await AddAsync(recruiter.Token, first.Id, email: email));

        var again = await Json(await AddAsync(recruiter.Token, second.Id, email: "  " + email.ToUpperInvariant() + " ", fullName: "Tên khác"));

        Assert.True(again.GetProperty("candidateMatched").GetBoolean());
        Assert.Equal(
            created.GetProperty("application").GetProperty("candidate").GetProperty("id").GetGuid(),
            again.GetProperty("application").GetProperty("candidate").GetProperty("id").GetGuid());
        Assert.Equal("Nguyễn Văn An", again.GetProperty("application").GetProperty("candidate").GetProperty("fullName").GetString());
        Assert.Equal(1, await api.QueryAsync(db => db.Set<Candidate>().CountAsync(c => c.EmailNormalized == email, _ct)));
    }

    [Theory]
    [InlineData("0912 345 678", "+84 912.345.678")]
    [InlineData("+84912345678", "0912345678")]
    public async Task Reuses_the_candidate_with_the_same_normalized_phone(string firstPhone, string secondPhone)
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var first = await OpenPositionAsync(recruiter.Id);
        var second = await OpenPositionAsync(recruiter.Id);
        var phone = RandomPhone();
        var created = await Json(await AddAsync(recruiter.Token, first.Id, email: null, phone: Spaced(firstPhone, phone)));

        var again = await Json(await AddAsync(recruiter.Token, second.Id, email: null, phone: Spaced(secondPhone, phone)));

        Assert.True(again.GetProperty("candidateMatched").GetBoolean());
        Assert.Equal(
            created.GetProperty("application").GetProperty("candidate").GetProperty("id").GetGuid(),
            again.GetProperty("application").GetProperty("candidate").GetProperty("id").GetGuid());
    }

    // ---------------------------------------------------------------- AC-US09 #3

    [Fact]
    public async Task A_second_application_to_the_same_position_is_409_with_the_existing_application()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var position = await OpenPositionAsync(recruiter.Id);
        var email = NewEmail();
        var created = await Json(await AddAsync(recruiter.Token, position.Id, email: email));
        var files = FileCount();

        var response = await AddAsync(recruiter.Token, position.Id, email: email);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);
        Assert.Equal("DUPLICATE_APPLICATION", problem.GetProperty("errorCode").GetString());
        Assert.Equal(
            created.GetProperty("application").GetProperty("id").GetGuid(),
            problem.GetProperty("existingApplicationId").GetGuid());
        Assert.Equal(files, FileCount());
    }

    // ---------------------------------------------------------------- AC-US09 #4 (nothing is stored)

    [Fact]
    public async Task A_wrong_file_type_is_400_and_nothing_is_stored()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var position = await OpenPositionAsync(recruiter.Id);
        var email = NewEmail();
        var files = FileCount();

        var byType = await AddAsync(recruiter.Token, position.Id, email: email, contentType: "image/png", fileName: "cv.png");
        var byContent = await AddAsync(recruiter.Token, position.Id, email: email, file: "just text"u8.ToArray());

        foreach (var response in new[] { byType, byContent })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("UNSUPPORTED_FILE_TYPE", await ErrorCodeOf(response));
        }

        await AssertNothingStoredAsync(position.Id, email, files);
    }

    [Fact]
    public async Task A_file_over_10_MB_is_413()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var position = await OpenPositionAsync(recruiter.Id);
        var email = NewEmail();
        var files = FileCount();
        var big = new byte[10 * 1024 * 1024 + 10];
        "%PDF-1.7\n"u8.CopyTo(big);

        var response = await AddAsync(recruiter.Token, position.Id, email: email, file: big);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("FILE_TOO_LARGE", await ErrorCodeOf(response));
        await AssertNothingStoredAsync(position.Id, email, files);
    }

    [Fact]
    public async Task A_pdf_without_text_is_400_and_nothing_is_stored()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var position = await OpenPositionAsync(recruiter.Id);
        var email = NewEmail();
        var files = FileCount();

        var response = await AddAsync(recruiter.Token, position.Id, email: email, file: SampleCv.Pdf(text: null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("NO_EXTRACTABLE_TEXT", await ErrorCodeOf(response));
        await AssertNothingStoredAsync(position.Id, email, files);
    }

    [Fact]
    public async Task A_missing_file_is_a_field_error()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var position = await OpenPositionAsync(recruiter.Id);

        var response = await AddAsync(recruiter.Token, position.Id, email: NewEmail(), file: []);

        Assert.Equal(["cv"], await ErrorsOf(response));
    }

    // ---------------------------------------------------------------- AC-US09 #5 (BR-18)

    [Fact]
    public async Task Source_is_required_and_must_be_in_the_list()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var position = await OpenPositionAsync(recruiter.Id);

        var missing = await AddAsync(recruiter.Token, position.Id, email: NewEmail(), source: null);
        var unknown = await AddAsync(recruiter.Token, position.Id, email: NewEmail(), source: "TWITTER");

        Assert.Equal(["source"], await ErrorsOf(missing));
        Assert.Equal(["source"], await ErrorsOf(unknown));
    }

    [Fact]
    public async Task OTHER_needs_a_note_and_other_sources_drop_it()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var position = await OpenPositionAsync(recruiter.Id);

        var withoutNote = await AddAsync(recruiter.Token, position.Id, email: NewEmail(), source: "OTHER");
        var withNote = await Json(await AddAsync(recruiter.Token, position.Id, email: NewEmail(), source: "OTHER", sourceNote: " Hội chợ việc làm "));
        var ignored = await Json(await AddAsync(recruiter.Token, position.Id, email: NewEmail(), source: "TOPCV", sourceNote: "bỏ qua"));

        Assert.Equal(["sourceNote"], await ErrorsOf(withoutNote));
        Assert.Equal("Hội chợ việc làm", withNote.GetProperty("application").GetProperty("sourceNote").GetString());
        Assert.Equal(JsonValueKind.Null, ignored.GetProperty("application").GetProperty("sourceNote").ValueKind);
    }

    [Fact]
    public async Task Candidate_fields_are_validated()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var position = await OpenPositionAsync(recruiter.Id);

        var noName = await AddAsync(recruiter.Token, position.Id, email: NewEmail(), fullName: " ");
        var badEmail = await AddAsync(recruiter.Token, position.Id, email: "khong-phai-email");
        var noContact = await AddAsync(recruiter.Token, position.Id, email: null, phone: null);
        var noPosition = await AddAsync(recruiter.Token, null, email: NewEmail());

        Assert.Equal(["fullName"], await ErrorsOf(noName));
        Assert.Equal(["email"], await ErrorsOf(badEmail));
        Assert.Equal(["email"], await ErrorsOf(noContact));
        Assert.Equal(["positionId"], await ErrorsOf(noPosition));
    }

    // ---------------------------------------------------------------- AC-US07 #1 and the position

    [Theory]
    [InlineData(PositionStatus.Draft)]
    [InlineData(PositionStatus.Closed)]
    public async Task A_position_that_is_not_open_is_409(PositionStatus status)
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var department = await api.CreateDepartmentAsync();
        var position = await api.CreatePositionAsync(recruiter.Id, department.Id, status: status);
        var email = NewEmail();
        var files = FileCount();

        var response = await AddAsync(recruiter.Token, position.Id, email: email);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("POSITION_NOT_OPEN", await ErrorCodeOf(response));
        await AssertNothingStoredAsync(position.Id, email, files);
    }

    [Fact]
    public async Task An_unknown_position_is_404()
    {
        var recruiter = await SignInAsync(Role.Recruiter);

        var response = await AddAsync(recruiter.Token, Guid.NewGuid(), email: NewEmail());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.HrManager)]
    public async Task Admin_and_HR_Manager_may_add_applications_and_decide_on_offers(Role role)
    {
        var user = await SignInAsync(role);
        var position = await OpenPositionAsync(user.Id);

        var created = await Json(await AddAsync(user.Token, position.Id, email: NewEmail()));

        Assert.Equal(["SCREENING", "REJECTED"], Strings(created.GetProperty("application").GetProperty("allowedStages")));
    }

    [Fact]
    public async Task An_Interviewer_and_an_anonymous_caller_may_not_add_applications()
    {
        var interviewer = await SignInAsync(Role.Interviewer);
        var position = await OpenPositionAsync(interviewer.Id);

        var forbidden = await AddAsync(interviewer.Token, position.Id, email: NewEmail());
        var anonymous = await AddAsync(null, position.Id, email: NewEmail());

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    // ---------------------------------------------------------------- matchCandidate (BR-06)

    [Fact]
    public async Task Match_finds_a_candidate_by_email_then_phone_and_tells_whether_they_applied()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var applied = await OpenPositionAsync(recruiter.Id);
        var other = await OpenPositionAsync(recruiter.Id);
        var email = NewEmail();
        var phone = RandomPhone();
        var created = await Json(await AddAsync(recruiter.Token, applied.Id, email: email, phone: phone));
        var applicationId = created.GetProperty("application").GetProperty("id").GetGuid();

        var byEmail = await MatchAsync(recruiter.Token, $"email={Uri.EscapeDataString(email.ToUpperInvariant())}&positionId={applied.Id}");
        var byPhone = await MatchAsync(recruiter.Token, $"phone={Uri.EscapeDataString("+84" + phone[1..])}&positionId={other.Id}");
        var conflict = await MatchAsync(recruiter.Token, $"email={Uri.EscapeDataString(email)}&phone=0999000111");

        Assert.True(byEmail.GetProperty("matched").GetBoolean());
        Assert.Equal("EMAIL", byEmail.GetProperty("matchedBy").GetString());
        Assert.Equal("Nguyễn Văn An", byEmail.GetProperty("candidate").GetProperty("fullName").GetString());
        Assert.True(byEmail.GetProperty("alreadyApplied").GetBoolean());
        Assert.Equal(applicationId, byEmail.GetProperty("existingApplicationId").GetGuid());
        Assert.Equal("PHONE", byPhone.GetProperty("matchedBy").GetString());
        Assert.False(byPhone.GetProperty("alreadyApplied").GetBoolean());
        Assert.Equal("EMAIL", conflict.GetProperty("matchedBy").GetString());
    }

    [Fact]
    public async Task Match_with_an_unknown_contact_finds_nobody()
    {
        var recruiter = await SignInAsync(Role.Recruiter);

        var result = await MatchAsync(recruiter.Token, $"email={Uri.EscapeDataString(NewEmail())}");

        Assert.False(result.GetProperty("matched").GetBoolean());
        Assert.False(result.GetProperty("alreadyApplied").GetBoolean());
    }

    [Fact]
    public async Task Match_needs_an_email_or_a_phone_and_a_hiring_role()
    {
        var recruiter = await SignInAsync(Role.Recruiter);
        var interviewer = await SignInAsync(Role.Interviewer);

        var empty = await SendAsync(HttpMethod.Get, "/api/candidates/match", recruiter.Token);
        var forbidden = await SendAsync(HttpMethod.Get, "/api/candidates/match?email=a@example.test", interviewer.Token);
        var anonymous = await SendAsync(HttpMethod.Get, "/api/candidates/match?email=a@example.test", null);

        Assert.Equal(["email"], await ErrorsOf(empty));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    // ---------------------------------------------------------------- helpers

    private sealed record SignedIn(Guid Id, string Token);

    private async Task<SignedIn> SignInAsync(Role role)
    {
        var user = await api.CreateUserAsync(Password, role);
        var login = await SendAsync(HttpMethod.Post, "/api/auth/login", null, new { email = user.Email, password = Password });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("accessToken").GetString()!;
        return new SignedIn(user.Id, token);
    }

    private async Task<Position> OpenPositionAsync(Guid ownerId)
    {
        var department = await api.CreateDepartmentAsync();
        return await api.CreatePositionAsync(ownerId, department.Id);
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.test";

    /// <summary>A national number no other test uses: 09 followed by 8 random digits.</summary>
    private static string RandomPhone() => "09" + Random.Shared.NextInt64(10_000_000, 99_999_999).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Writes <paramref name="national"/> the way <paramref name="style"/> shows it (dots, spaces, +84).</summary>
    private static string Spaced(string style, string national) =>
        style.StartsWith("+84", StringComparison.Ordinal) ? "+84 " + national[1..4] + "." + national[4..] : national[..4] + " " + national[4..];

    private int FileCount() =>
        Directory.Exists(api.FileRoot) ? Directory.GetFiles(api.FileRoot, "*", SearchOption.AllDirectories).Length : 0;

    private async Task AssertNothingStoredAsync(Guid positionId, string email, int filesBefore)
    {
        Assert.Equal(filesBefore, FileCount());
        Assert.Equal(0, await api.QueryAsync(db => db.Set<JobApplication>().CountAsync(a => a.PositionId == positionId, _ct)));
        Assert.Equal(0, await api.QueryAsync(db => db.Set<Candidate>().CountAsync(c => c.EmailNormalized == email, _ct)));
    }

    private async Task<HttpResponseMessage> AddAsync(
        string? token,
        Guid? positionId,
        string? email,
        string? phone = null,
        string? fullName = "Nguyễn Văn An",
        string? source = "LINKEDIN",
        string? sourceNote = null,
        byte[]? file = null,
        string contentType = SampleCv.PdfType,
        string fileName = "cv-nguyen-van-an.pdf")
    {
        using var form = new MultipartFormDataContent();
        void Field(string name, string? value)
        {
            if (value is not null)
            {
                form.Add(new StringContent(value), name);
            }
        }

        Field("positionId", positionId?.ToString());
        Field("fullName", fullName);
        Field("email", email);
        Field("phone", phone);
        Field("source", source);
        Field("sourceNote", sourceNote);
        var bytes = file ?? SampleCv.Pdf();
        if (bytes.Length > 0)
        {
            var part = new ByteArrayContent(bytes);
            part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(part, "cv", fileName);
        }

        using var client = api.CreateClient(new() { HandleCookies = false });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/applications") { Content = form };
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await client.SendAsync(request, _ct);
    }

    private async Task<JsonElement> Json(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(_ct);
    }

    private async Task<JsonElement> MatchAsync(string token, string query)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/candidates/match?" + query, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(_ct);
    }

    private async Task<List<string>> ErrorsOf(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);
        return [.. problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Order(StringComparer.Ordinal)];
    }

    private async Task<string?> ErrorCodeOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("errorCode").GetString();

    private static List<string?> Strings(JsonElement array) => [.. array.EnumerateArray().Select(e => e.GetString())];

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
}
