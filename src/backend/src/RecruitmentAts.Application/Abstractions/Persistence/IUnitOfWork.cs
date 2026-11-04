namespace RecruitmentAts.Application.Abstractions.Persistence;

/// <summary>
/// One database transaction per command: runs the work, saves every change it made
/// (including audit entries and queued jobs), and commits. A stale row version becomes
/// <c>CONCURRENCY_CONFLICT</c>.
/// </summary>
public interface IUnitOfWork
{
    Task ExecuteAsync(Func<Task> work, CancellationToken cancellationToken);

    Task<T> ExecuteAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken);
}
