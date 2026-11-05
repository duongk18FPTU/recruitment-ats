using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Api.Auth;
using RecruitmentAts.Application.Audit;
using RecruitmentAts.Application.Auth;
using RecruitmentAts.Application.Users;

namespace RecruitmentAts.Api.Tests.Auth;

/// <summary>AC-US01 and the first-login password change, over HTTP on a real database.</summary>
public sealed class AuthApiTests(DatabaseApiFactory api) : IClassFixture<DatabaseApiFactory>
{
    private const string Password = "Mat-khau-dung-2026";
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    // ---------------------------------------------------------------- login (AC-US01 #1, #2)

    [Fact]
    public async Task Valid_credentials_return_a_token_a_safe_profile_and_the_refresh_cookie()
    {
        var user = await api.CreateUserAsync(Password, Role.HrManager);

        var response = await LoginAsync(user.Email.ToUpperInvariant(), Password); // email is case-insensitive
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        var expiresIn = body.GetProperty("expiresAt").GetDateTimeOffset() - DateTimeOffset.UtcNow;
        Assert.InRange(expiresIn, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
        var profile = body.GetProperty("user");
        Assert.Equal(user.Id, profile.GetProperty("id").GetGuid());
        Assert.Equal("HR_MANAGER", profile.GetProperty("role").GetString());
        Assert.False(profile.GetProperty("mustChangePassword").GetBoolean());
        Assert.False(profile.TryGetProperty("passwordHash", out _));
        Assert.DoesNotContain("pbkdf2", profile.GetRawText(), StringComparison.Ordinal);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith(RefreshCookie.Name + "=", cookie, StringComparison.Ordinal);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);

        var saved = await api.QueryAsync(db => db.Set<User>().SingleAsync(u => u.Id == user.Id, _ct));
        Assert.NotNull(saved.LastLoginAt);
        Assert.True(await api.QueryAsync(db =>
            db.Set<AuditLog>().AnyAsync(a => a.ActorId == user.Id && a.Action == AuditAction.Login, _ct)));
    }

    [Fact]
    public async Task Wrong_password_unknown_email_and_deactivated_account_get_the_same_401()
    {
        var active = await api.CreateUserAsync(Password);
        var deactivated = await api.CreateUserAsync(Password, isActive: false);

        var problems = new List<JsonElement>();
        foreach (var (email, password) in new[]
                 {
                     (active.Email, "Sai-mat-khau-1"),
                     ("khong-ton-tai@example.test", Password),
                     (deactivated.Email, Password),
                 })
        {
            var response = await LoginAsync(email, password);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"));
            problems.Add(await response.Content.ReadFromJsonAsync<JsonElement>(_ct));
        }

        Assert.All(problems, p => Assert.Equal("INVALID_CREDENTIALS", p.GetProperty("errorCode").GetString()));
        Assert.Single(problems.Select(p => p.GetProperty("detail").GetString()).Distinct());
        Assert.True(await api.QueryAsync(db =>
            db.Set<AuditLog>().AnyAsync(a => a.ActorId == deactivated.Id && a.Action == AuditAction.LoginFailed, _ct)));
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_with_the_same_message()
    {
        var user = await api.CreateUserAsync(Password);
        for (var i = 0; i < User.MaxFailedLogins; i++)
        {
            await LoginAsync(user.Email, "Sai-mat-khau-1");
        }

        var response = await LoginAsync(user.Email, Password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var saved = await api.QueryAsync(db => db.Set<User>().SingleAsync(u => u.Id == user.Id, _ct));
        Assert.NotNull(saved.LockedUntil);
    }

    [Fact]
    public async Task Empty_fields_are_VALIDATION_FAILED()
    {
        var response = await LoginAsync("", "");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", problem.GetProperty("errorCode").GetString());
        Assert.Equal(["email", "password"], problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Order());
    }

    [Fact]
    public async Task More_than_10_logins_per_minute_from_one_IP_are_RATE_LIMITED()
    {
        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            last = await LoginAsync("rate@example.test", "x", ip: "203.0.113.9");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.True(last.Headers.Contains("Retry-After"));
        var problem = await last.Content.ReadFromJsonAsync<JsonElement>(_ct);
        Assert.Equal("RATE_LIMITED", problem.GetProperty("errorCode").GetString());
    }

    // ---------------------------------------------------------------- refresh (AD-06)

    [Fact]
    public async Task Refresh_rotates_the_cookie_and_a_reused_cookie_ends_the_session()
    {
        var user = await api.CreateUserAsync(Password);
        var first = RefreshTokenOf(await LoginAsync(user.Email, Password));

        var refreshed = await RefreshAsync(first);
        var second = RefreshTokenOf(refreshed);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        Assert.NotEqual(first, second);

        var reuse = await RefreshAsync(first);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Contains(reuse.Headers.GetValues("Set-Cookie"), c => c.StartsWith(RefreshCookie.Name + "=;", StringComparison.Ordinal));

        // Theft detection: the legitimate latest token of the family is revoked too.
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(second)).StatusCode);
    }

