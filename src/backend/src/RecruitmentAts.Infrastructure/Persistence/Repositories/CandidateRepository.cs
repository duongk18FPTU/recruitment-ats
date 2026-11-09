using Microsoft.EntityFrameworkCore;
using RecruitmentAts.Application.Abstractions.Persistence;
using RecruitmentAts.Application.Candidates;

namespace RecruitmentAts.Infrastructure.Persistence.Repositories;

internal sealed class CandidateRepository(AtsDbContext db) : ICandidateRepository
{
    // Same predicate as the partial unique indexes ux_candidates_email / ux_candidates_phone.
    public Task<Candidate?> FindByEmailAsync(string emailNormalized, CancellationToken cancellationToken) =>
        db.Set<Candidate>().SingleOrDefaultAsync(c => c.EmailNormalized == emailNormalized && !c.IsAnonymized, cancellationToken);

    public Task<Candidate?> FindByPhoneAsync(string phoneNormalized, CancellationToken cancellationToken) =>
        db.Set<Candidate>().SingleOrDefaultAsync(c => c.PhoneNormalized == phoneNormalized && !c.IsAnonymized, cancellationToken);

    public void Add(Candidate candidate) => db.Add(candidate);

    public void Add(CvFile cvFile) => db.Add(cvFile);
}
