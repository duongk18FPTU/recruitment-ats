using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Abstractions.Time;
using RecruitmentAts.Application.Applications;
using RecruitmentAts.Application.Applications.Contracts;
using RecruitmentAts.Application.Candidates;
using RecruitmentAts.Application.Positions;
using RecruitmentAts.Application.Screening;

namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

internal sealed class ApplicationRepository(AtsDbContext db, IClock clock) : IApplicationRepository
{
    public async Task<Guid?> FindIdAsync(Guid positionId, Guid candidateId, CancellationToken cancellationToken) =>
        await db.Set<JobApplication>().AsNoTracking()
            .Where(a => a.PositionId == positionId && a.CandidateId == candidateId)
            .Select(a => (Guid?)a.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ApplicationDetail?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await (
            from app in db.Set<JobApplication>().AsNoTracking()
            where app.Id == id
            join cand in db.Set<Candidate>().AsNoTracking() on app.CandidateId equals cand.Id
            join pos in db.Set<Position>().AsNoTracking() on app.PositionId equals pos.Id
            select new
            {
                Application = app,
                Candidate = cand,
                PositionCode = pos.Code,
                PositionTitle = pos.Title,
                AnalysisStatus = db.Set<CvAnalysis>()
                    .Where(x => x.ApplicationId == app.Id)
                    .OrderByDescending(x => x.RequestedAt)
                    .Select(x => (AnalysisStatus?)x.Status)
                    .FirstOrDefault(),
            }).SingleOrDefaultAsync(cancellationToken);
        if (row is null)
        {
            return null;
        }

        var a = row.Application;
        var cvFiles = await db.Set<CvFile>().AsNoTracking()
            .Where(f => f.CandidateId == a.CandidateId && f.DeletedAt == null)
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new CvFileView(f.Id, f.OriginalName, f.ContentType, f.SizeBytes, f.UploadedAt, f.Id == a.CurrentCvFileId))
            .ToListAsync(cancellationToken);
        var c = row.Candidate;

        return new ApplicationDetail(
            a.Id,
            new CandidateSummary(c.Id, c.FullName, c.Email, c.Phone, c.EmailInvalid, c.PoolConsentExpiresAt > clock.UtcNow),
            new ApplicationPositionRef(a.PositionId, row.PositionCode, row.PositionTitle),
            a.Stage, a.Source, a.AppliedAt, a.StageChangedAt,
            a.FitScore, a.MissingMustHave, row.AnalysisStatus, NextInterviewAt: null,
            a.SourceNote, a.IntakeChannel, a.CoverMessage, c.Notes,
            a.RejectionReason, a.RejectionNote, a.CurrentCvFileId,
            cvFiles, AllowedStages: [], PendingEmailCount: 0,
            a.RowVersion.ToString(CultureInfo.InvariantCulture));
    }

    public void Add(JobApplication application) => db.Add(application);

    public void Add(StageHistoryEntry entry) => db.Add(entry);
}
