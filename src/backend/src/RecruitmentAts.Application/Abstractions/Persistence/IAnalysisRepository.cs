namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface IAnalysisRepository
{
    /// <summary>
    /// Sets the completed analyses of the position's applications made with an older criteria version
    /// to <c>OUTDATED</c> (AC-US06 #2); returns how many changed.
    /// </summary>
    Task<int> MarkOutdatedAsync(Guid positionId, int currentVersion, CancellationToken cancellationToken);
}
