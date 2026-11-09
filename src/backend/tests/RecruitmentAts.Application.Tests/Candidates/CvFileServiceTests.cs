using System.Text;
using RecruitmentAts.Application.Abstractions.Files;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Common;
using RecruitmentAts.Application.Tests.Fakes;

namespace RecruitmentAts.Application.Tests.Candidates;

/// <summary>BR-15 and AC-US09 #4: type, size, and extractable text are checked before anything is stored.</summary>
public class CvFileServiceTests
{
    private static readonly Guid CandidateId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 8, 0, 0, TimeSpan.Zero);

    private readonly InMemoryStorage _storage = new();
    private readonly StubExtractor _extractor = new("Nguyen Van A\nBackend Developer");

    private CvFileService Service() => new(_storage, _extractor, new FakeClock(Now));

    private Task<CvFile> Store(CvUpload upload, Guid? uploadedBy = null) => Store(Service(), upload, uploadedBy);

    private static Task<CvFile> Store(CvFileService service, CvUpload upload, Guid? uploadedBy = null) =>
        service.StoreAsync(CandidateId, upload, uploadedBy, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Stores_a_pdf_with_its_text_hash_and_a_key_that_hides_the_file_name()
    {
        var bytes = Pdf(1000);

        var file = await Store(Upload("CV Nguyen Van A.pdf", CvFileRules.Pdf, bytes), uploadedBy: Guid.CreateVersion7());

        Assert.Equal(CandidateId, file.CandidateId);
        Assert.Equal("CV Nguyen Van A.pdf", file.OriginalName);
        Assert.Equal(CvFileRules.Pdf, file.ContentType);
        Assert.Equal(1000, file.SizeBytes);
        Assert.Equal("Nguyen Van A\nBackend Developer", file.ExtractedText);
        Assert.Equal(Now, file.UploadedAt);
        Assert.Equal(64, file.Sha256.Length);
        Assert.EndsWith(".pdf", file.StorageKey, StringComparison.Ordinal);
        Assert.DoesNotContain("Nguyen", file.StorageKey, StringComparison.Ordinal);
        Assert.Equal(bytes, _storage.Files[file.StorageKey]);
    }

    [Fact]
    public async Task Accepts_a_docx_and_a_content_type_with_parameters()
    {
        var file = await Store(Upload("cv.docx", CvFileRules.Docx.ToUpperInvariant() + "; charset=binary", Docx(500)));

        Assert.Equal(CvFileRules.Docx, file.ContentType);
        Assert.EndsWith(".docx", file.StorageKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Accepts_a_file_of_exactly_10_MB()
    {
        var file = await Store(Upload("cv.pdf", CvFileRules.Pdf, Pdf(CvFileRules.MaxSizeBytes)));

        Assert.Equal(CvFileRules.MaxSizeBytes, file.SizeBytes);
    }

    [Fact]
    public async Task Rejects_a_file_over_10_MB_without_storing_it()
    {
        var error = await Assert.ThrowsAsync<AppException>(() =>
            Store(Upload("cv.pdf", CvFileRules.Pdf, Pdf(CvFileRules.MaxSizeBytes + 1))));

        Assert.Equal(ErrorCode.FileTooLarge, error.Code);
        Assert.Empty(_storage.Files);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("text/plain")]
    [InlineData("application/msword")]
    [InlineData("")]
    public async Task Rejects_other_content_types(string contentType)
    {
        var error = await Assert.ThrowsAsync<AppException>(() =>
            Store(Upload("cv.pdf", contentType, Pdf(100))));

        Assert.Equal(ErrorCode.UnsupportedFileType, error.Code);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Rejects_content_that_does_not_match_the_declared_type()
    {
        var notPdf = Encoding.ASCII.GetBytes("this is a text file renamed to cv.pdf");

        var error = await Assert.ThrowsAsync<AppException>(() =>
            Store(Upload("cv.pdf", CvFileRules.Pdf, notPdf)));

        Assert.Equal(ErrorCode.UnsupportedFileType, error.Code);
    }

    [Fact]
    public async Task Rejects_an_empty_file()
    {
        var error = await Assert.ThrowsAsync<AppException>(() =>
            Store(Upload("cv.pdf", CvFileRules.Pdf, [])));

        Assert.Equal(ErrorCode.UnsupportedFileType, error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n\t ")]
    [InlineData("\0\0")]
    public async Task Rejects_a_file_without_extractable_text_and_stores_nothing(string text)
    {
        _extractor.Text = text;

        var error = await Assert.ThrowsAsync<AppException>(() =>
            Store(Upload("scan.pdf", CvFileRules.Pdf, Pdf(2000))));

        Assert.Equal(ErrorCode.NoExtractableText, error.Code);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Removes_NUL_characters_that_PostgreSQL_text_cannot_hold()
    {
        _extractor.Text = "Java\0 Developer ";

        var file = await Store(Upload("cv.pdf", CvFileRules.Pdf, Pdf(100)));

        Assert.Equal("Java Developer", file.ExtractedText);
    }

    [Theory]
    [InlineData(@"C:\Users\a\Documents\cv.pdf", "cv.pdf")]
    [InlineData("../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData("", "cv")]
    public async Task Keeps_only_the_file_name_of_the_original_path(string original, string expected)
    {
        var file = await Store(Upload(original, CvFileRules.Pdf, Pdf(100)));

        Assert.Equal(expected, file.OriginalName);
    }

    [Fact]
    public async Task Discard_removes_the_stored_file_after_a_failed_transaction()
    {
        var service = Service();
        var file = await Store(service, Upload("cv.pdf", CvFileRules.Pdf, Pdf(100)));

        await service.DiscardAsync(file, TestContext.Current.CancellationToken);

        Assert.Empty(_storage.Files);
    }

    private static CvUpload Upload(string name, string contentType, byte[] bytes) =>
        new(name, contentType, new MemoryStream(bytes));

    private static byte[] Pdf(int size) => Sized("%PDF-1.7\n"u8, size);

    private static byte[] Docx(int size) => Sized("PK\u0003\u0004"u8, size);

    private static byte[] Sized(ReadOnlySpan<byte> header, int size)
    {
        var bytes = new byte[size];
        header[..Math.Min(header.Length, size)].CopyTo(bytes);
        return bytes;
    }

    private sealed class InMemoryStorage : IFileStorage
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
        {
            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            Files[key] = copy.ToArray();
        }

        public Task<Stream> OpenAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(Files[key]));

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            Files.Remove(key);
            return Task.CompletedTask;
        }
    }

    private sealed class StubExtractor(string text) : ITextExtractor
    {
        public string Text { get; set; } = text;

        public Task<string> ExtractAsync(Stream content, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult(Text);
    }
}
