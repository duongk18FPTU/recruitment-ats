using System.Security.Cryptography;
using RecruitmentAts.Application.Abstractions.Files;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Common;

namespace RecruitmentAts.Application.Candidates;

/// <summary>
/// Checks a CV against BR-15, extracts its text, and writes it to the file store. The returned
/// <see cref="CvFile"/> is not yet tracked by the database: the caller adds it in its unit of work and
/// calls <see cref="DiscardAsync"/> when that work fails, so no orphan file stays in storage.
/// </summary>
public sealed class CvFileService(IFileStorage storage, ITextExtractor extractor, IClock clock)
{
    public async Task<CvFile> StoreAsync(
        Guid candidateId, CvUpload upload, Guid? uploadedBy = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);

        var contentType = upload.ContentType.Split(';')[0].Trim().ToLowerInvariant();
        if (!CvFileRules.IsAllowed(contentType))
        {
            throw new AppException(ErrorCode.UnsupportedFileType, "Chỉ chấp nhận tệp CV định dạng PDF hoặc DOCX.");
        }

        var bytes = await ReadAtMostAsync(upload.Content, CvFileRules.MaxSizeBytes, cancellationToken);
        if (bytes.Length > CvFileRules.MaxSizeBytes)
        {
            throw new AppException(ErrorCode.FileTooLarge, "Tệp CV vượt quá 10 MB.");
        }

        if (bytes.Length == 0 || !CvFileRules.MatchesSignature(contentType, bytes))
        {
            throw new AppException(ErrorCode.UnsupportedFileType, "Nội dung tệp không phải PDF hoặc DOCX hợp lệ.");
        }

        string text;
        using (var buffer = new MemoryStream(bytes, writable: false))
        {
            text = Clean(await extractor.ExtractAsync(buffer, contentType, cancellationToken));
        }

        if (text.Length == 0)
        {
            throw new AppException(
                ErrorCode.NoExtractableText,
                "Không đọc được nội dung văn bản trong CV (tệp là ảnh quét hoặc bị lỗi). Hãy dùng bản PDF/DOCX có chữ.");
        }

        var id = Guid.CreateVersion7();
        var key = $"{candidateId:N}/{id:N}{CvFileRules.Extension(contentType)}";
        using (var buffer = new MemoryStream(bytes, writable: false))
        {
            await storage.SaveAsync(key, buffer, cancellationToken);
        }

        return new CvFile
        {
            Id = id,
            CandidateId = candidateId,
            StorageKey = key,
            OriginalName = TrimName(upload.FileName),
            ContentType = contentType,
            SizeBytes = bytes.Length,
            Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
            ExtractedText = text,
            UploadedBy = uploadedBy,
            UploadedAt = clock.UtcNow,
        };
    }

    /// <summary>Removes the stored file of a CV whose database work did not complete.</summary>
    public Task DiscardAsync(CvFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        return storage.DeleteAsync(file.StorageKey, cancellationToken);
    }

    private static async Task<byte[]> ReadAtMostAsync(Stream stream, int limit, CancellationToken cancellationToken)
    {
        // One byte more than the limit tells "exactly 10 MB" from "too large" without reading a huge body.
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        while (buffer.Length <= limit)
        {
            var wanted = (int)Math.Min(chunk.Length, limit + 1 - buffer.Length);
            var read = await stream.ReadAsync(chunk.AsMemory(0, wanted), cancellationToken);
            if (read == 0)
            {
                break;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    /// <summary>PostgreSQL <c>text</c> rejects NUL characters, which some PDFs produce.</summary>
    private static string Clean(string text) => text.Replace("\0", string.Empty, StringComparison.Ordinal).Trim();

    private static string TrimName(string fileName)
    {
        var name = Path.GetFileName(fileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "cv";
        }

        return name.Length <= 255 ? name : name[..255];
    }
}
