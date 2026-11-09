namespace RecruitmentAts.Application.Candidates;

/// <summary>BR-15: CV files are PDF or DOCX, at most 10 MB, and contain extractable text.</summary>
public static class CvFileRules
{
    public const string Pdf = "application/pdf";
    public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public const int MaxSizeBytes = 10 * 1024 * 1024;

    public static bool IsAllowed(string contentType) => contentType is Pdf or Docx;

    public static string Extension(string contentType) => contentType == Pdf ? ".pdf" : ".docx";

    /// <summary>Whether the first bytes match the declared type (PDF header or ZIP container for DOCX).</summary>
    public static bool MatchesSignature(string contentType, ReadOnlySpan<byte> content) => contentType switch
    {
        Pdf => content[..Math.Min(content.Length, 1024)].IndexOf("%PDF-"u8) >= 0,
        Docx => content.StartsWith("PK\u0003\u0004"u8),
        _ => false,
    };
}
