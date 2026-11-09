namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface IAnalysisRepository
{
    /// <summary>Queues an analysis; the Worker picks up <c>PENDING</c> rows (AD-02).</summary>
    void Add(RecruitmentAts.Application.Screening.CvAnalysis analysis);

    /// <summary>
    /// Sets the completed analyses of the position's applications made with an older criteria version
    /// to <c>OUTDATED</c> (AC-US06 #2); returns how many changed.
    /// </summary>
    Task<int> MarkOutdatedAsync(Guid positionId, int currentVersion, CancellationToken cancellationToken);
}
