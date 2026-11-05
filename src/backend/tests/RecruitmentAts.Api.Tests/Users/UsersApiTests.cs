using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Api.Tests.Users;

/// <summary>AC-US20 Manage users, over HTTP on a real database.</summary>
public sealed class UsersApiTests(DatabaseApiFactory api) : IClassFixture<DatabaseApiFactory>
{
    private const string Password = "Mat-khau-dung-2026";
    private const string TemporaryPassword = "Mat-khau-tam-2026";
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    // ---------------------------------------------------------------- create (AC-US20 #1, #2, #5)

    [Fact]
    public async Task A_created_user_can_log_in_and_must_replace_the_temporary_password()
    {
        var admin = await SignInAsAdminAsync();
        var email = Unique("an.le");

        var created = await SendAsync(HttpMethod.Post, "/api/users", admin.Token, new
        {
            email = $"  {email}  ",
            fullName = "Lê Thị An",
            phone = "0901234567",
            role = "RECRUITER",
            temporaryPassword = TemporaryPassword,
        });
        var body = await created.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = body.GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/users/{id}", created.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("RECRUITER", body.GetProperty("role").GetString());
        Assert.True(body.GetProperty("isActive").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("lastLoginAt").ValueKind);
        Assert.False(body.TryGetProperty("passwordHash", out _));
        Assert.True(await AuditedAsync(admin.Id, id, AuditAction.Create));

        // AC-US20 #1 and #5: the new account logs in, and only the password change is open.
        var login = await LoginAsync(email, TemporaryPassword);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>(_ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True(loginBody.GetProperty("user").GetProperty("mustChangePassword").GetBoolean());
        var blocked = await SendAsync(HttpMethod.Get, "/test/protected", loginBody.GetProperty("accessToken").GetString()!);
        Assert.Equal("PASSWORD_CHANGE_REQUIRED", await ErrorCodeOf(blocked));
    }

    [Fact]
    public async Task A_duplicate_email_is_409_DUPLICATE_EMAIL_ignoring_case()
    {
        var admin = await SignInAsAdminAsync();
        var existing = await api.CreateUserAsync(Password);

        var response = await SendAsync(HttpMethod.Post, "/api/users", admin.Token, new
        {
            email = existing.Email.ToUpperInvariant(),
            fullName = "Người Trùng Email",
            role = "INTERVIEWER",
            temporaryPassword = TemporaryPassword,
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("DUPLICATE_EMAIL", await ErrorCodeOf(response));
    }

    [Fact]
    public async Task Invalid_fields_and_a_missing_role_are_VALIDATION_FAILED()
    {
        var admin = await SignInAsAdminAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/users", admin.Token, new
        {
            email = "khong-phai-email",
            fullName = "A",
            temporaryPassword = "chi-co-chu-khong-so",
        });
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            ["email", "fullName", "role", "temporaryPassword"],
            problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Order());
    }

    // ---------------------------------------------------------------- update

    [Fact]
    public async Task Edit_changes_the_fields_and_rejects_another_users_email()
    {
        var admin = await SignInAsAdminAsync();
        var user = await api.CreateUserAsync(Password);
        var other = await api.CreateUserAsync(Password);
        var email = Unique("doi.email");

        var updated = await SendAsync(HttpMethod.Put, $"/api/users/{user.Id}", admin.Token, new
        {
            email,
            fullName = "Trần Đức Long",
            phone = "",
            role = "HR_MANAGER",
        });
        var body = await updated.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("HR_MANAGER", body.GetProperty("role").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("phone").ValueKind);
        Assert.True(await AuditedAsync(admin.Id, user.Id, AuditAction.Update));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(email, Password)).StatusCode); // password unchanged

        var sameEmailAgain = await SendAsync(HttpMethod.Put, $"/api/users/{user.Id}", admin.Token, new
        {
            email = email.ToUpperInvariant(), fullName = "Trần Đức Long", role = "HR_MANAGER",
        });
        Assert.Equal(HttpStatusCode.OK, sameEmailAgain.StatusCode);

