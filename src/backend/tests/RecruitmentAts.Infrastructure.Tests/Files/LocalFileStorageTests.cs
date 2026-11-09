using RecruitmentAts.Infrastructure.Files;

namespace RecruitmentAts.Infrastructure.Tests.Files;

public sealed class LocalFileStorageTests : IDisposable
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private readonly string _root = Path.Combine(Path.GetTempPath(), "ats-files-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task Saves_opens_and_deletes_a_file_in_a_nested_key()
    {
        var storage = new LocalFileStorage(_root);
        byte[] content = [1, 2, 3, 4];

        await storage.SaveAsync("abc/def.pdf", new MemoryStream(content), Token);

        await using (var opened = await storage.OpenAsync("abc/def.pdf", Token))
        {
            using var copy = new MemoryStream();
            await opened.CopyToAsync(copy, Token);
            Assert.Equal(content, copy.ToArray());
        }

        Assert.Empty(Directory.GetFiles(Path.Combine(_root, "abc"), "*.tmp"));

        await storage.DeleteAsync("abc/def.pdf", Token);

        await Assert.ThrowsAsync<FileNotFoundException>(() => storage.OpenAsync("abc/def.pdf", Token));
    }

    [Fact]
    public async Task Deleting_an_unknown_key_does_nothing()
    {
        await new LocalFileStorage(_root).DeleteAsync("missing/file.pdf", Token);
    }

    [Theory]
    [InlineData("../outside.pdf")]
    [InlineData("a/../../outside.pdf")]
    [InlineData("..")]
    public async Task Refuses_a_key_that_leaves_the_store(string key)
    {
        var storage = new LocalFileStorage(_root);

        await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveAsync(key, new MemoryStream([1]), Token));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.OpenAsync(key, Token));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.DeleteAsync(key, Token));
    }

    [Fact]
    public async Task Does_not_overwrite_an_existing_file()
    {
        var storage = new LocalFileStorage(_root);
        await storage.SaveAsync("k.pdf", new MemoryStream([1]), Token);

        await Assert.ThrowsAsync<IOException>(() => storage.SaveAsync("k.pdf", new MemoryStream([2]), Token));

        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }
}
