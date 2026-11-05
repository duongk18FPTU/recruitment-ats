namespace RecruitmentAts.Api.Auth;

/// <summary>The refresh token cookie (AD-06): httpOnly, Secure, SameSite=Strict, sent only to /api/auth.</summary>
public static class RefreshCookie
{
    public const string Name = "ats_refresh";
    private const string Path = "/api/auth";

    public static string? Read(HttpRequest request) =>
        request?.Cookies.TryGetValue(Name, out var value) == true ? value : null;

    public static void Write(HttpResponse response, string token, DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Cookies.Append(Name, token, Options(expiresAt));
    }

    public static void Delete(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Cookies.Delete(Name, Options(expires: null));
    }

    private static CookieOptions Options(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true,
    };
}