        var taken = await SendAsync(HttpMethod.Put, $"/api/users/{user.Id}", admin.Token, new
        {
            email = other.Email, fullName = "Trần Đức Long", role = "HR_MANAGER",
        });
        Assert.Equal("DUPLICATE_EMAIL", await ErrorCodeOf(taken));
    }

    [Fact]
    public async Task A_new_temporary_password_replaces_the_old_one_ends_sessions_and_unlocks()
    {
        var admin = await SignInAsAdminAsync();
        var user = await api.CreateUserAsync(Password);
        var cookie = RefreshTokenOf(await LoginAsync(user.Email, Password));
        await api.QueryAsync(db => db.Set<User>().Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.LockedUntil, DateTimeOffset.UtcNow.AddMinutes(10)), _ct));

        var response = await SendAsync(HttpMethod.Put, $"/api/users/{user.Id}", admin.Token, new
        {
            email = user.Email, fullName = user.FullName, role = "RECRUITER", temporaryPassword = TemporaryPassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(cookie)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(user.Email, Password)).StatusCode);
        var login = await LoginAsync(user.Email, TemporaryPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True((await login.Content.ReadFromJsonAsync<JsonElement>(_ct))
            .GetProperty("user").GetProperty("mustChangePassword").GetBoolean());
    }

    // ---------------------------------------------------------------- status (AC-US20 #3, #4)

    [Fact]
    public async Task Deactivation_blocks_login_and_refresh_and_activation_restores_login()
    {
        var admin = await SignInAsAdminAsync();
        var user = await api.CreateUserAsync(Password);
        var cookie = RefreshTokenOf(await LoginAsync(user.Email, Password));

        var deactivated = await SendAsync(HttpMethod.Put, $"/api/users/{user.Id}/status", admin.Token, new { isActive = false });

        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        Assert.False((await deactivated.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("isActive").GetBoolean());
        var login = await LoginAsync(user.Email, Password);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", await ErrorCodeOf(login));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(cookie)).StatusCode);
        Assert.True(await AuditedAsync(admin.Id, user.Id, AuditAction.StatusChange));

        var activated = await SendAsync(HttpMethod.Put, $"/api/users/{user.Id}/status", admin.Token, new { isActive = true });
        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user.Email, Password)).StatusCode);
    }

    [Fact]
    public async Task An_Admin_cannot_deactivate_their_own_account()
    {
        var admin = await SignInAsAdminAsync();

        var response = await SendAsync(HttpMethod.Put, $"/api/users/{admin.Id}/status", admin.Token, new { isActive = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("SELF_DEACTIVATION", await ErrorCodeOf(response));
        Assert.True(await api.QueryAsync(db => db.Set<User>().Where(u => u.Id == admin.Id).Select(u => u.IsActive).SingleAsync(_ct)));
    }

    [Fact]
    public async Task A_missing_status_is_VALIDATION_FAILED_not_a_deactivation()
    {
        var admin = await SignInAsAdminAsync();
        var user = await api.CreateUserAsync(Password);

        var response = await SendAsync(HttpMethod.Put, $"/api/users/{user.Id}/status", admin.Token, new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await api.QueryAsync(db => db.Set<User>().Where(u => u.Id == user.Id).Select(u => u.IsActive).SingleAsync(_ct)));
    }

    // ---------------------------------------------------------------- list and detail

    [Fact]
    public async Task The_list_filters_by_text_role_and_status_and_pages()
    {
        var admin = await SignInAsAdminAsync();
        var tag = Guid.NewGuid().ToString("N")[..8];
        await api.CreateUserAsync(Password, Role.Interviewer, fullName: $"Phỏng Vấn {tag} A");
        await api.CreateUserAsync(Password, Role.Interviewer, fullName: $"Phỏng Vấn {tag} B");
        await api.CreateUserAsync(Password, Role.Interviewer, fullName: $"Phỏng Vấn {tag} C", isActive: false);
        await api.CreateUserAsync(Password, Role.Recruiter, fullName: $"Tuyển Dụng {tag}");

        var page = await ListAsync(admin.Token, $"?q={tag.ToUpperInvariant()}&role=INTERVIEWER&isActive=true&pageSize=1&page=2");

        Assert.Equal(2, page.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, page.GetProperty("totalPages").GetInt32());
        Assert.Equal(2, page.GetProperty("page").GetInt32());
        Assert.Equal(1, page.GetProperty("pageSize").GetInt32());
        Assert.Equal($"Phỏng Vấn {tag} B", Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("fullName").GetString());

        var byEmailFragment = await ListAsync(admin.Token, "?q=%25"); // a literal "%" matches nobody
        Assert.Equal(0, byEmailFragment.GetProperty("totalItems").GetInt32());
    }

    [Theory]
    [InlineData("?role=hr_manager")]
    [InlineData("?role=1")]
    [InlineData("?pageSize=101")]
    [InlineData("?page=0")]
    public async Task Invalid_list_parameters_are_VALIDATION_FAILED(string query)
    {
        var admin = await SignInAsAdminAsync();

        var response = await SendAsync(HttpMethod.Get, "/api/users" + query, admin.Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", await ErrorCodeOf(response));
    }

    [Fact]
    public async Task Detail_returns_the_user_and_an_unknown_id_is_404()
    {
        var admin = await SignInAsAdminAsync();
        var user = await api.CreateUserAsync(Password, Role.HrManager);

        var found = await SendAsync(HttpMethod.Get, $"/api/users/{user.Id}", admin.Token);
        var missing = await SendAsync(HttpMethod.Get, $"/api/users/{Guid.NewGuid()}", admin.Token);

        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal("HR_MANAGER", (await found.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("role").GetString());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("NOT_FOUND", await ErrorCodeOf(missing));
    }

    // ---------------------------------------------------------------- authorization (x-roles: ADMIN)

    [Theory]
    [InlineData(Role.HrManager)]
    [InlineData(Role.Recruiter)]
    [InlineData(Role.Interviewer)]
    public async Task Other_roles_get_403_FORBIDDEN(Role role)
    {
        var user = await api.CreateUserAsync(Password, role);
        var token = await AccessTokenOf(await LoginAsync(user.Email, Password));

        var list = await SendAsync(HttpMethod.Get, "/api/users", token);
        var deactivate = await SendAsync(HttpMethod.Put, $"/api/users/{user.Id}/status", token, new { isActive = false });

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal("FORBIDDEN", await ErrorCodeOf(list));
        Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
    }

    [Fact]
    public async Task Anonymous_callers_get_401()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/users", accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------- helpers

    private sealed record SignedIn(Guid Id, string Token);

    private static string Unique(string name) => $"{name}.{Guid.NewGuid():N}@example.test";

    private async Task<SignedIn> SignInAsAdminAsync()
    {
        var admin = await api.CreateUserAsync(Password, Role.Admin);
        return new SignedIn(admin.Id, await AccessTokenOf(await LoginAsync(admin.Email, Password)));
    }

    private async Task<JsonElement> ListAsync(string token, string query)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/users" + query, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(_ct);
    }

    private Task<bool> AuditedAsync(Guid actorId, Guid userId, AuditAction action) =>
        api.QueryAsync(db => db.Set<AuditLog>().AnyAsync(
            a => a.ActorId == actorId && a.EntityId == userId && a.Module == AuditModule.User && a.Action == action, _ct));

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

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        SendAsync(HttpMethod.Post, "/api/auth/login", accessToken: null, new { email, password });

    private async Task<HttpResponseMessage> RefreshAsync(string cookie)
    {
        using var client = api.CreateClient(new() { HandleCookies = false });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", $"{RefreshCookie.Name}={cookie}");
        return await client.SendAsync(request, _ct);
    }

    private async Task<string> AccessTokenOf(HttpResponseMessage login) =>
        (await login.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("accessToken").GetString()!;

    private async Task<string?> ErrorCodeOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("errorCode").GetString();

    private static string RefreshTokenOf(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(RefreshCookie.Name + "=", StringComparison.Ordinal));
        return cookie[(RefreshCookie.Name.Length + 1)..cookie.IndexOf(';', StringComparison.Ordinal)];
    }
}