    [Fact]
    public async Task Refresh_without_a_cookie_is_UNAUTHENTICATED()
    {
        var response = await RefreshAsync(cookie: null);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task A_deactivated_user_cannot_refresh()
    {
        var user = await api.CreateUserAsync(Password);
        var cookie = RefreshTokenOf(await LoginAsync(user.Email, Password));
        await api.QueryAsync(db => db.Set<User>().Where(u => u.Id == user.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.IsActive, false), _ct));

        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(cookie)).StatusCode);
    }

    // ---------------------------------------------------------------- protection

    [Fact]
    public async Task Endpoints_need_a_valid_bearer_token()
    {
        using var client = api.CreateClient();
        var user = await api.CreateUserAsync(Password);
        var token = await AccessTokenOf(await LoginAsync(user.Email, Password));

        var anonymous = await client.GetAsync(new Uri("/test/protected", UriKind.Relative), _ct);
        var forged = await GetProtectedAsync(token[..^4] + "AAAA");
        var signedIn = await GetProtectedAsync(token);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        var problem = await anonymous.Content.ReadFromJsonAsync<JsonElement>(_ct);
        Assert.Equal("UNAUTHENTICATED", problem.GetProperty("errorCode").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri("/test/anonymous", UriKind.Relative), _ct)).StatusCode);
    }

    // ---------------------------------------------------------------- first-login password change

    [Fact]
    public async Task An_issued_password_allows_only_the_password_change_until_it_is_replaced()
    {
        var user = await api.CreateUserAsync(Password, mustChangePassword: true);
        var login = await LoginAsync(user.Email, Password);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>(_ct);
        var token = loginBody.GetProperty("accessToken").GetString()!;
        var cookie = RefreshTokenOf(login);
        var otherSession = RefreshTokenOf(await LoginAsync(user.Email, Password));

        Assert.True(loginBody.GetProperty("user").GetProperty("mustChangePassword").GetBoolean());
        var blocked = await GetProtectedAsync(token);
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("PASSWORD_CHANGE_REQUIRED",
            (await blocked.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("errorCode").GetString());

        var changed = await ChangePasswordAsync(token, Password, "Mat-khau-moi-2026");
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        // The current session continues with an unrestricted token; other sessions are signed out.
        var refreshed = await RefreshAsync(cookie);
        var refreshedBody = await refreshed.Content.ReadFromJsonAsync<JsonElement>(_ct);
        Assert.False(refreshedBody.GetProperty("user").GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await GetProtectedAsync(refreshedBody.GetProperty("accessToken").GetString()!)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(otherSession)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(user.Email, Password)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user.Email, "Mat-khau-moi-2026")).StatusCode);
        Assert.True(await api.QueryAsync(db => db.Set<AuditLog>().AnyAsync(
            a => a.ActorId == user.Id && a.Module == AuditModule.User && a.Action == AuditAction.Update, _ct)));
    }

    [Theory]
    [InlineData("Sai-mat-khau-1", "Mat-khau-moi-2026", "currentPassword")]
    [InlineData(Password, "ngan1", "newPassword")]
    [InlineData(Password, "chi-co-chu-khong-so", "newPassword")]
    [InlineData(Password, Password, "newPassword")]
    public async Task Password_change_rejects_a_wrong_current_or_weak_new_password(string current, string next, string field)
    {
        var user = await api.CreateUserAsync(Password, mustChangePassword: true);
        var token = await AccessTokenOf(await LoginAsync(user.Email, Password));

        var response = await ChangePasswordAsync(token, current, next);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(_ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _), problem.GetRawText());
        var saved = await api.QueryAsync(db => db.Set<User>().SingleAsync(u => u.Id == user.Id, _ct));
        Assert.True(saved.MustChangePassword);
    }

    [Fact]
    public async Task Refresh_tokens_are_stored_only_as_hashes()
    {
        var user = await api.CreateUserAsync(Password);
        var cookie = RefreshTokenOf(await LoginAsync(user.Email, Password));

        var stored = await api.QueryAsync(db => db.Set<RefreshToken>().SingleAsync(t => t.UserId == user.Id, _ct));

        Assert.NotEqual(cookie, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
    }

    // ---------------------------------------------------------------- helpers

    private async Task<HttpResponseMessage> LoginAsync(string email, string password, string? ip = null)
    {
        using var client = api.CreateClient(new() { HandleCookies = false });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password }),
        };
        if (ip is not null)
        {
            request.Headers.Add(TestApiFactory.TestIpHeader, ip);
        }
        return await client.SendAsync(request, _ct);
    }

    private async Task<HttpResponseMessage> RefreshAsync(string? cookie)
    {
        using var client = api.CreateClient(new() { HandleCookies = false });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookie.Name}={cookie}");
        }
        return await client.SendAsync(request, _ct);
    }

    private async Task<HttpResponseMessage> GetProtectedAsync(string accessToken)
    {
        using var client = api.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request, _ct);
    }

    private async Task<HttpResponseMessage> ChangePasswordAsync(string accessToken, string current, string next)
    {
        using var client = api.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/me/password")
        {
            Content = JsonContent.Create(new { currentPassword = current, newPassword = next }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request, _ct);
    }

    private async Task<string> AccessTokenOf(HttpResponseMessage login) =>
        (await login.Content.ReadFromJsonAsync<JsonElement>(_ct)).GetProperty("accessToken").GetString()!;

    private static string RefreshTokenOf(HttpResponseMessage response)
    {
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(RefreshCookie.Name + "=", StringComparison.Ordinal));
        return cookie[(RefreshCookie.Name.Length + 1)..cookie.IndexOf(';', StringComparison.Ordinal)];
    }
}
