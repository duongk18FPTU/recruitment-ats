namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

/// <summary>Free-text search with (I)LIKE: the user's text is matched literally, never as a wildcard.</summary>
internal static class LikePattern
{
    public const string Escape = "\\";

    /// <summary><c>50%_off</c> → <c>%50\%\_off%</c>.</summary>
    public static string Contains(string text) =>
        "%" + text.Replace(Escape, Escape + Escape, StringComparison.Ordinal)
            .Replace("%", Escape + "%", StringComparison.Ordinal)
            .Replace("_", Escape + "_", StringComparison.Ordinal) + "%";
}
