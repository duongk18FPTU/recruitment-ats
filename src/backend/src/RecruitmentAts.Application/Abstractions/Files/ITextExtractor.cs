namespace RecruitmentAts.Application.Abstractions.Files;

public interface ITextExtractor
{
    /// <summary>
    /// Text of a PDF or DOCX file in reading order. Returns an empty string for a file that holds no text
    /// (a scan) or cannot be read (corrupt), so the caller can reject both the same way.
    /// </summary>
    Task<string> ExtractAsync(Stream content, string contentType, CancellationToken cancellationToken = default);
}
