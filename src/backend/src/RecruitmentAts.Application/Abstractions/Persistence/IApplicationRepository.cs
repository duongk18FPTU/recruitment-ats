using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Applications.Contracts;

namespace RecruitmentAts.Application.Abstractions.Persistence;

public interface IApplicationRepository
{
    /// <summary>The id of the candidate's application to the position (BR-06), or null.</summary>
    Task<Guid?> FindIdAsync(Guid positionId, Guid candidateId, CancellationToken cancellationToken);

    /// <summary>The application with its candidate, position, and CV files; <c>AllowedStages</c> is left empty.</summary>
    Task<ApplicationDetail?> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    void Add(JobApplication application);

    void Add(StageHistoryEntry entry);
}
