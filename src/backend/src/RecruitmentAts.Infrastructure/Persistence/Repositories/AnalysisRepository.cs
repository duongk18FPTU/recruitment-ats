using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Screening;

namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

internal sealed class AnalysisRepository(AtsDbContext db) : IAnalysisRepository
{
    public void Add(CvAnalysis analysis) => db.Add(analysis);

    // Runs at once inside the caller's unit-of-work transaction.
    public Task<int> MarkOutdatedAsync(Guid positionId, int currentVersion, CancellationToken cancellationToken) =>
        db.Set<CvAnalysis>()
            .Where(a => a.Status == AnalysisStatus.Completed
                && a.CriteriaVersion < currentVersion
                && db.Set<JobApplication>().Any(app => app.Id == a.ApplicationId && app.PositionId == positionId))
            .ExecuteUpdateAsync(set => set.SetProperty(a => a.Status, AnalysisStatus.Outdated), cancellationToken);
}
