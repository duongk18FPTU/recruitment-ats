using RecruitmentAts.Application.Abstractions.Files;

namespace RecruitmentAts.Infrastructure.Files;

/// <summary>Stores files under one root directory (a Docker volume in production).</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("The file store root is not set.", nameof(root));
        }

        _root = Path.GetFullPath(root);
    }

    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        var path = PathOf(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Never leave a half-written file under its final name.
        var temp = path + ".tmp";
        try
        {
            await using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await content.CopyToAsync(file, cancellationToken);
            }

            File.Move(temp, path, overwrite: false);
        }
        finally
        {
            File.Delete(temp);
        }
    }

    public Task<Stream> OpenAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = PathOf(key);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The stored file does not exist.", key);
        }

        return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        // File.Delete ignores a missing file but throws when the directory is missing too.
        var path = PathOf(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string PathOf(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("The key points outside the file store.", nameof(key));
        }

        return path;
    }
}
