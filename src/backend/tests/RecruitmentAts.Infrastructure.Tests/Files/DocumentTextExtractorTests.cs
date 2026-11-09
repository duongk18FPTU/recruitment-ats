using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Infrastructure.Files;

namespace RecruitmentAts.Infrastructure.Tests.Files;

/// <summary>Real PDF and DOCX files are built in memory, so no binary fixtures are checked in.</summary>
public class DocumentTextExtractorTests
{
    private readonly DocumentTextExtractor _extractor = new();

    [Fact]
    public async Task Reads_the_text_of_a_pdf()
    {
        var text = await Extract(Pdf("Backend Developer with 5 years of .NET"), CvFileRules.Pdf);

        Assert.Contains("Backend Developer with 5 years of .NET", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_pdf_without_text_gives_an_empty_string()
    {
        var text = await Extract(Pdf(null), CvFileRules.Pdf);

        Assert.Equal(string.Empty, text.Trim());
    }

    [Fact]
    public async Task Reads_paragraphs_and_table_cells_of_a_docx()
    {
        var text = await Extract(Docx("Nguyen Van A", "Kinh nghiệm: 5 năm ASP.NET"), CvFileRules.Docx);

        Assert.Contains("Nguyen Van A", text, StringComparison.Ordinal);
        Assert.Contains("Kinh nghiệm: 5 năm ASP.NET", text, StringComparison.Ordinal);
        Assert.Contains("Skill: SQL", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(CvFileRules.Pdf)]
    [InlineData(CvFileRules.Docx)]
    public async Task A_damaged_file_gives_an_empty_string(string contentType)
    {
        var header = contentType == CvFileRules.Pdf ? "%PDF-1.7\n garbage" : "PK\u0003\u0004 garbage";

        var text = await Extract(Encoding.ASCII.GetBytes(header), contentType);

        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public async Task An_unknown_content_type_gives_an_empty_string()
    {
        Assert.Equal(string.Empty, await Extract(Pdf("text"), "image/png"));
    }

    private Task<string> Extract(byte[] bytes, string contentType) =>
        _extractor.ExtractAsync(new MemoryStream(bytes), contentType, TestContext.Current.CancellationToken);

    /// <summary>A one-page PDF with the Helvetica font; offsets in the cross-reference table are computed.</summary>
    private static byte[] Pdf(string? text)
    {
        var content = text is null ? string.Empty : $"BT /F1 12 Tf 72 720 Td ({text}) Tj ET";
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        ];

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }

        var xref = pdf.Length;
        pdf.Append("xref\n0 ").Append(objects.Length + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            pdf.Append(offset.ToString("D10", System.Globalization.CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        pdf.Append("trailer\n<< /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\nstartxref\n")
            .Append(xref).Append("\n%%EOF");
        return Encoding.Latin1.GetBytes(pdf.ToString());
    }

    private static byte[] Docx(string firstParagraph, string secondParagraph)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(new Run(new Text(firstParagraph))),
                new Paragraph(new Run(new Text(secondParagraph))),
                new Table(new TableRow(new TableCell(new Paragraph(new Run(new Text("Skill: SQL"))))))));
        }

        return stream.ToArray();
    }
}
