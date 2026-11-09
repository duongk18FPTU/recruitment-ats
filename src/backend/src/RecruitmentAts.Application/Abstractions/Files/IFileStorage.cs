namespace RecruitmentAts.Application.Abstractions.Files;

/// <summary>The CV file store; keys are stored in the database, never exposed as URLs.</summary>
public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens a stored file for reading; throws <see cref="FileNotFoundException"/> when the key is unknown.</summary>
    Task<Stream> OpenAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Removes a stored file; does nothing when the key is unknown.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
