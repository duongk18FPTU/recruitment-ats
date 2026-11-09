using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RecruitmentAts.Application.Abstractions.Files;
using RecruitmentAts.Application.Candidates;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace RecruitmentAts.Infrastructure.Files;

/// <summary>PDF text with PdfPig and DOCX text with the Open XML SDK. No OCR: a scan has no text.</summary>
public sealed class DocumentTextExtractor : ITextExtractor
{
    public Task<string> ExtractAsync(Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(contentType switch
            {
                CvFileRules.Pdf => FromPdf(content),
                CvFileRules.Docx => FromDocx(content),
                _ => string.Empty,
            });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Parsers throw many different types on damaged input; a CV that cannot be read has no text.
            return Task.FromResult(string.Empty);
        }
    }

    private static string FromPdf(Stream content)
    {
        using var document = PdfDocument.Open(content);
        return string.Join('\n', document.GetPages().Select(page => ContentOrderTextExtractor.GetText(page)));
    }

    private static string FromDocx(Stream content)
    {
        using var document = WordprocessingDocument.Open(content, isEditable: false);
        var body = document.MainDocumentPart?.Document?.Body;
        return body is null ? string.Empty : string.Join('\n', body.Descendants<Paragraph>().Select(p => p.InnerText));
    }
}
